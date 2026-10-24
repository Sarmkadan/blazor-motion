// BlazorMotion Web Animations API adapter.
//
// Loaded lazily from .NET via IJSRuntime.InvokeAsync<IJSObjectReference>("import", ...).
// Dependency-free: no runtime packages, no DOM globals beyond the element passed in.
//
// Payload shape (camelCase, as serialised from Interop/MotionPayload.cs):
//
//   AnimationPayload = {
//     keyframes: [{ properties: { [cssProperty]: string }, offset: number | null }],
//     timing: {
//       duration:  number,          // ms
//       delay:     number,          // ms
//       easing:    string,          // CSS timing function
//       iterations: number | null,  // null = infinite
//       direction: "normal" | "reverse" | "alternate",
//       fill:      "forwards" | "none",
//     },
//   }
//
//   TimelineEntry = { target: Element, animation: AnimationPayload, startAt: number }  // startAt in ms
//
// Ids returned by animate/playTimeline index a module-level registry. An entry is removed
// when its animation finishes or is cancelled, so later calls with that id are no-ops
// (finished() resolves false for an unknown id).

const animations = new Map();
let nextId = 1;
let respectReducedMotion = true;

/**
 * Receives the global settings from MotionOptions. Called once, right after the module is imported.
 * Collapsing durations to 0 under prefers-reduced-motion is not implemented yet.
 * @param {{ respectReducedMotion: boolean }} options
 */
export function configure(options) {
  respectReducedMotion = options?.respectReducedMotion !== false;
}

function requireElement(element, method, api) {
  if (element == null || typeof element[api] !== "function") {
    throw new Error(
      `blazor-motion: ${method} needs a rendered element with the Web Animations API (${api}()); got ${element === null ? "null" : typeof element}.`
    );
  }
}

function toKeyframes(keyframes) {
  return keyframes.map(({ properties, offset }) =>
    offset == null ? { ...properties } : { ...properties, offset }
  );
}

function toOptions(timing) {
  return {
    duration: timing.duration,
    delay: timing.delay,
    easing: timing.easing,
    iterations: timing.iterations ?? Infinity,
    direction: timing.direction,
    fill: timing.fill,
  };
}

/**
 * Starts an animation on an element and registers it.
 * @param {Element} element
 * @param {object} payload AnimationPayload (see header).
 * @returns {number} Id used by the other exports.
 */
export function animate(element, payload) {
  requireElement(element, "animate", "animate");

  const animation = element.animate(
    toKeyframes(payload.keyframes),
    toOptions(payload.timing)
  );

  const id = nextId++;
  animations.set(id, animation);

  // Handle the rejection that cancel() causes so it is not reported as unhandled,
  // and drop the entry once the animation is over either way.
  const remove = () => animations.delete(id);
  animation.finished.then(remove, remove);

  return id;
}

/**
 * Resolves when the animation finishes: true on finish, false on cancel.
 * Never rejects. An unknown id (already finished or cancelled) resolves false.
 * @param {number} id
 * @returns {Promise<boolean>}
 */
export function finished(id) {
  const animation = animations.get(id);
  if (!animation) {
    return Promise.resolve(false);
  }

  return animation.finished.then(() => true, () => false);
}

/** Pauses a registered animation. No-op for an unknown id. */
export function pause(id) {
  animations.get(id)?.pause();
}

/** Resumes a registered animation. No-op for an unknown id. */
export function play(id) {
  animations.get(id)?.play();
}

/** Cancels a registered animation and removes its entry. No-op for an unknown id. */
export function cancel(id) {
  const animation = animations.get(id);
  if (!animation) {
    return;
  }

  animations.delete(id);
  animation.cancel();
}

/**
 * Starts several animations, each delayed by its startAt.
 * If one entry fails to start, the ones already started are cancelled and the error is rethrown.
 * @param {Array<{target: Element, animation: object, startAt: number}>} entries
 * @returns {number[]} One id per entry, in the same order.
 */
export function playTimeline(entries) {
  const ids = [];

  try {
    for (const { target, animation, startAt } of entries) {
      ids.push(
        animate(target, {
          keyframes: animation.keyframes,
          timing: { ...animation.timing, delay: animation.timing.delay + startAt },
        })
      );
    }
  } catch (error) {
    ids.forEach(cancel);
    throw error;
  }

  return ids;
}

/**
 * Cancels every animation running on an element, including ones not started through this module.
 * @param {Element} element
 */
export function cancelAll(element) {
  requireElement(element, "cancelAll", "getAnimations");

  element.getAnimations().forEach((animation) => animation.cancel());
}
