/**
 * Talk to the Python CV backend HTTP API and the webpack control endpoints
 * that start/stop main.py for Physical platform mode.
 */

const API_BASE = '';

export async function checkHealth() {
  try {
    const res = await fetch(`${API_BASE}/health`, {method: 'GET'});
    if (!res.ok) return false;
    const data = await res.json();
    return !!data.ok;
  } catch {
    return false;
  }
}

/**
 * Ask the webpack dev server to spawn main.py (opens the camera).
 * @returns {Promise<{ok: boolean, running?: boolean, error?: string}>}
 */
export async function startCvBackend() {
  const res = await fetch(`${API_BASE}/backend/start`, {method: 'POST'});
  if (!res.ok) {
    throw new Error(`Could not start CV backend (${res.status})`);
  }
  return res.json();
}

/**
 * Stop main.py and release the camera.
 */
export async function stopCvBackend() {
  try {
    const res = await fetch(`${API_BASE}/backend/stop`, {method: 'POST'});
    if (!res.ok) return {ok: false};
    return res.json();
  } catch {
    return {ok: false};
  }
}

/**
 * Wait until /health succeeds or timeout.
 * @param {number} timeoutMs
 */
export async function waitForBackend(timeoutMs = 90000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    if (await checkHealth()) return true;
    await new Promise((r) => setTimeout(r, 400));
  }
  return false;
}

/**
 * @returns {Promise<string[]>}
 */
export async function scanBlocks() {
  const res = await fetch(`${API_BASE}/scan`, {method: 'POST'});
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Scan failed (${res.status}): ${text}`);
  }
  const data = await res.json();
  return Array.isArray(data.blocks) ? data.blocks : [];
}
