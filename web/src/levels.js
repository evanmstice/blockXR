/**
 * Maze levels adapted from Google Blockly Games Maze
 * (appengine/maze/src/main.js).
 *
 * Blockly encoding: 0=wall, 1=open, 2=start, 3=finish.
 * Ours: grid 1=path / 0=wall, plus start/goal coords.
 * Start facing East (dir: 1) to match Blockly Games.
 */

/** @type {number[][][]} */
const BLOCKLY_MAPS = [
  // Level 1 — one step forward
  [
    [0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 0, 0],
    [0, 0, 2, 1, 3, 0, 0],
    [0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 0, 0],
  ],
  // Level 2 — turn then forward
  [
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 1, 3, 0, 0, 0],
    [0, 0, 2, 1, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
  ],
  // Level 3 — long corridor (repeat)
  [
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 2, 1, 1, 1, 1, 3, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
  ],
  // Level 4 — zig-zag with extras past start/goal
  [
    [0, 0, 0, 0, 0, 0, 0, 1],
    [0, 0, 0, 0, 0, 0, 1, 1],
    [0, 0, 0, 0, 0, 3, 1, 0],
    [0, 0, 0, 0, 1, 1, 0, 0],
    [0, 0, 0, 1, 1, 0, 0, 0],
    [0, 0, 1, 1, 0, 0, 0, 0],
    [0, 2, 1, 0, 0, 0, 0, 0],
    [1, 1, 0, 0, 0, 0, 0, 0],
  ],
  // Level 5 — around the bend
  [
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 3, 0, 0],
    [0, 0, 0, 0, 0, 1, 0, 0],
    [0, 0, 0, 0, 0, 1, 0, 0],
    [0, 0, 0, 0, 0, 1, 0, 0],
    [0, 0, 0, 0, 0, 1, 0, 0],
    [0, 0, 0, 2, 1, 1, 0, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
  ],
  // Level 6 — inner goal (if/path helps; still solvable by sequence)
  [
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 1, 1, 1, 1, 1, 0, 0],
    [0, 1, 0, 0, 0, 1, 0, 0],
    [0, 1, 1, 3, 0, 1, 0, 0],
    [0, 0, 0, 0, 0, 1, 0, 0],
    [0, 2, 1, 1, 1, 1, 0, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
  ],
  // Level 7
  [
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 1, 1, 0],
    [0, 2, 1, 1, 1, 1, 0, 0],
    [0, 0, 0, 0, 0, 1, 1, 0],
    [0, 1, 1, 3, 0, 1, 0, 0],
    [0, 1, 0, 1, 0, 1, 0, 0],
    [0, 1, 1, 1, 1, 1, 1, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
  ],
  // Level 8
  [
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 1, 1, 1, 1, 0, 0, 0],
    [0, 1, 0, 0, 1, 1, 0, 0],
    [0, 1, 1, 1, 0, 1, 0, 0],
    [0, 0, 0, 1, 0, 1, 0, 0],
    [0, 2, 1, 1, 0, 3, 0, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
  ],
  // Level 9
  [
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 1, 1, 1, 1, 1, 0, 0],
    [0, 0, 1, 0, 0, 0, 0, 0],
    [3, 1, 1, 1, 1, 1, 1, 0],
    [0, 1, 0, 1, 0, 1, 1, 0],
    [1, 1, 1, 1, 1, 0, 1, 0],
    [0, 1, 0, 1, 0, 2, 1, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
  ],
  // Level 10
  [
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 1, 1, 0, 3, 0, 1, 0],
    [0, 1, 1, 0, 1, 1, 1, 0],
    [0, 1, 0, 1, 0, 1, 0, 0],
    [0, 1, 1, 1, 1, 1, 1, 0],
    [0, 0, 0, 1, 0, 0, 1, 0],
    [0, 2, 1, 1, 1, 0, 1, 0],
    [0, 0, 0, 0, 0, 0, 0, 0],
  ],
];

const LEVEL_NAMES = [
  'One step',
  'Turn the corner',
  'Long corridor',
  'Zigzag',
  'Around the bend',
  'Inside the loop',
  'Nested paths',
  'Spiral in',
  'Complex maze',
  'Final challenge',
];

/**
 * Convert a Blockly Games map into our level format.
 * @param {number[][]} raw
 * @param {number} id 1-based
 * @param {string} name
 */
function fromBlocklyMap(raw, id, name) {
  let start = null;
  let goal = null;
  const grid = raw.map((row, r) =>
    row.map((cell, c) => {
      if (cell === 2) {
        start = {r, c, dir: 1}; // East — Blockly Games default
        return 1;
      }
      if (cell === 3) {
        goal = {r, c};
        return 1;
      }
      return cell === 1 ? 1 : 0;
    }),
  );

  if (!start || !goal) {
    throw new Error(`Blockly map ${id} missing start or finish`);
  }

  return {
    id,
    name,
    rows: raw.length,
    cols: raw[0].length,
    grid,
    start,
    goal,
    loopCount: 100,
  };
}

export const levels = BLOCKLY_MAPS.map((raw, i) =>
  fromBlocklyMap(raw, i + 1, LEVEL_NAMES[i]),
);

export function getLevel(id) {
  return levels.find((level) => level.id === id) || levels[0];
}

/** @returns {object|null} next level, or null if finished */
export function getNextLevel(currentId) {
  const idx = levels.findIndex((level) => level.id === currentId);
  if (idx < 0 || idx >= levels.length - 1) return null;
  return levels[idx + 1];
}
