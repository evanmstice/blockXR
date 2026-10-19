/**
 * Custom blocks matching physical blockXR class names.
 */

import * as Blockly from 'blockly/core';

const bxrForward = {
  type: 'bxr_forward',
  message0: 'Forward',
  previousStatement: null,
  nextStatement: null,
  colour: 210,
  tooltip: 'Move one step forward',
  helpUrl: '',
};

const bxrLeft = {
  type: 'bxr_left',
  message0: 'Left',
  previousStatement: null,
  nextStatement: null,
  colour: 160,
  tooltip: 'Turn left',
  helpUrl: '',
};

const bxrRight = {
  type: 'bxr_right',
  message0: 'Right',
  previousStatement: null,
  nextStatement: null,
  colour: 160,
  tooltip: 'Turn right',
  helpUrl: '',
};

// C-shaped block (message1 holds the statement input) — same pattern as
// Blockly's controls_whileUntil / maze "repeat until".
const bxrRepeatUntil = {
  type: 'bxr_repeat_until',
  message0: 'Repeat until',
  message1: 'do %1',
  args1: [
    {
      type: 'input_statement',
      name: 'DO',
    },
  ],
  previousStatement: null,
  nextStatement: null,
  colour: 120,
  tooltip: 'Repeat the enclosed blocks until the goal is reached',
  helpUrl: '',
};

export const blocks = Blockly.common.createBlockDefinitionsFromJsonArray([
  bxrForward,
  bxrLeft,
  bxrRight,
  bxrRepeatUntil,
]);
