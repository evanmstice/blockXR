/**
 * Shared command executor — same semantics as Unity OSCController.ExecuteBlocks.
 * Accepts detector / Blockly-mapped names: Forward, Left, Right, Repeat until, End loop.
 */

/**
 * @param {import('./maze.js').Maze} maze
 * @param {string[]} blockList
 * @param {{loopCount?: number, onStatus?: (msg: string) => void}} options
 * @returns {Promise<'win'|'fail'|'empty'|'incomplete'>}
 */
export async function executeBlocks(maze, blockList, options = {}) {
  const loopCount = options.loopCount ?? 1;
  const onStatus = options.onStatus || (() => {});

  const commands = (blockList || [])
    .map((b) => String(b).trim())
    .filter(Boolean);

  if (commands.length === 0) {
    onStatus('No blocks detected.');
    return 'empty';
  }

  const hasRepeat = commands.includes('Repeat until');
  const hasEnd = commands.includes('End loop');
  if (hasRepeat && !hasEnd) {
    onStatus('Missing End loop block.');
    return 'incomplete';
  }

  maze.reset();
  onStatus(`Running: ${commands.join(' → ')}`);

  try {
    await runList(maze, commands, loopCount, false);
  } catch (err) {
    if (err && err.code === 'offPath') {
      onStatus('Off the path — try again.');
      return 'fail';
    }
    throw err;
  }

  if (maze.offPath) {
    onStatus('Off the path — try again.');
    return 'fail';
  }
  if (maze.atGoal) {
    onStatus('Goal reached!');
    return 'win';
  }
  onStatus('Program finished — not at the goal yet.');
  return 'fail';
}

/**
 * @param {import('./maze.js').Maze} maze
 * @param {string[]} blockList
 * @param {number} loopCount
 * @param {boolean} nested
 */
async function runList(maze, blockList, loopCount, nested) {
  let i = 0;
  while (i < blockList.length) {
    const block = blockList[i];

    if (block === 'Forward') {
      await maze.moveForward();
      if (maze.offPath) {
        const err = new Error('offPath');
        err.code = 'offPath';
        throw err;
      }
      await maze.pause(200);
      i++;
    } else if (block === 'Right') {
      maze.turnRight();
      await maze.pause();
      i++;
    } else if (block === 'Left') {
      maze.turnLeft();
      await maze.pause();
      i++;
    } else if (block === 'Repeat until') {
      const endLoopIndex = blockList.indexOf('End loop', i);
      if (endLoopIndex < 0) {
        const err = new Error('Missing End loop');
        err.code = 'incomplete';
        throw err;
      }
      const body = blockList.slice(i + 1, endLoopIndex);
      // Loop until goal (Blockly Games / "Repeat until" semantics).
      // loopCount is kept as a safety ceiling so a bad body can't spin forever.
      const maxIters = Math.max(loopCount, 100);
      let guard = 0;
      while (!maze.atGoal && !maze.offPath && guard < maxIters) {
        await runList(maze, body, loopCount, true);
        if (maze.offPath) {
          const err = new Error('offPath');
          err.code = 'offPath';
          throw err;
        }
        guard++;
      }
      i = endLoopIndex + 1;
    } else {
      // Skip unknown names (e.g. "When clicked")
      i++;
    }
  }
}

/**
 * Run digital Blockly-generated async code against the maze.
 * @param {import('./maze.js').Maze} maze
 * @param {string} code
 * @param {number} loopCount
 * @param {(msg: string) => void} [onStatus]
 */
export async function executeGeneratedCode(maze, code, loopCount, onStatus) {
  maze.reset();
  if (onStatus) onStatus('Running Blockly program…');

  const AsyncFunction = Object.getPrototypeOf(async function () {}).constructor;
  const fn = new AsyncFunction('maze', 'loopCount', code);

  try {
    await fn(maze, loopCount);
  } catch (err) {
    if (maze.offPath || (err && err.code === 'offPath')) {
      if (onStatus) onStatus('Off the path — try again.');
      return 'fail';
    }
    throw err;
  }

  if (maze.offPath) {
    if (onStatus) onStatus('Off the path — try again.');
    return 'fail';
  }
  if (maze.atGoal) {
    if (onStatus) onStatus('Goal reached!');
    return 'win';
  }
  if (onStatus) onStatus('Program finished — not at the goal yet.');
  return 'fail';
}
