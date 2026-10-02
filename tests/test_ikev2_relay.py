import asyncio
import contextlib
import importlib.util
import json
import os
from pathlib import Path
import socket
import struct
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("relay", ROOT / "server/ikev2_relay.py")
relay = importlib.util.module_from_spec(spec)
spec.loader.exec_module(relay)


class Echo(asyncio.DatagramProtocol):
    def connection_made(self, transport):
        self.transport = transport
        self.packets = []

    def datagram_received(self, data, addr):
        self.packets.append((data, addr))
        self.transport.sendto(data, addr)


class RelayFixture(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        loop = asyncio.get_running_loop()
        self.transports, self.echoes, ports = [], [], []
        for _ in range(2):
            transport, echo = await loop.create_datagram_endpoint(Echo, local_addr=("127.0.0.1", 0))
            self.transports.append(transport)
            self.echoes.append(echo)
            ports.append(transport.get_extra_info("sockname")[1])
        self.relay = relay.Relay("127.0.0.1", ports=ports, idle_timeout=2)
        self.server = await asyncio.start_server(self.relay.handle, "127.0.0.1", 0)
        self.port = self.server.sockets[0].getsockname()[1]
        self.writers = []

    async def asyncTearDown(self):
        for writer in self.writers:
            writer.close()
            with contextlib.suppress(OSError):
                await writer.wait_closed()
        self.server.close()
        await self.server.wait_closed()
        await self.relay.close()
        for transport in self.transports:
            transport.close()

    async def connect(self, channel):
        reader, writer = await asyncio.open_connection("127.0.0.1", self.port)
        self.writers.append(writer)
        hello = relay.MAGIC + bytes([channel])
        # TCP does not preserve write boundaries, including in the greeting.
        for byte in hello:
            writer.write(bytes([byte]))
            await writer.drain()
        self.assertEqual(await reader.readexactly(6), hello)
        return reader, writer


class BackendTests(RelayFixture):
    async def test_probe_does_not_send_udp(self):
        reader, _ = await self.connect(0)
        self.assertEqual(await reader.read(), b"")
        self.assertEqual([len(e.packets) for e in self.echoes], [0, 0])

    async def test_channels_binary_frames_and_stable_udp_source(self):
        for channel in (1, 2):
            reader, writer = await self.connect(channel)
            payloads = [b"\xff", bytes(range(256)), b"x" * 60000]
            wire = b"".join(struct.pack("!H", len(p)) + p for p in payloads)
            for pos in range(0, len(wire), 113):
                writer.write(wire[pos:pos + 113])
            await writer.drain()
            for payload in payloads:
                self.assertEqual(await asyncio.wait_for(relay.read_frame(reader), 3), payload)
            self.assertEqual(len({addr for _, addr in self.echoes[channel - 1].packets}), 1)

    async def test_invalid_channel_and_lengths_are_closed(self):
        reader, writer = await asyncio.open_connection("127.0.0.1", self.port)
        self.writers.append(writer)
        writer.write(relay.MAGIC + b"\x03")
        await writer.drain()
        self.assertEqual(await asyncio.wait_for(reader.read(), 1), b"")
        for size in (0, 65508, 65535):
            reader, writer = await self.connect(1)
            writer.write(struct.pack("!H", size))
            await writer.drain()
            self.assertEqual(await asyncio.wait_for(reader.read(), 1), b"")
        self.assertEqual(len(self.echoes[0].packets), 0)

    async def test_connection_cap_and_idle_cleanup(self):
        self.relay.max_connections = 1
        self.relay.idle_timeout = 0.15
        reader, _ = await self.connect(1)
        rejected, writer = await asyncio.open_connection("127.0.0.1", self.port)
        self.writers.append(writer)
        self.assertEqual(await asyncio.wait_for(rejected.read(), 1), b"")
        self.assertEqual(await asyncio.wait_for(reader.read(), 1), b"")
        for _ in range(20):
            if not self.relay.active:
                break
            await asyncio.sleep(0.01)
        self.assertEqual(len(self.relay.active), 0)


def free_udp_port():
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
        sock.bind(("127.0.0.1", 0))
        return sock.getsockname()[1]


def ike(payload=b"", natt=False):
    # Synthetic IKE header; these tests check transport, not cryptography or authentication.
    header = bytearray(28)
    header[0] = 1
    header[17:20] = bytes([0x20, 34, 8])
    header[24:28] = struct.pack("!I", 28 + len(payload))
    return (b"\0" * 4 if natt else b"") + header + payload


@unittest.skipUnless(os.environ.get("PROGO_RELAY_TEST_COMMAND"), "Set PROGO_RELAY_TEST_COMMAND to a JSON argv array for the compiled C# harness")
class WindowsTransportTests(RelayFixture):
    async def asyncSetUp(self):
        await super().asyncSetUp()
        self.socks_connections = 0
        self.socks_tasks = set()
        self.socks = await asyncio.start_server(self.socks_handle, "127.0.0.1", 0)
        self.ike_port, self.nat_port = free_udp_port(), free_udp_port()
        while self.nat_port == self.ike_port:
            self.nat_port = free_udp_port()
        self.process = await asyncio.create_subprocess_exec(
            *json.loads(os.environ["PROGO_RELAY_TEST_COMMAND"]),
            str(self.socks.sockets[0].getsockname()[1]), str(self.ike_port),
            str(self.nat_port), str(self.port), stdin=asyncio.subprocess.PIPE,
            stdout=asyncio.subprocess.PIPE, stderr=asyncio.subprocess.PIPE)
        self.assertEqual((await asyncio.wait_for(self.process.stdout.readline(), 15)).strip(), b"READY")

    async def socks_handle(self, reader, writer):
        task = asyncio.current_task()
        self.socks_tasks.add(task)
        upstream = None
        pumps = []
        try:
            self.assertEqual(await reader.readexactly(3), b"\x05\x01\x00")
            writer.write(b"\x05")
            await writer.drain()
            writer.write(b"\x00")
            await writer.drain()
            request = await reader.readexactly(10)
            self.assertEqual(request[:8], b"\x05\x01\x00\x01\x7f\0\0\x01")
            self.assertEqual(struct.unpack("!H", request[8:])[0], self.port)
            self.socks_connections += 1
            remote, upstream = await asyncio.open_connection("127.0.0.1", self.port)
            for byte in b"\x05\x00\x00\x01\x7f\0\0\x01\0\0":
                writer.write(bytes([byte]))
                await writer.drain()

            async def pump(source, target):
                while True:
                    data = await source.read(4096)
                    if not data:
                        return
                    target.write(data)
                    await target.drain()

            pumps = [asyncio.create_task(pump(reader, upstream)), asyncio.create_task(pump(remote, writer))]
            done, _ = await asyncio.wait(pumps, return_when=asyncio.FIRST_COMPLETED)
            for completed in done:
                completed.result()
        except (OSError, asyncio.IncompleteReadError):
            pass
        finally:
            for pump_task in pumps:
                pump_task.cancel()
            await asyncio.gather(*pumps, return_exceptions=True)
            for target in (writer, upstream):
                if target is not None:
                    target.close()
                    with contextlib.suppress(OSError):
                        await target.wait_closed()
            self.socks_tasks.discard(task)

    async def asyncTearDown(self):
        if self.process.returncode is None:
            self.process.stdin.write(b"quit\n")
            await self.process.stdin.drain()
        self.assertEqual(await asyncio.wait_for(self.process.wait(), 5), 0)
        self.assertEqual(await self.process.stderr.read(), b"")
        self.socks.close()
        await self.socks.wait_closed()
        tasks = list(self.socks_tasks)
        for task in tasks:
            task.cancel()
        await asyncio.gather(*tasks, return_exceptions=True)
        await super().asyncTearDown()

    async def test_frontend_routes_both_ports_via_socks_and_isolates_clients(self):
        loop = asyncio.get_running_loop()
        peers = [socket.socket(socket.AF_INET, socket.SOCK_DGRAM) for _ in range(2)]
        try:
            for sock in peers:
                sock.bind(("127.0.0.1", 0))
                sock.setblocking(False)
            for port, natt in ((self.ike_port, False), (self.nat_port, True)):
                for index, sock in enumerate(peers):
                    packet = ike(bytes([index]) * 1000, natt)
                    await loop.sock_sendto(sock, packet, ("127.0.0.1", port))
                for index, sock in enumerate(peers):
                    packet, addr = await asyncio.wait_for(loop.sock_recvfrom(sock, 65535), 5)
                    self.assertEqual(packet, ike(bytes([index]) * 1000, natt))
                    self.assertEqual(addr[1], port)
            for packet in (b"\xff", b"\x12\x34\x56\x78" + b"x" * 1500):
                await loop.sock_sendto(peers[0], packet, ("127.0.0.1", self.nat_port))
                result, _ = await asyncio.wait_for(loop.sock_recvfrom(peers[0], 65535), 5)
                self.assertEqual(result, packet)
            self.assertEqual(self.socks_connections, 5)  # probe + four independent flows
            self.process.stdin.write(b"stop\n")
            await self.process.stdin.drain()
            await asyncio.wait_for(self.process.stdout.readline(), 3)
            # Stop releases the UDP ports immediately, even with active streams.
            with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as check:
                check.bind(("127.0.0.1", self.ike_port))
        finally:
            for sock in peers:
                sock.close()

    async def test_stray_esp_and_malformed_ike_do_not_open_upstream(self):
        loop = asyncio.get_running_loop()
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
            sock.setblocking(False)
            for packet in (b"\xff", b"\x12\x34\x56\x78" + b"x" * 20, b"\0" * 32):
                await loop.sock_sendto(sock, packet, ("127.0.0.1", self.nat_port))
            await asyncio.sleep(0.15)
        self.assertEqual(self.socks_connections, 1)


@unittest.skipUnless(os.environ.get("PROGO_RELAY_TEST_COMMAND"), "Requires compiled C# harness")
class StartupFailureTests(unittest.IsolatedAsyncioTestCase):
    async def test_socks_refusal_does_not_start_udp_or_bypass_proxy(self):
        async def reject(reader, writer):
            await reader.readexactly(3)
            writer.write(b"\x05\xff")
            await writer.drain()
            writer.close()
            await writer.wait_closed()

        socks = await asyncio.start_server(reject, '127.0.0.1', 0)
        ike_port, nat_port = free_udp_port(), free_udp_port()
        try:
            process = await asyncio.create_subprocess_exec(
                *json.loads(os.environ['PROGO_RELAY_TEST_COMMAND']),
                str(socks.sockets[0].getsockname()[1]), str(ike_port), str(nat_port), '17878',
                stdin=asyncio.subprocess.PIPE, stdout=asyncio.subprocess.PIPE, stderr=asyncio.subprocess.PIPE)
            stdout, _ = await asyncio.wait_for(process.communicate(), 5)
            self.assertEqual(process.returncode, 1)
            self.assertNotIn(b'READY', stdout)
            with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
                sock.bind(('127.0.0.1', ike_port))
        finally:
            socks.close()
            await socks.wait_closed()


if __name__ == "__main__":
    unittest.main()
