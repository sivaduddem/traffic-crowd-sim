const canvas = document.getElementById("canvas");
const ctx = canvas.getContext("2d");
const statsEl = document.getElementById("stats");
const scrubber = document.getElementById("scrubber");
const frameLabel = document.getElementById("frameLabel");
const runBtn = document.getElementById("runBtn");
const playBtn = document.getElementById("playBtn");
const pauseBtn = document.getElementById("pauseBtn");

const CELL = 72;
const PAD = 48;

let recording = null;
let frameIndex = 0;
let playing = false;
let playTimer = null;
let edgePositions = new Map();

function nodeXY(nodeId, cols) {
  const r = Math.floor(nodeId / cols);
  const c = nodeId % cols;
  return { x: PAD + c * CELL, y: PAD + r * CELL };
}

function lerp(a, b, t) {
  return a + (b - a) * t;
}

function congestionColor(ratio) {
  const t = Math.min(1, ratio);
  const r = Math.round(lerp(46, 230, t));
  const g = Math.round(lerp(160, 72, t));
  const b = Math.round(lerp(120, 60, t));
  return `rgb(${r},${g},${b})`;
}

function agentXY(agent, cols, edges) {
  if (agent.currentEdgeId != null && agent.isActive) {
    const edge = edges.find((e) => e.id === agent.currentEdgeId);
    if (edge) {
      const from = nodeXY(edge.fromNodeId, cols);
      const to = nodeXY(edge.toNodeId, cols);
      const t = agent.progressOnEdge;
      return { x: lerp(from.x, to.x, t), y: lerp(from.y, to.y, t) };
    }
  }
  return nodeXY(agent.currentNodeId, cols);
}

function drawFrame(frame) {
  if (!recording) return;

  const cols = recording.gridCols;
  const rows = recording.gridRows;
  ctx.clearRect(0, 0, canvas.width, canvas.height);

  // Roads
  for (const edge of frame.edges) {
    const from = nodeXY(edge.fromNodeId, cols);
    const to = nodeXY(edge.toNodeId, cols);
    const ratio = edge.occupancyRatio;

    ctx.lineWidth = 4 + ratio * 10;
    ctx.strokeStyle = congestionColor(ratio);
    ctx.globalAlpha = 0.35 + ratio * 0.5;
    ctx.beginPath();
    ctx.moveTo(from.x, from.y);
    ctx.lineTo(to.x, to.y);
    ctx.stroke();
    ctx.globalAlpha = 1;
  }

  // Intersections
  for (let id = 0; id < rows * cols; id++) {
    const { x, y } = nodeXY(id, cols);
    ctx.fillStyle = "#2a3544";
    ctx.beginPath();
    ctx.arc(x, y, 7, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = "#4a5d78";
    ctx.lineWidth = 1.5;
    ctx.stroke();
  }

  // Agents
  const active = frame.agents.filter((a) => a.isActive);
  for (const agent of active) {
    const pos = agentXY(agent, cols, frame.edges);
    const dest = nodeXY(agent.destinationNodeId, cols);

    ctx.fillStyle = "#4fc3f7";
    ctx.beginPath();
    ctx.arc(pos.x, pos.y, 5, 0, Math.PI * 2);
    ctx.fill();

    ctx.strokeStyle = "rgba(129,199,132,0.5)";
    ctx.setLineDash([3, 4]);
    ctx.beginPath();
    ctx.moveTo(pos.x, pos.y);
    ctx.lineTo(dest.x, dest.y);
    ctx.stroke();
    ctx.setLineDash([]);
  }

  // Legend
  ctx.fillStyle = "#8b9cb3";
  ctx.font = "12px Segoe UI, sans-serif";
  ctx.fillText("Edge heat = occupancy / capacity", PAD, canvas.height - 16);
  ctx.fillText(`Policy: ${recording.policyName}`, PAD, 24);

  statsEl.textContent =
    `t=${frame.timestep} · active=${frame.activeAgents} · pending=${frame.pendingAgents} · ` +
    `completed=${frame.totalCompletions} (+${frame.completionsThisStep} this step)`;

  frameLabel.textContent = `t = ${frame.timestep}`;
  scrubber.value = frameIndex;
}

function setFrame(index) {
  if (!recording?.frames?.length) return;
  frameIndex = Math.max(0, Math.min(index, recording.frames.length - 1));
  drawFrame(recording.frames[frameIndex]);
}

function stopPlay() {
  playing = false;
  if (playTimer) {
    clearInterval(playTimer);
    playTimer = null;
  }
}

function startPlay() {
  if (!recording?.frames?.length) return;
  stopPlay();
  playing = true;
  playTimer = setInterval(() => {
    if (frameIndex >= recording.frames.length - 1) {
      stopPlay();
      return;
    }
    setFrame(frameIndex + 1);
  }, 80);
}

async function runSimulation() {
  runBtn.disabled = true;
  statsEl.textContent = "Running simulation…";
  stopPlay();

  const params = new URLSearchParams({
    policy: document.getElementById("policy").value,
    agents: document.getElementById("agents").value,
    timesteps: document.getElementById("timesteps").value,
    capacity: document.getElementById("capacity").value,
  });

  try {
    const res = await fetch(`/api/simulate?${params}`);
    if (!res.ok) throw new Error(await res.text());
    recording = await res.json();
    scrubber.max = Math.max(0, recording.frames.length - 1);
    scrubber.disabled = false;
    playBtn.disabled = false;
    pauseBtn.disabled = false;
    setFrame(0);
    startPlay();
  } catch (err) {
    statsEl.textContent = `Error: ${err.message}`;
  } finally {
    runBtn.disabled = false;
  }
}

runBtn.addEventListener("click", runSimulation);
playBtn.addEventListener("click", startPlay);
pauseBtn.addEventListener("click", stopPlay);
scrubber.addEventListener("input", () => {
  stopPlay();
  setFrame(Number(scrubber.value));
});
