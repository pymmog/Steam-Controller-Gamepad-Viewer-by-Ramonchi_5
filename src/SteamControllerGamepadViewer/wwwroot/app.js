const params = new URLSearchParams(window.location.search);
document.body.classList.toggle("solid", params.get("bg") === "solid");
document.body.classList.toggle("debug", params.has("debug"));
document.body.classList.toggle("clean", params.has("clean"));
document.body.classList.toggle("hide-title", params.get("title") === "0");
document.body.classList.toggle("preview-all", params.get("preview") === "all");

const trackpadPressPressure = 0.05;

const controls = new Map();
for (const element of document.querySelectorAll("[data-control]")) {
  const name = element.dataset.control;
  if (!controls.has(name)) {
    controls.set(name, []);
  }
  controls.get(name).push(element);
}
const sticks = {
  left: {
    dot: document.querySelector('[data-stick-dot="left"]'),
  },
  right: {
    dot: document.querySelector('[data-stick-dot="right"]'),
  },
};
const pads = {
  left: {
    root: document.querySelector('[data-pad="left"]'),
    dot: document.querySelector('[data-pad-dot="left"]'),
    ring: document.querySelector('[data-pad-ring="left"]'),
    x: 103.296,
    y: 139.723,
    size: 93,
    mirroredX: false,
  },
  right: {
    root: document.querySelector('[data-pad="right"]'),
    dot: document.querySelector('[data-pad-dot="right"]'),
    ring: document.querySelector('[data-pad-ring="right"]'),
    x: -0.81371,
    y: 1.15667,
    size: 93,
    mirroredX: true,
  },
};
const triggers = {
  left: {
    fill: document.querySelector('[data-trigger-fill-rect="lt"]'),
    fade: document.querySelector('[data-trigger-fade-rect="lt"]'),
    top: -42,
    bottom: 38,
    transition: 8,
  },
  right: {
    fill: document.querySelector('[data-trigger-fill-rect="rt"]'),
    fade: document.querySelector('[data-trigger-fade-rect="rt"]'),
    top: -42,
    bottom: 38,
    transition: 8,
  },
};
const statusElement = document.querySelector("[data-status]");
const titleElement = document.querySelector("[data-title]");

let lastStateAt = performance.now();

function setActive(name, active, strength = 1) {
  const elements = controls.get(name);
  if (!elements) {
    return;
  }

  for (const element of elements) {
    element.classList.toggle("active", Boolean(active));
    element.style.setProperty("--press", clamp01(strength).toFixed(3));
  }
}

function setStick(name, x, y, pressed) {
  const stick = sticks[name];
  if (!stick?.dot) {
    return;
  }

  const nx = clampSigned(x);
  const ny = clampSigned(y);
  const moved = Math.hypot(nx, ny) > 0.08;
  stick.dot.classList.toggle("moved", moved);
  stick.dot.classList.toggle("pressed", Boolean(pressed));
  stick.dot.setAttribute("transform", `translate(${(nx * 23).toFixed(2)} ${(ny * 23).toFixed(2)})`);
  setActive(name === "left" ? "left-stick-press" : "right-stick-press", pressed);
}

function setTouchpad(name, pad) {
  const view = pads[name];
  if (!view) {
    return;
  }

  const pressure = clamp01(pad?.pressure ?? 0);
  const touched = Boolean(pad?.touched) || pressure > 0.02;
  const trackerExpanded = pressure >= trackpadPressPressure;
  const clicked = touched && trackerExpanded;
  view.root?.classList.toggle("touched", touched);
  view.root?.classList.toggle("pressed", clicked);

  const normalizedX = clamp01(pad?.x ?? 0.5);
  const localX = view.x + (view.mirroredX ? 1 - normalizedX : normalizedX) * view.size;
  const localY = view.y + clamp01(pad?.y ?? 0.5) * view.size;
  const radius = 12 + (trackerExpanded ? pressure * 8 : 0);

  view.dot?.setAttribute("cx", localX.toFixed(2));
  view.dot?.setAttribute("cy", localY.toFixed(2));
  view.ring?.setAttribute("cx", localX.toFixed(2));
  view.ring?.setAttribute("cy", localY.toFixed(2));
  view.ring?.setAttribute("r", radius.toFixed(2));
}

function setTrigger(name, value) {
  const depth = clamp01(value);
  const controlName = name === "left" ? "lt" : "rt";
  const view = triggers[name];

  setActive(controlName, depth > 0.001, 1);

  if (!view?.fill || !view?.fade) {
    return;
  }

  const total = view.bottom - view.top;
  const filled = total * depth;
  const boundary = view.bottom - filled;
  const fadeHeight = depth <= 0 || depth >= 1 ? 0 : Math.min(view.transition, Math.max(0, boundary - view.top));
  const fadeY = boundary - fadeHeight;

  view.fill.setAttribute("y", boundary.toFixed(2));
  view.fill.setAttribute("height", filled.toFixed(2));
  view.fade.setAttribute("y", fadeY.toFixed(2));
  view.fade.setAttribute("height", fadeHeight.toFixed(2));
}

function applyState(state) {
  lastStateAt = performance.now();
  document.body.classList.toggle("disconnected", !state.connected);

  if (titleElement && state.connected && state.name) {
    titleElement.textContent = friendlyName(state.name);
  }

  if (statusElement) {
    statusElement.textContent = statusText(state);
  }

  const b = state.buttons ?? {};
  const a = state.axes ?? {};
  const leftTrigger = clamp01(a.leftTrigger ?? 0);
  const rightTrigger = clamp01(a.rightTrigger ?? 0);

  setActive("a", b.a);
  setActive("b", b.b);
  setActive("x", b.x);
  setActive("y", b.y);
  setActive("view", b.view);
  setActive("menu", b.menu);
  setActive("steam", b.steam);
  setActive("touch-click", b.quickAccess);
  setActive("lb", b.leftBumper);
  setActive("rb", b.rightBumper);
  setTrigger("left", leftTrigger);
  setTrigger("right", rightTrigger);
  setActive("dpad-up", b.dpadUp);
  setActive("dpad-right", b.dpadRight);
  setActive("dpad-down", b.dpadDown);
  setActive("dpad-left", b.dpadLeft);
  setActive("dpad-outline", b.dpadUp || b.dpadRight || b.dpadDown || b.dpadLeft);
  setActive("lg2", b.leftGripUpper);
  setActive("lg1", b.leftGripLower);
  setActive("rg2", b.rightGripUpper);
  setActive("rg1", b.rightGripLower);

  setStick("left", a.leftStickX ?? 0, a.leftStickY ?? 0, b.leftStick);
  setStick("right", a.rightStickX ?? 0, a.rightStickY ?? 0, b.rightStick);
  setTouchpad("left", state.leftTouchpad);
  setTouchpad("right", state.rightTouchpad);
}

function statusText(state) {
  if (state.connected) {
    const vendor = state.vendorId ? state.vendorId.toString(16).padStart(4, "0") : "0000";
    const product = state.productId ? state.productId.toString(16).padStart(4, "0") : "0000";
    return `${friendlyName(state.name)} connected (${vendor}:${product})`;
  }

  return state.name || state.status || "Waiting for controller";
}

function friendlyName(name) {
  if (!name) {
    return "Controller";
  }

  return name.replace(/\s*\(.*?\)\s*$/, "").trim();
}

function clamp01(value) {
  return Math.min(1, Math.max(0, Number(value) || 0));
}

function clampSigned(value) {
  return Math.min(1, Math.max(-1, Number(value) || 0));
}

function connectEvents() {
  const events = new EventSource("/events");
  events.onmessage = (event) => applyState(JSON.parse(event.data));
  events.onerror = () => {
    document.body.classList.add("disconnected");
    if (performance.now() - lastStateAt > 2500 && statusElement) {
      statusElement.textContent = "Waiting for local reader";
    }
  };
}

async function loadInitialState() {
  try {
    const response = await fetch("/api/state", { cache: "no-store" });
    if (response.ok) {
      applyState(await response.json());
    }
  } catch {
    document.body.classList.add("disconnected");
  }
}

loadInitialState();
connectEvents();
