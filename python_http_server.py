"""
HTTP API for the Blockly web frontend.
Shares the same BlockDetector instance as the OSC server.
"""

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
import uvicorn


def create_app(detector):
    app = FastAPI(title="blockXR CV API")

    app.add_middleware(
        CORSMiddleware,
        allow_origins=["*"],
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )

    @app.get("/health")
    def health():
        return {"ok": True}

    @app.post("/scan")
    def scan():
        blocks = detector.get_blocks()
        if not blocks:
            print("[HTTP] No blocks detected - returning empty list", flush=True)
            return {"blocks": []}

        # block[0] is the name, block[1] is the confidence
        names = [block[0] for block in blocks]
        print(f"[HTTP] Scan result: {names}", flush=True)
        return {"blocks": names}

    return app


def run_http_server(detector, host="127.0.0.1", port=8765):
    app = create_app(detector)
    print(f"HTTP Server Active on http://{host}:{port}", flush=True)
    uvicorn.run(app, host=host, port=port, log_level="info")
