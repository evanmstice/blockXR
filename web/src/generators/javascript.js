/**
 * Generators for maze blocks — emit async calls against the shared Maze API.
 */

export const forBlock = Object.create(null);

forBlock['bxr_forward'] = function () {
  return (
    'await maze.moveForward();\n' +
    'if (maze.offPath) { const e = new Error("offPath"); e.code = "offPath"; throw e; }\n'
  );
};

forBlock['bxr_left'] = function () {
  return 'maze.turnLeft();\nawait maze.pause();\n';
};

forBlock['bxr_right'] = function () {
  return 'maze.turnRight();\nawait maze.pause();\n';
};

forBlock['bxr_repeat_until'] = function (block, generator) {
  const branch = generator.statementToCode(block, 'DO');
  // Match Blockly Games maze: loop until at the goal (or off the path).
  return (
    '{\n' +
    '  let __guard = 0;\n' +
    '  while (!maze.atGoal && !maze.offPath && __guard < 100) {\n' +
    branch +
    '    __guard++;\n' +
    '  }\n' +
    '}\n'
  );
};
