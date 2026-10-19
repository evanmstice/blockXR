/**
 * blockXR dual-mode Blockly maze frontend.
 */

import * as Blockly from 'blockly';
import {javascriptGenerator} from 'blockly/javascript';

import {blocks} from './blocks/maze';
import {forBlock} from './generators/javascript';
import {toolbox} from './toolbox';
import {save, load} from './serialization';
import {levels, getLevel, getNextLevel} from './levels';
import {Maze} from './maze';
import {executeBlocks, executeGeneratedCode} from './executor';
import {
  scanBlocks,
  checkHealth,
  startCvBackend,
  stopCvBackend,
  waitForBackend,
} from './api';
import './index.css';

Blockly.common.defineBlocks(blocks);
Object.assign(javascriptGenerator.forBlock, forBlock);

const els = {
  start: document.getElementById('startScreen'),
  digital: document.getElementById('digitalScreen'),
  physical: document.getElementById('physicalScreen'),
  levelSelect: document.getElementById('levelSelect'),
  btnDigital: document.getElementById('btnDigital'),
  btnPhysical: document.getElementById('btnPhysical'),
  digitalBack: document.getElementById('digitalBack'),
  digitalRun: document.getElementById('digitalRun'),
  digitalReset: document.getElementById('digitalReset'),
  digitalStatus: document.getElementById('digitalStatus'),
  digitalLevelName: document.getElementById('digitalLevelName'),
  blocklyDiv: document.getElementById('blocklyDiv'),
  digitalCanvas: document.getElementById('digitalMaze'),
  physicalBack: document.getElementById('physicalBack'),
  physicalScan: document.getElementById('physicalScan'),
  physicalReset: document.getElementById('physicalReset'),
  physicalStatus: document.getElementById('physicalStatus'),
  physicalLevelName: document.getElementById('physicalLevelName'),
  physicalCanvas: document.getElementById('physicalMaze'),
  backendStatus: document.getElementById('backendStatus'),
};

/** @type {Blockly.WorkspaceSvg|null} */
let workspace = null;
/** @type {Maze|null} */
let digitalMaze = null;
/** @type {Maze|null} */
let physicalMaze = null;
let currentLevel = levels[0];
let running = false;

function populateLevels() {
  els.levelSelect.innerHTML = '';
  for (const level of levels) {
    const opt = document.createElement('option');
    opt.value = String(level.id);
    opt.textContent = `${level.id}. ${level.name}`;
    els.levelSelect.appendChild(opt);
  }
}

function selectedLevel() {
  return getLevel(Number(els.levelSelect.value));
}

function showScreen(name) {
  els.start.classList.toggle('hidden', name !== 'start');
  els.digital.classList.toggle('hidden', name !== 'digital');
  els.physical.classList.toggle('hidden', name !== 'physical');
}

function setStatus(el, msg) {
  el.textContent = msg || '';
}

function setRunning(isRunning, mode) {
  running = isRunning;
  if (mode === 'digital') {
    els.digitalRun.disabled = isRunning;
    els.digitalReset.disabled = isRunning;
  } else if (mode === 'physical') {
    els.physicalScan.disabled = isRunning;
    els.physicalReset.disabled = isRunning;
  }
}

function resizeCanvas(canvas, maze) {
  const parent = canvas.parentElement;
  if (!parent) return;
  const max = Math.min(parent.clientWidth - 24, parent.clientHeight - 24, 720);
  const size = Math.max(280, Math.floor(max));
  if (canvas.width !== size || canvas.height !== size) {
    canvas.width = size;
    canvas.height = size;
  }
  if (maze) maze._draw();
}

function ensureDigitalWorkspace() {
  if (workspace) {
    Blockly.svgResize(workspace);
    return;
  }
  workspace = Blockly.inject(els.blocklyDiv, {
    toolbox,
    trashcan: true,
    renderer: 'zelos',
    move: {scrollbars: true, drag: true, wheel: true},
  });
  load(workspace);
  workspace.addChangeListener((e) => {
    if (e.isUiEvent) return;
    save(workspace);
  });
  window.addEventListener('resize', () => {
    if (workspace) Blockly.svgResize(workspace);
    resizeCanvas(els.digitalCanvas, digitalMaze);
  });
}

function applyLevelToUi(mode, level, {clearWorkspace = false, statusMsg} = {}) {
  currentLevel = level;
  els.levelSelect.value = String(level.id);

  if (mode === 'digital') {
    els.digitalLevelName.textContent = `Level ${level.id}: ${level.name}`;
    setStatus(
      els.digitalStatus,
      statusMsg || 'Build a program, then press Run.',
    );
    if (!digitalMaze) {
      digitalMaze = new Maze(els.digitalCanvas, level);
    } else {
      digitalMaze.setLevel(level);
    }
    if (clearWorkspace && workspace) {
      workspace.clear();
    }
    requestAnimationFrame(() => {
      resizeCanvas(els.digitalCanvas, digitalMaze);
      if (workspace) Blockly.svgResize(workspace);
    });
  } else if (mode === 'physical') {
    els.physicalLevelName.textContent = `Level ${level.id}: ${level.name}`;
    setStatus(
      els.physicalStatus,
      statusMsg || 'Place blocks on the mat, then Scan & Run.',
    );
    if (!physicalMaze) {
      physicalMaze = new Maze(els.physicalCanvas, level);
    } else {
      physicalMaze.setLevel(level);
    }
    requestAnimationFrame(() =>
      resizeCanvas(els.physicalCanvas, physicalMaze),
    );
  }
}

/**
 * After a win, pause briefly then load the next level (or celebrate finish).
 * @param {'digital'|'physical'} mode
 */
async function advanceAfterWin(mode) {
  const statusEl =
    mode === 'digital' ? els.digitalStatus : els.physicalStatus;
  const next = getNextLevel(currentLevel.id);

  if (!next) {
    setStatus(statusEl, 'Goal reached! You finished all levels.');
    return;
  }

  setStatus(
    statusEl,
    `Goal reached! Loading level ${next.id}: ${next.name}…`,
  );
  await new Promise((resolve) => setTimeout(resolve, 1200));

  applyLevelToUi(mode, next, {
    clearWorkspace: mode === 'digital',
    statusMsg:
      mode === 'digital'
        ? `Level ${next.id}: build a new program, then Run.`
        : `Level ${next.id}: place blocks, then Scan & Run.`,
  });
}

async function enterDigital() {
  showScreen('digital');
  ensureDigitalWorkspace();
  applyLevelToUi('digital', selectedLevel());
}

async function enterPhysical() {
  showScreen('physical');
  applyLevelToUi('physical', selectedLevel());
  els.backendStatus.textContent = 'Backend: starting camera…';
  els.backendStatus.classList.remove('ok', 'bad');
  setStatus(els.physicalStatus, 'Starting camera — allow access if prompted…');
  els.physicalScan.disabled = true;

  try {
    const started = await startCvBackend();
    if (!started.ok) {
      throw new Error(started.error || 'Failed to start CV backend');
    }
    const ready = await waitForBackend(90000);
    if (!ready) {
      throw new Error('Camera/backend timed out — check the npm start terminal');
    }
    setStatus(els.physicalStatus, 'Place blocks on the mat, then Scan & Run.');
  } catch (err) {
    console.error(err);
    setStatus(els.physicalStatus, err.message || 'Could not start camera');
  } finally {
    els.physicalScan.disabled = false;
    updateBackendStatus();
  }
}

async function leavePhysical() {
  els.backendStatus.textContent = 'Backend: stopping…';
  await stopCvBackend();
  showScreen('start');
  els.backendStatus.textContent = '';
  els.backendStatus.classList.remove('ok', 'bad');
}

async function updateBackendStatus() {
  const ok = await checkHealth();
  els.backendStatus.textContent = ok
    ? 'Backend: camera ready'
    : 'Backend: offline';
  els.backendStatus.classList.toggle('ok', ok);
  els.backendStatus.classList.toggle('bad', !ok);
}

async function runDigital() {
  if (running || !digitalMaze || !workspace) return;
  setRunning(true, 'digital');
  try {
    const code = javascriptGenerator.workspaceToCode(workspace);
    if (!code.trim()) {
      setStatus(els.digitalStatus, 'Add some blocks first.');
      return;
    }
    const result = await executeGeneratedCode(
      digitalMaze,
      code,
      currentLevel.loopCount,
      (msg) => setStatus(els.digitalStatus, msg),
    );
    if (result === 'win') {
      await advanceAfterWin('digital');
    }
  } catch (err) {
    console.error(err);
    setStatus(els.digitalStatus, err.message || 'Error running program');
  } finally {
    setRunning(false, 'digital');
  }
}

async function scanAndRunPhysical() {
  if (running || !physicalMaze) return;
  setRunning(true, 'physical');
  try {
    setStatus(els.physicalStatus, 'Scanning…');
    const blocksDetected = await scanBlocks();
    const result = await executeBlocks(physicalMaze, blocksDetected, {
      loopCount: currentLevel.loopCount,
      onStatus: (msg) => setStatus(els.physicalStatus, msg),
    });
    if (result === 'win') {
      await advanceAfterWin('physical');
    }
  } catch (err) {
    console.error(err);
    setStatus(
      els.physicalStatus,
      err.message || 'Scan failed — is the Python backend running?',
    );
    updateBackendStatus();
  } finally {
    setRunning(false, 'physical');
  }
}

function goStart() {
  showScreen('start');
}

// Wire UI
populateLevels();

els.btnDigital.addEventListener('click', enterDigital);
els.btnPhysical.addEventListener('click', enterPhysical);
els.digitalBack.addEventListener('click', goStart);
els.physicalBack.addEventListener('click', leavePhysical);

window.addEventListener('beforeunload', () => {
  // Best-effort; may not always complete on tab close.
  navigator.sendBeacon?.('/backend/stop') ||
    fetch('/backend/stop', {method: 'POST', keepalive: true});
});
els.digitalRun.addEventListener('click', runDigital);
els.digitalReset.addEventListener('click', () => {
  if (running || !digitalMaze) return;
  digitalMaze.reset();
  setStatus(els.digitalStatus, 'Reset.');
});
els.physicalScan.addEventListener('click', scanAndRunPhysical);
els.physicalReset.addEventListener('click', () => {
  if (running || !physicalMaze) return;
  physicalMaze.reset();
  setStatus(els.physicalStatus, 'Reset.');
});

window.addEventListener('resize', () => {
  resizeCanvas(els.physicalCanvas, physicalMaze);
  resizeCanvas(els.digitalCanvas, digitalMaze);
});
