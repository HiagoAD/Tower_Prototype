#!/usr/bin/env python3
"""Read-only, loopback-only development dashboard. Python standard library only."""

import argparse
import hashlib
import json
import mimetypes
import re
import subprocess
from datetime import datetime, timezone
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import parse_qs, unquote, urlsplit

ROOT = Path(__file__).resolve().parents[2]
APP = Path(__file__).resolve().parent
DEV = "Docs/Development/"
REFERENCE = "Docs/Reference/Unity-technical-test/"
HASH_CACHE = {}


def read(root, path):
    file = root / path
    return file.read_text(encoding="utf-8") if file.is_file() else ""


def plain(value):
    return re.sub(r"[*`]", "", value).strip()


def table(text, first_header):
    """Read a Markdown table by its first column; ignore prose and other tables."""
    rows, active = [], False
    for line in text.splitlines():
        if not line.strip().startswith("|"):
            if active:
                break
            continue
        cells = [cell.strip() for cell in re.split(r"(?<!\\)\|", line.strip())[1:-1]]
        if not active:
            active = bool(cells and (cells[0] == first_header or
                                    (first_header == "Recife," and cells[0].startswith(first_header))))
        elif cells and not re.fullmatch(r"[:\-\s]+", cells[0]):
            rows.append(cells)
    return rows


def artifact(root, file):
    return {"path": file.relative_to(root).as_posix(), "name": file.name,
            "bytes": file.stat().st_size,
            "modified": datetime.fromtimestamp(file.stat().st_mtime, timezone.utc).isoformat()}


def files(root, directory, patterns):
    folder = root / directory
    found = {p for pattern in patterns for p in folder.glob(pattern) if p.is_file()
             and p.resolve().is_relative_to(root.resolve())}
    return [artifact(root, p) for p in sorted(found, key=lambda p: p.stat().st_mtime, reverse=True)]


def reference_integrity(root):
    folder = root / REFERENCE
    manifest = folder / "SHA256SUMS"
    if not manifest.exists():
        return {"ok": False, "files": 0, "issues": ["Checksum manifest missing"]}
    issues, count = [], 0
    for line in manifest.read_text().splitlines():
        match = re.match(r"^([a-fA-F0-9]{64})\s+[* ]?(.+)$", line)
        if not match:
            issues.append("Invalid checksum entry")
            continue
        expected, name = match.groups()
        file = (folder / name).resolve()
        if not file.is_relative_to(folder.resolve()) or not file.is_file():
            issues.append(name + ": missing or outside reference")
            continue
        stat = file.stat()
        key = (str(file), stat.st_mtime_ns, stat.st_ctime_ns, stat.st_size)
        if key not in HASH_CACHE:
            with file.open("rb") as stream:
                digest = hashlib.sha256()
                for block in iter(lambda: stream.read(1024 * 1024), b""):
                    digest.update(block)
            if len(HASH_CACHE) > 100:
                HASH_CACHE.clear()
            HASH_CACHE[key] = digest.hexdigest()
        if HASH_CACHE[key] != expected.lower():
            issues.append(name + ": checksum mismatch")
        count += 1
    return {"ok": count > 0 and not issues, "files": count, "issues": issues}


def snapshot(root=ROOT):
    status = read(root, DEV + "STATUS.md")
    plan = read(root, DEV + "ACTION_PLAN.md")
    brief = read(root, REFERENCE + "Unity-technical-test.md")
    issues = []
    if not status:
        issues.append("STATUS.md is missing or empty. Gate status is unavailable.")
    if not plan:
        issues.append("ACTION_PLAN.md is missing or empty. Plan details are unavailable.")
    date_match = re.search(r"Submission target:.*?(\d{4}-\d{2}-\d{2})\s+(\d{2}:\d{2})\s+Recife", status)
    target = date_match.group(1) + "T" + date_match.group(2) + ":00-03:00" if date_match else None
    date = date_match.group(1) if date_match else None
    deadline = re.search(r"interpreted as.*?(\d{4}-\d{2}-\d{2})\s+(\d{2}:\d{2})\s+UTC", status)
    hard = deadline.group(1) + "T" + deadline.group(2) + ":00Z" if deadline else None
    acceptance = {}
    for row in table(plan, "Recife,"):
        if len(row) >= 4:
            match = re.search(r"\*\*(G\d+):\*\*\s*(.*)", row[3])
            if match:
                acceptance[match.group(1)] = match.group(2)
    # Explicit current checkpoints supersede the original schedule's acceptance text.
    for row in table(plan, "Checkpoint"):
        if len(row) >= 2:
            acceptance[plain(row[0])] = row[1]
    roadmap = [{"order": plain(r[0]), "work": plain(r[1]), "evidence": plain(r[2])}
               for r in table(plan, "Order") if len(r) >= 3]
    priority = re.search(r"^Current implementation priority:\s*(.+)$", status, re.M)
    priority_text = re.sub(r"\[([^]]+)\]\([^)]+\)", r"\1", plain(priority.group(1))) if priority else ""
    gates = []
    for row in table(status, "Gate"):
        if len(row) < 4:
            continue
        match = re.match(r"(G\d+)\s*[—–-]\s*(.+)", plain(row[0]))
        if not match:
            continue
        gate_id, name = match.groups()
        reported_state = plain(row[2])
        state_match = re.fullmatch(r"(NOT VERIFIED|NOT COMPLETE|IN PROGRESS|PARTIAL|BLOCKED|PASS|FAIL)(?:\s*[—–:]\s*.*)?",
                                  reported_state.upper())
        state = state_match.group(1) if state_match else "NOT VERIFIED"
        if not state_match:
            issues.append(gate_id + " has an unrecognized status: " + reported_state)
        gates.append({"id": gate_id, "name": name, "time": plain(row[1]), "status": state, "reportedStatus": reported_state,
                      "due": date + "T" + plain(row[1]) + ":00-03:00" if date and re.fullmatch(r"\d{2}:\d{2}", plain(row[1])) else None,
                      "evidence": row[3], "acceptance": acceptance.get(gate_id, "Consult the action plan.")})
    if status and not gates:
        issues.append("No gate table could be read from STATUS.md. Check its Gate / Target / Status / Evidence table.")
    risks = [{"item": r[0], "owner": r[1], "action": r[2]} for r in table(status, "Item") if len(r) >= 3]
    quotas = [{"account": r[0], "session": r[1], "weekly": r[2], "notes": r[3]}
              for r in table(status, "Account / observation") if len(r) >= 4]
    levels = [{"name": r[0], "plan": r[1], "demonstration": r[2]}
              for r in table(plan, "Level") if len(r) >= 3]
    weights = [{"weight": r[0], "category": r[1], "description": r[2]}
               for r in table(brief, "Weight") if len(r) >= 3]
    reports = []
    for part in re.split(r"^### ", status, flags=re.M)[1:]:
        title, _, body = part.partition("\n")
        reports.append({"title": title.strip(), "body": body.strip().strip("`").removeprefix("text\n").strip()})
    docs = files(root, DEV, ["*.md"])
    docs += files(root, REFERENCE, ["Unity-technical-test.md"])
    docs += files(root, ".", ["README.md"])
    screenshots = files(root, DEV + "evidence", ["*.png", "*.jpg", "*.jpeg", "*.webp"])
    apks = files(root, "Builds", ["**/*.apk"])
    recordings = files(root, "Builds", ["**/*.mp4", "**/*.mov"])
    recordings += files(root, DEV + "evidence", ["*.mp4", "*.mov"])
    level_files = files(root, "Assets/Game/Levels", ["*.asset"])
    licenses = files(root, "Assets/Game/Art", ["**/*LICENSE*", "**/*License*", "**/*license*"])
    licenses = [f for f in licenses if not f["name"].endswith(".meta")]
    logs = files(root, "Logs", ["*.log", "*.txt"])
    source = root / (DEV + "STATUS.md")
    updated = datetime.fromtimestamp(source.stat().st_mtime, timezone.utc).isoformat() if source.exists() else None
    git = {"branch": "Unavailable", "revision": "Unavailable", "changed": None}
    try:
        result = subprocess.run(["git", "--no-optional-locks", "status", "--porcelain", "--branch", "-uno"],
                                cwd=root, capture_output=True, text=True, timeout=3, check=True)
        lines = result.stdout.splitlines()
        git["branch"] = lines[0].removeprefix("## ").split("...")[0] if lines else "Unknown"
        git["changed"] = len(lines[1:])
        revision = subprocess.run(["git", "rev-parse", "--short", "HEAD"], cwd=root,
                                  capture_output=True, text=True, timeout=3, check=True)
        git["revision"] = revision.stdout.strip()
    except (OSError, subprocess.SubprocessError):
        pass
    engine = re.search(r"m_EditorVersion:\s*(\S+)", read(root, "ProjectSettings/ProjectVersion.txt"))
    state = re.search(r"^Current state:\s*(.+)$", status, re.M)
    return {"fetchedAt": datetime.now(timezone.utc).isoformat(), "statusUpdated": updated,
            "sourceState": plain(state.group(1)) if state else "Not recorded",
            "priority": priority_text, "roadmap": roadmap,
            "target": target, "deadline": hard, "deadlineNote": "Interpreted deadline; date confirmation remains open in STATUS.md.",
            "gates": gates, "risks": risks, "quotas": quotas, "levels": levels,
            "weights": weights, "reports": reports, "documents": docs, "screenshots": screenshots,
            "apks": apks, "recordings": recordings, "levelFiles": level_files,
            "licenses": licenses, "logs": logs[:12], "git": git,
            "engine": engine.group(1) if engine else "Unknown", "integrity": reference_integrity(root), "issues": issues}


def allowed_file(root, path):
    file = (root / path).resolve()
    if not file.is_relative_to(root.resolve()) or not file.is_file():
        return None
    relative = file.relative_to(root.resolve()).as_posix()
    permitted = (relative.startswith("Docs/") or relative.startswith("Builds/")
                 or relative.startswith("Logs/") or relative == "README.md"
                 or (relative.startswith("Assets/Game/Art/") and "license" in file.name.lower()))
    extensions = {".md", ".txt", ".log", ".png", ".jpg", ".jpeg", ".webp", ".mp4", ".mov", ".apk", ".zip"}
    return file if permitted and file.suffix.lower() in extensions else None


class Handler(BaseHTTPRequestHandler):
    def log_message(self, fmt, *args):
        if args and str(args[1] if len(args) > 1 else "") not in ("200", "206"):
            super().log_message(fmt, *args)

    def headers_for(self, code, content_type, length):
        self.send_response(code)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(length))
        self.send_header("Cache-Control", "no-store")
        self.send_header("X-Content-Type-Options", "nosniff")
        self.send_header("Referrer-Policy", "no-referrer")
        self.send_header("Content-Security-Policy", "default-src 'self'; img-src 'self'; media-src 'self'; script-src 'self'; style-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'none'")

    def json(self, data, code=200):
        body = json.dumps(data).encode()
        self.headers_for(code, "application/json; charset=utf-8", len(body))
        self.end_headers()
        if self.command != "HEAD":
            self.wfile.write(body)

    def do_HEAD(self):
        self.do_GET()

    def do_GET(self):
        request = urlsplit(self.path)
        # Avoid serving project data to arbitrary Host headers (DNS rebinding).
        if self.headers.get("Host", "").split(":")[0] not in ("127.0.0.1", "localhost"):
            self.json({"error": "Local access only"}, 403)
            return
        if request.path == "/api/status":
            try:
                self.json(snapshot())
            except (OSError, ValueError) as error:
                self.json({"error": str(error)}, 503)
            return
        if request.path == "/api/document":
            path = parse_qs(request.query).get("path", [""])[0]
            file = allowed_file(ROOT, path)
            if file and file.suffix.lower() in (".md", ".txt", ".log"):
                # Keep a large build log from overwhelming the browser.
                with file.open("rb") as stream:
                    stream.seek(max(0, file.stat().st_size - 250_000))
                    content = stream.read().decode("utf-8", errors="replace")
                self.json({"path": path, "content": content, "truncated": file.stat().st_size > 250_000})
            else:
                self.json({"error": "Document not available"}, 404)
            return
        static = {"/": "index.html", "/index.html": "index.html", "/styles.css": "styles.css", "/app.js": "app.js"}
        file = APP / static[request.path] if request.path in static else None
        if request.path.startswith("/files/"):
            file = allowed_file(ROOT, unquote(request.path[7:]))
        if not file or not file.is_file():
            self.json({"error": "Not found"}, 404)
            return
        size = file.stat().st_size
        start, end, code = 0, size - 1, 200
        range_header = self.headers.get("Range")
        if range_header:
            match = re.fullmatch(r"bytes=(\d*)-(\d*)", range_header)
            if match and any(match.groups()):
                first, last = match.groups()
                start = int(first) if first else max(0, size - int(last))
                end = min(int(last), size - 1) if first and last else size - 1
            if not match or not any(match.groups()) or start > end or start >= size:
                self.headers_for(416, "text/plain", 0)
                self.send_header("Content-Range", "bytes */" + str(size))
                self.end_headers()
                return
            code = 206
        kind = mimetypes.guess_type(file.name)[0] or "application/octet-stream"
        if file.suffix in (".md", ".log"):
            kind = "text/plain; charset=utf-8"
        self.headers_for(code, kind, max(0, end - start + 1))
        self.send_header("Accept-Ranges", "bytes")
        if code == 206:
            self.send_header("Content-Range", f"bytes {start}-{end}/{size}")
        self.end_headers()
        if self.command == "HEAD":
            return
        try:
            with file.open("rb") as stream:
                stream.seek(start)
                remaining = end - start + 1
                while remaining > 0:
                    chunk = stream.read(min(remaining, 64 * 1024))
                    if not chunk:
                        break
                    self.wfile.write(chunk)
                    remaining -= len(chunk)
        except (BrokenPipeError, ConnectionResetError):
            pass


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, default=8765)
    args = parser.parse_args()
    server = ThreadingHTTPServer(("127.0.0.1", args.port), Handler)
    print(f"Tower development panel: http://127.0.0.1:{args.port}", flush=True)
    print("Reads project records every refresh. Ctrl+C stops the server.", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
