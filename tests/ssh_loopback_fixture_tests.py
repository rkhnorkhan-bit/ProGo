"""Verify the disposable server using native OpenSSH, not a mock SSH client."""
import pathlib
import shutil
import socket
import struct
import subprocess
import tempfile
import time
import unittest
from contextlib import contextmanager

from ssh_loopback_fixture import LoopbackSshFixture


class NativeSshFixtureTests(unittest.TestCase):
    def setUp(self):
        self.ssh = shutil.which("ssh")
        self.keygen = shutil.which("ssh-keygen")
        if not self.ssh or not self.keygen:
            self.fail("native ssh and ssh-keygen are required; fixture validation NOT_RUN")
        self.temporary = tempfile.TemporaryDirectory(prefix="progo-owned-ssh-")
        self.addCleanup(self.temporary.cleanup)
        self.root = pathlib.Path(self.temporary.name)
        self.key = self.make_key("selected")

    def make_key(self, name):
        path = self.root / name
        result = subprocess.run([self.keygen, "-q", "-t", "rsa", "-b", "2048", "-N", "", "-f", str(path)],
                                capture_output=True, timeout=10)
        self.assertEqual(result.returncode, 0, "owned key generation failed")
        return path

    @contextmanager
    def tunnel(self, server, key=None, wrong_host_key=False):
        known = self.root / "known_hosts"
        host_key = server.host_key
        if wrong_host_key:
            import paramiko
            host_key = paramiko.RSAKey.generate(2048)
        known.write_text("[127.0.0.1]:%d %s %s\n" %
                         (server.ssh_port, host_key.get_name(), host_key.get_base64()))
        config = self.root / "config"
        config.write_text("Host *\n  BatchMode yes\n  IdentityAgent none\n  IdentitiesOnly yes\n"
                          "  StrictHostKeyChecking yes\n  UserKnownHostsFile \"%s\"\n"
                          "  GlobalKnownHostsFile none\n  ConnectTimeout 2\n" % known.as_posix())
        with socket.socket() as reserve:
            reserve.bind(("127.0.0.1", 0))
            port = reserve.getsockname()[1]
        command = [self.ssh, "-F", str(config), "-N", "-D", "127.0.0.1:%d" % port,
                   "-i", str(key or self.key), "-p", str(server.ssh_port), "-l", "progo-fixture",
                   "-o", "ExitOnForwardFailure=yes", "127.0.0.1"]
        process = subprocess.Popen(command, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL,
                                   stderr=subprocess.PIPE)
        try:
            yield process, port
        finally:
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(3)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(3)
            process.stderr.close()

    def connect_socks(self, process, port):
        deadline = time.monotonic() + 5
        while True:
            self.assertIsNone(process.poll(), "owned OpenSSH exited before SOCKS readiness")
            connection = None
            try:
                connection = socket.create_connection(("127.0.0.1", port), timeout=0.2)
                connection.settimeout(2)
                connection.sendall(b"\x05\x01\x00")
                self.assertEqual(self.read_exact(connection, 2), b"\x05\x00")
                return connection
            except OSError:
                if connection is not None:
                    connection.close()
                if time.monotonic() >= deadline:
                    self.fail("owned OpenSSH did not prepare SOCKS within five seconds")
                time.sleep(0.02)
            except BaseException:
                if connection is not None:
                    connection.close()
                raise

    @staticmethod
    def read_exact(connection, length):
        data = b""
        while len(data) < length:
            part = connection.recv(length - len(data))
            if not part:
                raise EOFError("owned SOCKS stream ended before its complete reply")
            data += part
        return data

    def test_real_publickey_socks_http_and_cleanup(self):
        with LoopbackSshFixture(self.key.with_suffix(".pub").read_text()) as server:
            with self.tunnel(server) as (process, port):
                with self.connect_socks(process, port) as connection:
                    connection.sendall(b"\x05\x01\x00\x01\x7f\x00\x00\x01" + struct.pack("!H", server.http_port))
                    reply = self.read_exact(connection, 10)
                    self.assertEqual(reply[:2], b"\x05\x00", "native SSH refused owned HTTP forwarding")
                    connection.sendall(b"GET / HTTP/1.0\r\nHost: fixture\r\n\r\n")
                    response = b""
                    while True:
                        part = connection.recv(4096)
                        if not part:
                            break
                        response += part
                    self.assertIn(server.payload, response)
                self.assertGreater(server.forwarded, 0)
            self.assertGreater(server.authenticated, 0, "key policy callback alone is not successful signature verification")
        self.assertTrue(all(not thread.is_alive() for thread in server.workers))

    def test_wrong_selected_key_is_rejected(self):
        other = self.make_key("wrong")
        with LoopbackSshFixture(self.key.with_suffix(".pub").read_text()) as server:
            with self.tunnel(server, key=other) as (process, _):
                self.assertNotEqual(process.wait(5), 0)
                self.assertIn(b"Permission denied", process.stderr.read())
            self.assertEqual(server.authenticated, 0)
            self.assertEqual(server.forwarded, 0)

    def test_changed_host_key_is_rejected(self):
        with LoopbackSshFixture(self.key.with_suffix(".pub").read_text()) as server:
            with self.tunnel(server, wrong_host_key=True) as (process, _):
                self.assertNotEqual(process.wait(5), 0)
                self.assertIn(b"HOST IDENTIFICATION HAS CHANGED", process.stderr.read())
            self.assertEqual(server.authenticated, 0)

    def test_unowned_destination_cannot_be_forwarded(self):
        with LoopbackSshFixture(self.key.with_suffix(".pub").read_text()) as server:
            with self.tunnel(server) as (process, port):
                with self.connect_socks(process, port) as connection:
                    connection.sendall(b"\x05\x01\x00\x01\xc0\x00\x02\x01\x00\x50")
                    reply = connection.recv(10)
                    self.assertTrue(not reply or reply[:2] != b"\x05\x00")
                self.assertEqual(server.forwarded, 0)
                self.assertGreater(server.denied_destinations, 0)

    def test_cleanup_interrupts_stalled_ssh_handshake(self):
        started = time.monotonic()
        # Keep the client open while server cleanup must interrupt its handshake.
        with socket.socket() as connection:
            connection.settimeout(2)
            with LoopbackSshFixture(self.key.with_suffix(".pub").read_text()) as server:
                connection.connect(("127.0.0.1", server.ssh_port))
                connection.settimeout(2)
                self.assertTrue(connection.recv(256).startswith(b"SSH-2.0-"))
        self.assertLess(time.monotonic() - started, 6)
        self.assertTrue(all(not thread.is_alive() for thread in server.workers))


if __name__ == "__main__":
    unittest.main()
