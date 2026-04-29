// @ts-check
/**
 * Angular template ESLint rule: blocks literal English/Arabic strings in
 * Angular *.html templates so every user-visible string flows through
 * @ngx-translate. Required by Constitution Principle IV ("Bilingual & Accessible
 * Experience") and spec.md FR-006/FR-009.
 *
 * Detects bare text nodes containing letters (Latin or Arabic). Allowed:
 *   - {{ expr }} interpolations
 *   - elements with `[attr.aria-label]` or `*ngIf` etc. (we only inspect text)
 *   - the `{{ 'key' | translate }}` pipe (which is an interpolation)
 *   - whitespace, digits, punctuation, and HTML entities
 */

const LETTERS = /[A-Za-z؀-ۿ]/;

module.exports = {
  meta: {
    type: "problem",
    docs: { description: "Disallow literal user-visible strings in Angular templates" },
    schema: [],
    messages: {
      literal:
        "Literal text '{{text}}' must be sourced from i18n (use the | translate pipe). See Constitution Principle IV.",
    },
  },
  create(context) {
    return {
      Text$1(node) {
        const value = (node.value || "").trim();
        if (!value || !LETTERS.test(value)) return;
        context.report({
          node,
          messageId: "literal",
          data: { text: value.length > 40 ? value.slice(0, 40) + "..." : value },
        });
      },
    };
  },
};
