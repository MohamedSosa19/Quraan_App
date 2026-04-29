#!/usr/bin/env node
// WCAG 2.1 AA contrast gate (Principle IV / R-11). Reads
// src/styles/_tokens.scss, extracts named --color-* hex tokens, asserts
// fg-vs-bg pairs meet ≥ 4.5:1 (normal text). Exits non-zero on failure so CI
// fails the build.
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const __dirname = dirname(fileURLToPath(import.meta.url));
const tokensPath = join(__dirname, '..', 'src', 'styles', '_tokens.scss');

const css = readFileSync(tokensPath, 'utf8');
const blocks = [...css.matchAll(/:root\s*{([\s\S]*?)}/g)].map((m) => m[1]);

function parse(block) {
  const out = {};
  for (const line of block.split('\n')) {
    const m = line.match(/--color-([\w-]+):\s*(#[0-9a-fA-F]{6})/);
    if (m) out[m[1]] = m[2];
  }
  return out;
}

function luminance(hex) {
  const c = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255);
  const lin = c.map((v) => (v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4));
  return 0.2126 * lin[0] + 0.7152 * lin[1] + 0.0722 * lin[2];
}

function ratio(a, b) {
  const [l1, l2] = [luminance(a), luminance(b)].sort((x, y) => y - x);
  return (l1 + 0.05) / (l2 + 0.05);
}

const failures = [];
for (const block of blocks) {
  const t = parse(block);
  if (!t.bg) continue;
  for (const key of ['fg', 'muted', 'link', 'accent']) {
    if (!t[key]) continue;
    const r = ratio(t.bg, t[key]);
    if (r < 4.5) failures.push(`color-${key} (${t[key]}) on bg (${t.bg}) = ${r.toFixed(2)}:1 — below AA 4.5`);
  }
}

if (failures.length > 0) {
  console.error('Contrast check FAILED:');
  for (const f of failures) console.error('  • ' + f);
  process.exit(1);
}
console.log('Contrast check OK.');
