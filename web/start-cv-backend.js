/**
 * Spawns / stops main.py on demand for the web "Physical platform" mode.
 * Browser calls POST /backend/start and POST /backend/stop via the webpack
 * dev server — camera is not started with npm start alone.
 */
const {spawn} = require('child_process');
const fs = require('fs');
const path = require('path');

const REPO_ROOT = path.resolve(__dirname, '..');

let backend = null;

function resolvePython() {
  const win = process.platform === 'win32';
  const candidates = win
    ? [
        path.join(REPO_ROOT, '.venv', 'Scripts', 'python.exe'),
        path.join(REPO_ROOT, 'venv', 'Scripts', 'python.exe'),
        'python',
      ]
    : [
        path.join(REPO_ROOT, '.venv', 'bin', 'python'),
        path.join(REPO_ROOT, 'venv', 'bin', 'python'),
        '/opt/homebrew/bin/python3.13',
        '/opt/homebrew/bin/python3',
        'python3',
      ];

  for (const c of candidates) {
    if (c === 'python' || c === 'python3') return c;
    if (fs.existsSync(c)) return c;
  }
  return win ? 'python' : 'python3';
}

function isRunning() {
  return !!(backend && backend.exitCode === null && !backend.killed);
}

function stopBackend() {
  if (!backend) return {ok: true, running: false};
  const child = backend;
  backend = null;
  try {
    if (process.platform === 'win32') {
      spawn('taskkill', ['/pid', String(child.pid), '/f', '/t']);
    } else {
      child.kill('SIGTERM');
      setTimeout(() => {
        try {
          if (!child.killed) child.kill('SIGKILL');
        } catch {
          /* ignore */
        }
      }, 2500);
    }
  } catch {
    /* ignore */
  }
  return {ok: true, running: false};
}

function startBackend() {
  if (isRunning()) return {ok: true, running: true, already: true};

  const mainPy = path.join(REPO_ROOT, 'main.py');
  if (!fs.existsSync(mainPy)) {
    return {ok: false, error: 'main.py not found', running: false};
  }

  const python = resolvePython();
  console.log(`[cv-backend] starting: ${python} ${mainPy}`);

  const child = spawn(python, [mainPy], {
    cwd: REPO_ROOT,
    env: {...process.env, PYTHONUNBUFFERED: '1'},
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  backend = child;

  const prefix = (chunk) => {
    String(chunk)
      .split(/\r?\n/)
      .filter(Boolean)
      .forEach((line) => console.log('[cv-backend]', line));
  };
  child.stdout.on('data', prefix);
  child.stderr.on('data', prefix);
  child.on('exit', (code, signal) => {
    console.log(`[cv-backend] exited code=${code} signal=${signal || ''}`);
    if (backend === child) backend = null;
  });

  return {ok: true, running: true, pid: child.pid};
}

function sendJson(res, status, body) {
  res.statusCode = status;
  res.setHeader('Content-Type', 'application/json');
  res.end(JSON.stringify(body));
}

function attachBackendControl(middlewares, _devServer) {
  middlewares.unshift({
    name: 'blockxr-cv-backend-control',
    middleware: (req, res, next) => {
      const url = (req.url || '').split('?')[0];
      if (url === '/backend/start' && (req.method === 'POST' || req.method === 'GET')) {
        return sendJson(res, 200, startBackend());
      }
      if (url === '/backend/stop' && (req.method === 'POST' || req.method === 'GET')) {
        return sendJson(res, 200, stopBackend());
      }
      if (url === '/backend/status' && req.method === 'GET') {
        return sendJson(res, 200, {ok: true, running: isRunning()});
      }
      return next();
    },
  });

  const shutdown = () => stopBackend();
  process.on('exit', shutdown);
  process.on('SIGINT', () => {
    stopBackend();
    process.exit(0);
  });
  process.on('SIGTERM', () => {
    stopBackend();
    process.exit(0);
  });

  return middlewares;
}

module.exports = {attachBackendControl, startBackend, stopBackend, isRunning};
