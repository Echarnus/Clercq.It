"""Fake production site for smoke-test.test.sh. MODE picks the behaviour."""
import json
import os
import sys
from http.server import BaseHTTPRequestHandler, HTTPServer

MODE = os.environ.get("MODE", "healthy")


class Handler(BaseHTTPRequestHandler):
    def log_message(self, *args):
        pass

    def send(self, status, body, content_type):
        data = body.encode()
        self.send_response(status)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        if self.path == "/version.json":
            version = "0.0.1" if MODE == "old-version" else "1.2.3"
            self.send(200, json.dumps({"version": version}), "application/json")
        elif self.path == "/":
            if MODE == "broken-home":
                self.send(502, "Bad Gateway", "text/html")
            else:
                self.send(200, "<html>ok</html>", "text/html")
        elif self.path == "/api/projects":
            if MODE == "broken-api":
                self.send(500, json.dumps({"error": "db down"}), "application/json")
            elif MODE == "html-api":
                self.send(200, "<html>not json</html>", "text/html")
            else:
                self.send(200, json.dumps([]), "application/json")
        else:
            self.send(404, "not found", "text/plain")


if __name__ == "__main__":
    HTTPServer(("127.0.0.1", int(sys.argv[1])), Handler).serve_forever()
