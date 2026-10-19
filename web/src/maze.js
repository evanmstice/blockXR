/**
 * Canvas maze with step-based movement matching Unity PlayerMovement semantics.
 */

const DIRS = [
  {dr: -1, dc: 0}, // North
  {dr: 0, dc: 1}, // East
  {dr: 1, dc: 0}, // South
  {dr: 0, dc: -1}, // West
];

const STEP_MS = 320;
const TURN_MS = 120;

export class Maze {
  /**
   * @param {HTMLCanvasElement} canvas
   * @param {object} level
   */
  constructor(canvas, level) {
    this.canvas = canvas;
    this.ctx = canvas.getContext('2d');
    this.level = level;
    this.offPath = false;
    this.isMoving = false;
    this.reset();
  }

  setLevel(level) {
    this.level = level;
    this.reset();
  }

  reset() {
    const {start} = this.level;
    this.r = start.r;
    this.c = start.c;
    this.dir = start.dir;
    this.offPath = false;
    this.isMoving = false;
    this._draw();
  }

  get atGoal() {
    const {goal} = this.level;
    return this.r === goal.r && this.c === goal.c && !this.offPath;
  }

  turnLeft() {
    this.dir = (this.dir + 3) % 4;
    this._draw();
  }

  turnRight() {
    this.dir = (this.dir + 1) % 4;
    this._draw();
  }

  async moveForward() {
    if (this.offPath) return;
    this.isMoving = true;

    const {dr, dc} = DIRS[this.dir];
    const nextR = this.r + dr;
    const nextC = this.c + dc;
    const from = {r: this.r, c: this.c};
    const to = {r: nextR, c: nextC};

    await this._animateStep(from, to);

    this.r = nextR;
    this.c = nextC;

    if (!this._isPath(this.r, this.c)) {
      this.offPath = true;
    }

    this.isMoving = false;
    this._draw();
  }

  async _animateStep(from, to) {
    const start = performance.now();
    return new Promise((resolve) => {
      const tick = (now) => {
        const t = Math.min(1, (now - start) / STEP_MS);
        this._draw(from, to, t);
        if (t < 1) {
          requestAnimationFrame(tick);
        } else {
          resolve();
        }
      };
      requestAnimationFrame(tick);
    });
  }

  _isPath(r, c) {
    const {grid, rows, cols} = this.level;
    if (r < 0 || c < 0 || r >= rows || c >= cols) return false;
    return grid[r][c] === 1;
  }

  _cellSize() {
    const pad = 16;
    const w = this.canvas.width - pad * 2;
    const h = this.canvas.height - pad * 2;
    return {
      size: Math.min(w / this.level.cols, h / this.level.rows),
      pad,
    };
  }

  _cellOrigin(r, c, size, pad) {
    const gridW = size * this.level.cols;
    const gridH = size * this.level.rows;
    const ox = (this.canvas.width - gridW) / 2;
    const oy = (this.canvas.height - gridH) / 2;
    return {
      x: ox + c * size,
      y: oy + r * size,
    };
  }

  /**
   * @param {{r:number,c:number}=} from
   * @param {{r:number,c:number}=} to
   * @param {number=} t
   */
  _draw(from, to, t = 1) {
    const ctx = this.ctx;
    const {width, height} = this.canvas;
    ctx.clearRect(0, 0, width, height);

    ctx.fillStyle = '#1a2332';
    ctx.fillRect(0, 0, width, height);

    const {size, pad} = this._cellSize();
    const {grid, rows, cols, goal} = this.level;

    for (let r = 0; r < rows; r++) {
      for (let c = 0; c < cols; c++) {
        const {x, y} = this._cellOrigin(r, c, size, pad);
        if (grid[r][c] === 1) {
          ctx.fillStyle = '#3d5a46';
          ctx.fillRect(x + 1, y + 1, size - 2, size - 2);
        } else {
          ctx.fillStyle = '#243044';
          ctx.fillRect(x + 1, y + 1, size - 2, size - 2);
        }
      }
    }

    // Goal
    {
      const {x, y} = this._cellOrigin(goal.r, goal.c, size, pad);
      ctx.fillStyle = '#e8b84a';
      ctx.beginPath();
      ctx.arc(x + size / 2, y + size / 2, size * 0.22, 0, Math.PI * 2);
      ctx.fill();
    }

    // Player
    let pr = this.r;
    let pc = this.c;
    if (from && to && t < 1) {
      pr = from.r + (to.r - from.r) * t;
      pc = from.c + (to.c - from.c) * t;
    }
    const {x, y} = this._cellOrigin(pr, pc, size, pad);
    const cx = x + size / 2;
    const cy = y + size / 2;
    const rad = size * 0.28;

    ctx.fillStyle = this.offPath ? '#c45c5c' : '#6ec3ff';
    ctx.beginPath();
    ctx.arc(cx, cy, rad, 0, Math.PI * 2);
    ctx.fill();

    // Facing notch
    const {dr, dc} = DIRS[this.dir];
    ctx.fillStyle = '#0d1520';
    ctx.beginPath();
    ctx.arc(cx + dc * rad * 0.55, cy + dr * rad * 0.55, rad * 0.28, 0, Math.PI * 2);
    ctx.fill();
  }

  /** Brief pause used between turns so motion is readable. */
  async pause(ms = TURN_MS) {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }
}
