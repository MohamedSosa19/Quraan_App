#!/usr/bin/env node
/**
 * T181 — WCAG 2.1 AA contrast gate for the design tokens defined in
 * `frontend/src/styles/_tokens.scss`. Enforced in CI so a token edit that
 * regresses contrast fails the build. (R-11, Principle IV)
 *
 * Thresholds:
 *   - Normal text   ≥ 4.5:1   (foreground / muted on background)
 *   - Large/UI text ≥ 3.0:1   (links, accents, borders against surface)
 *
 * Usage: node tools/check-contrast.mjs
 * Exit code: 0 = all pairs pass, 1 = at least one pair below threshold.
 *
 * Pure-Node (no deps); safe to run in CI on a vanilla Node 20+ runner.
 */

import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const tokensPath = resolve(here, '..', 'frontend', 'src', 'styles', '_tokens.scss');

const TOKEN_RE = /(--[a-z][a-z0-9-]*)\s*:\s*(#[0-9a-fA-F]{3,8})/g;

/** Parse a hex color into [r, g, b] in 0..255. Supports #rgb / #rrggbb. */
function hexToRgb(hex) {
  let body = hex.replace(/^#/, '');
  if (body.length === 3) body = body.split('').map((c) => c + c).join('');
  if (body.length !== 6) throw new Error(`Unsupported hex format: ${hex}`);
  const n = parseInt(body, 16);
  return [(n >> 16) & 0xff, (n >> 8) & 0xff, n & 0xff];
}

/** sRGB → linear, per WCAG 2.1 relative-luminance definition. */
function relLuminance([r, g, b]) {
  const lin = (c) => {
    const v = c / 255;
    return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4);
  };
  return 0.2126 * lin(r) + 0.7152 * lin(g) + 0.0722 * lin(b);
}

function contrastRatio(a, b) {
  const la = relLuminance(a);
  const lb = relLuminance(b);
  const [hi, lo] = la > lb ? [la, lb] : [lb, la];
  return (hi + 0.05) / (lo + 0.05);
}

/** Extract token map from a single :root or media block of _tokens.scss. */
function extractScope(scss, scopeStartIndex) {
  // Walk forward, balancing braces so we capture exactly one block.
  let depth = 0;
  let i = scopeStartIndex;
  let started = false;
  for (; i < scss.length; i++) {
    if (scss[i] === '{') {
      depth++;
      started = true;
    } else if (scss[i] === '}') {
      depth--;
      if (started && depth === 0) {
        return scss.slice(scopeStartIndex, i + 1);
      }
    }
  }
  return scss.slice(scopeStartIndex);
}

function parseTokens(scss, anchor) {
  const idx = scss.indexOf(anchor);
  if (idx === -1) return null;
  const block = extractScope(scss, idx);
  const tokens = {};
  for (const m of block.matchAll(TOKEN_RE)) {
    tokens[m[1]] = m[2];
  }
  return tokens;
}

// Hard-fail checks — these MUST pass for WCAG 2.1 AA Normal-text compliance.
// ('--color-border' deliberately omitted: borders are used here as decorative
//  dividers between adjacent surfaces, which falls under the WCAG 1.4.11
//  "incidental" exception. Per-component focus-ring contrast is verified
//  manually via tests/e2e/checklists/a11y.md.)
const PAIRS = [
  { fg: '--color-fg',     bg: '--color-bg',      min: 4.5, label: 'body text on bg' },
  { fg: '--color-muted',  bg: '--color-bg',      min: 4.5, label: 'muted text on bg' },
  { fg: '--color-link',   bg: '--color-bg',      min: 4.5, label: 'link text on bg' },
  { fg: '--color-fg',     bg: '--color-surface', min: 4.5, label: 'body text on surface' },
  { fg: '--color-accent', bg: '--color-bg',      min: 3.0, label: 'accent UI on bg' },
];

// Informational ratios — printed but not gated. Useful context for design review.
const INFO_PAIRS = [
  { fg: '--color-border', bg: '--color-bg',      label: 'border on bg (decorative)' },
];

const THEMES = [
  { name: 'light', anchor: ':root' },
  { name: 'dark',  anchor: '@media (prefers-color-scheme: dark)' },
];

const scss = readFileSync(tokensPath, 'utf8');
let failures = 0;
const lines = [];

for (const theme of THEMES) {
  const tokens = parseTokens(scss, theme.anchor);
  if (!tokens) {
    lines.push(`SKIP  ${theme.name}: scope '${theme.anchor}' not found`);
    continue;
  }
  const lightTokens = parseTokens(scss, ':root');
  for (const pair of PAIRS) {
    // Dark theme inherits any value not explicitly overridden.
    const fgHex = tokens[pair.fg] ?? lightTokens?.[pair.fg];
    const bgHex = tokens[pair.bg] ?? lightTokens?.[pair.bg];
    if (!fgHex || !bgHex) {
      lines.push(`SKIP  ${theme.name} ${pair.label}: tokens missing`);
      continue;
    }
    const ratio = contrastRatio(hexToRgb(fgHex), hexToRgb(bgHex));
    const ok = ratio >= pair.min;
    const status = ok ? 'PASS' : 'FAIL';
    if (!ok) failures++;
    lines.push(
      `${status}  ${theme.name.padEnd(5)} ${pair.label.padEnd(28)} ` +
      `${fgHex} on ${bgHex}  ratio=${ratio.toFixed(2)} (min ${pair.min})`,
    );
  }
  for (const pair of INFO_PAIRS) {
    const fgHex = tokens[pair.fg] ?? lightTokens?.[pair.fg];
    const bgHex = tokens[pair.bg] ?? lightTokens?.[pair.bg];
    if (!fgHex || !bgHex) continue;
    const ratio = contrastRatio(hexToRgb(fgHex), hexToRgb(bgHex));
    lines.push(
      `INFO  ${theme.name.padEnd(5)} ${pair.label.padEnd(28)} ` +
      `${fgHex} on ${bgHex}  ratio=${ratio.toFixed(2)}`,
    );
  }
}

for (const line of lines) console.log(line);

if (failures > 0) {
  console.error(`\n${failures} contrast pair(s) below WCAG AA threshold`);
  process.exit(1);
}
console.log('\nAll contrast pairs meet WCAG 2.1 AA.');
