#!/usr/bin/env python3
"""Loopback-only framed TCP <-> local IKEv2 UDP relay. No VPN termination.

Reach this service through an authenticated SSH forward, never a public bind.
Wire: b'PGIK\x01' + channel (0=probe, 1=IKE/500, 2=NAT-T/4500), echoed;
then repeated network-order uint16 length + opaque UDP payload in each direction.
The target address is fixed by the administrator; clients cannot choose a host.
"""

import argparse
import asyncio
import ipaddress
import logging
import socket
import struct

MAGIC = b"PGIK\x01"
MAX_DATAGRAM = 65507
MAX_CONNECTIONS = 32
MAX_QUEUE_BYTES = 262144
HANDSHAKE_TIMEOUT = 10
IDLE_TIMEOUT = 300
WRITE_TIMEOUT = 30


async def read_frame(reader):
    length = struct.unpack("!H", await reader.readexactly(2))[0]
    if not 1 <= length <= MAX_DATAGRAM:
        raise ValueError("Invalid frame length")
    return await reader.readexactly(length)


async def write_frame(writer, data):
    if not 1 <= len(data) <= MAX_DATAGRAM:
        raise ValueError("Invalid datagram length")
    writer.write(struct.pack("!H", len(data)) + data)
    await asyncio.wait_for(writer.drain(), WRITE_TIMEOUT)


class DatagramQueue(asyncio.DatagramProtocol):
    def __init__(self):
        self.queue = asyncio.Queue(maxsize=128)
        self.queued_bytes = 0

    def datagram_received(self, data, addr):
        if (not 1 <= len(data) <= MAX_DATAGRAM or self.queue.full()
                or self.queued_bytes + len(data) > MAX_QUEUE_BYTES):
            return
        self.queue.put_nowait(data)
        self.queued_bytes += len(data)

    async def get(self):
        data = await self.queue.get()
        self.queued_bytes -= len(data)
        return data


class Relay:
    def __init__(self, target, *, ports=(500, 4500), max_connections=MAX_CONNECTIONS,
                 idle_timeout=IDLE_TIMEOUT):
        # ports is injectable for local integration tests, never supplied by clients.
        self.target = str(ipaddress.IPv4Address(target))
        self.ports = ports
        self.max_connections = max_connections
        self.idle_timeout = idle_timeout
        self.active = set()

    async def handle(self, reader, writer):
        task = asyncio.current_task()
        if len(self.active) >= self.max_connections:
            writer.close()
            await writer.wait_closed()
            return
        self.active.add(task)
        transport = None
        tasks = []
        try:
            hello = await asyncio.wait_for(reader.readexactly(6), HANDSHAKE_TIMEOUT)
            if hello[:5] != MAGIC or hello[5] not in (0, 1, 2):
                return
            if hello[5] != 0:
                loop = asyncio.get_running_loop()
                transport, udp = await loop.create_datagram_endpoint(
                    DatagramQueue, local_addr=("127.0.0.1", 0),
                    remote_addr=(self.target, self.ports[hello[5] - 1]),
                    family=socket.AF_INET)
            writer.write(hello)
            await asyncio.wait_for(writer.drain(), WRITE_TIMEOUT)
            if hello[5] == 0:
                return

            async def to_udp():
                while True:
                    data = await asyncio.wait_for(read_frame(reader), self.idle_timeout)
                    # A fixed, connected UDP socket retains its NAT mapping for this flow.
                    transport.sendto(data)

            async def to_tcp():
                while True:
                    await write_frame(writer, await udp.get())

            tasks = [asyncio.create_task(to_udp()), asyncio.create_task(to_tcp())]
            done, _ = await asyncio.wait(tasks, return_when=asyncio.FIRST_COMPLETED)
            for completed in done:
                completed.result()
        except (asyncio.IncompleteReadError, ConnectionError, OSError,
                ValueError, asyncio.TimeoutError):
            # Packet contents, identifiers and authentication exchanges are never logged.
            pass
        finally:
            for child in tasks:
                child.cancel()
            await asyncio.gather(*tasks, return_exceptions=True)
            if transport is not None:
                transport.close()
            writer.close()
            try:
                await asyncio.wait_for(writer.wait_closed(), 1)
            except (ConnectionError, OSError, asyncio.TimeoutError):
                writer.transport.abort()
            self.active.discard(task)

    async def close(self):
        tasks = list(self.active)
        for task in tasks:
            task.cancel()
        await asyncio.gather(*tasks, return_exceptions=True)


async def serve(target, port):
    # Refuse an arbitrary Internet destination, even if misconfigured as root.
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as check:
        check.bind((str(ipaddress.IPv4Address(target)), 0))
    relay = Relay(target)
    server = await asyncio.start_server(relay.handle, "127.0.0.1", port, limit=131072)
    logging.info("ProGo IKEv2 relay listening on loopback")
    try:
        async with server:
            await server.serve_forever()
    finally:
        await relay.close()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--target", required=True, type=ipaddress.IPv4Address,
                        help="Local IPv4 address used by strongSwan local_addrs")
    parser.add_argument("--port", type=int, default=17878)
    args = parser.parse_args()
    if not 1024 <= args.port <= 65535:
        parser.error("Port must be between 1024 and 65535")
    logging.basicConfig(level=logging.INFO, format="%(levelname)s: %(message)s")
    try:
        asyncio.run(serve(str(args.target), args.port))
    except KeyboardInterrupt:
        pass


if __name__ == "__main__":
    main()
