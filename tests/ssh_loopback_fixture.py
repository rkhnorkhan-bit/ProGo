"""Test-owned SSH/publickey + direct-tcpip server; never installs a service.

Paramiko verifies the actual SSH signature. The only forwarding destination is
this fixture's HTTP listener on 127.0.0.1. No sessions, commands, passwords,
system accounts, agent service or real VPS are used.
"""
import base64
import http.server
import json
import pathlib
import select
import socket
import threading
import time

import paramiko


class LoopbackSshFixture:
    payload = b"progo-owned-ssh-http-fixture\n"

    def __init__(self, authorized_key):
        if paramiko.__version__ != "4.0.0":
            raise RuntimeError("SSH fixture requires the pinned Paramiko 4.0.0 contract")
        fields = authorized_key.split()
        if len(fields) < 2:
            raise ValueError("fixture requires one OpenSSH public key")
        self.authorized_blob = base64.b64decode(fields[1], validate=True)
        self.host_key = paramiko.RSAKey.generate(2048)
        self.stop = threading.Event()
        self.lock = threading.Lock()
        self.transports = []
        self.connections = []
        self.workers = []
        self.authenticated = 0
        self.forwarded = 0
        self.denied_destinations = 0
        self.errors = []
        self.listener = socket.socket()
        try:
            self.listener.bind(("127.0.0.1", 0))
            self.listener.listen(8)
            self.listener.settimeout(0.2)
        except BaseException:
            self.listener.close()
            raise
        self.ssh_port = self.listener.getsockname()[1]
        outer = self

        class Handler(http.server.BaseHTTPRequestHandler):
            def do_GET(self):
                self.send_response(200)
                self.send_header("Content-Length", str(len(outer.payload)))
                self.end_headers()
                self.wfile.write(outer.payload)

            def log_message(self, *args):
                pass

        class HttpServer(http.server.ThreadingHTTPServer):
            def get_request(self):
                request, address = super().get_request()
                request.settimeout(2)
                return request, address

        try:
            self.http = HttpServer(("127.0.0.1", 0), Handler)
        except BaseException:
            self.listener.close()
            raise
        self.http.daemon_threads = False
        self.http_port = self.http.server_port
        self.http_thread = threading.Thread(target=self.http.serve_forever, daemon=True)
        self.accept_thread = threading.Thread(target=self._accept, daemon=True)

    def __enter__(self):
        self.http_thread.start()
        self.accept_thread.start()
        return self

    def metadata(self):
        return {"ssh_port": self.ssh_port, "http_port": self.http_port,
                "host_key": self.host_key.get_name() + " " + self.host_key.get_base64()}

    def _accept(self):
        while not self.stop.is_set():
            try:
                connection, _ = self.listener.accept()
            except socket.timeout:
                continue
            except OSError:
                if self.stop.is_set():
                    return
                with self.lock:
                    self.errors.append("listener failure")
                return
            worker = threading.Thread(target=self._connection, args=(connection,), daemon=True)
            with self.lock:
                self.connections.append(connection)
                self.workers.append(worker)
            worker.start()

    def _connection(self, connection):
        outer = self

        class Server(paramiko.ServerInterface):
            def get_allowed_auths(self, username):
                return "publickey"

            def check_auth_publickey(self, username, key):
                if username == "progo-fixture" and key.asbytes() == outer.authorized_blob:
                    # Paramiko subsequently verifies the signature itself.
                    return paramiko.AUTH_SUCCESSFUL
                return paramiko.AUTH_FAILED

            def check_channel_direct_tcpip_request(self, chanid, origin, destination):
                if destination == ("127.0.0.1", outer.http_port):
                    return paramiko.OPEN_SUCCEEDED
                with outer.lock:
                    outer.denied_destinations += 1
                return paramiko.OPEN_FAILED_ADMINISTRATIVELY_PROHIBITED

        transport = paramiko.Transport(connection)
        transport.banner_timeout = 2
        transport.handshake_timeout = 3
        transport.auth_timeout = 3
        with self.lock:
            self.transports.append(transport)
        try:
            transport.add_server_key(self.host_key)
            transport.start_server(server=Server())
            counted = False
            while not self.stop.is_set() and transport.is_active():
                if transport.is_authenticated() and not counted:
                    with self.lock:
                        self.authenticated += 1
                    counted = True
                channel = transport.accept(0.2)
                if channel is None:
                    continue
                # The callback permits only this owned listener, regardless of
                # the peer's requested origin or any ambient DNS/network state.
                with channel, socket.create_connection(("127.0.0.1", self.http_port), timeout=2) as target:
                    with self.lock:
                        self.forwarded += 1
                    channel.settimeout(2)
                    while not self.stop.is_set():
                        readable, _, _ = select.select([channel, target], [], [], 0.2)
                        ended = False
                        for source in readable:
                            data = source.recv(32768)
                            if not data:
                                ended = True
                                break
                            (target if source is channel else channel).sendall(data)
                        if ended:
                            break
        except (EOFError, OSError, paramiko.SSHException):
            # Client refusal, bad host key and explicit cancellation are normal
            # fixture scenarios. Their outcome is asserted by the caller.
            pass
        except Exception as error:
            with self.lock:
                self.errors.append(type(error).__name__)
        finally:
            transport.close()
            connection.close()

    def close(self):
        self.stop.set()
        self.listener.close()
        self.accept_thread.join(3)
        with self.lock:
            transports, workers, connections = list(self.transports), list(self.workers), list(self.connections)
        for connection in connections:
            try:
                connection.shutdown(socket.SHUT_RDWR)
            except OSError:
                pass
            connection.close()
        for transport in transports:
            transport.close()
        self.http.shutdown()
        self.http.server_close()
        self.http_thread.join(3)
        deadline = time.monotonic() + 3
        for worker in workers:
            worker.join(max(0, deadline - time.monotonic()))
        for transport in transports:
            if transport.ident is not None:
                transport.join(max(0, deadline - time.monotonic()))
        if self.accept_thread.is_alive() or self.http_thread.is_alive() or any(t.is_alive() for t in workers + transports):
            raise TimeoutError("owned SSH fixture cleanup did not settle within its deadline")
        if self.errors:
            raise AssertionError("owned SSH fixture worker failed: " + ",".join(self.errors))

    def __exit__(self, *args):
        self.close()


def main():
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("--public-key", required=True)
    parser.add_argument("--directory", required=True)
    args = parser.parse_args()
    directory = pathlib.Path(args.directory)
    # Caller creates its unique directory and owns every file below it.
    with LoopbackSshFixture(pathlib.Path(args.public_key).read_text()) as fixture:
        ready = directory / "ready.partial"
        ready.write_text(json.dumps(fixture.metadata()))
        ready.replace(directory / "ready.json")
        deadline = time.monotonic() + 90
        while not (directory / "stop").exists():
            if time.monotonic() >= deadline:
                raise TimeoutError("owned SSH fixture lifetime expired without explicit stop")
            time.sleep(0.05)
    print("OWNED_SSH_FIXTURE_CLEANUP_COMPLETE", flush=True)


if __name__ == "__main__":
    main()
