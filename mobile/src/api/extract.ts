/**
 * Helpers to pull values out of a response whose exact shape we don't fully control.
 *
 * The completelogin response is a wrapper (`CompleteAuthorizeUserResponse`) and the fields we
 * need — `securityToken`, member profile bits — may sit at the top level or nested inside a
 * `data` / member object. Rather than hard-code one path, we search the object graph by key
 * (case-insensitive), preferring the shallowest match.
 */

function isObject(v: unknown): v is Record<string, unknown> {
  return v !== null && typeof v === 'object';
}

/** Breadth-first search for the first non-empty string under any key matching `key`. */
export function deepFindString(root: unknown, key: string): string | null {
  const target = key.toLowerCase();
  const queue: unknown[] = [root];
  while (queue.length) {
    const cur = queue.shift();
    if (!isObject(cur)) continue;
    const entries = Object.entries(cur);
    for (const [k, v] of entries) {
      if (k.toLowerCase() === target && typeof v === 'string' && v.length > 0) return v;
    }
    for (const [, v] of entries) {
      if (isObject(v)) queue.push(v);
    }
  }
  return null;
}

/** First match across several candidate keys (tried in order at each level). */
export function deepFindFirstString(root: unknown, keys: string[]): string | null {
  for (const key of keys) {
    const hit = deepFindString(root, key);
    if (hit) return hit;
  }
  return null;
}

/** Top-level keys of an object (for diagnostics when an expected field is missing). */
export function topLevelKeys(root: unknown): string[] {
  return isObject(root) ? Object.keys(root) : [];
}
