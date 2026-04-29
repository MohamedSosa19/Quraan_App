// @ts-check
const eslint = require("@eslint/js");
const tseslint = require("typescript-eslint");
const angular = require("angular-eslint");
const noLiteralTemplateStrings = require("./eslint-rules/no-literal-template-strings.js");

module.exports = tseslint.config(
  {
    files: ["**/*.ts"],
    extends: [
      eslint.configs.recommended,
      ...tseslint.configs.recommended,
      ...tseslint.configs.stylistic,
      ...angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      "@angular-eslint/directive-selector": [
        "error",
        { type: "attribute", prefix: "app", style: "camelCase" },
      ],
      "@angular-eslint/component-selector": [
        "error",
        { type: "element", prefix: "app", style: "kebab-case" },
      ],
    },
  },
  {
    files: ["**/*.html"],
    plugins: {
      "no-literal-template-strings": {
        rules: {
          "no-literal-template-strings": noLiteralTemplateStrings,
        },
      },
    },
    extends: [
      ...angular.configs.templateRecommended,
      ...angular.configs.templateAccessibility,
    ],
    rules: {
      "no-literal-template-strings/no-literal-template-strings": "error",
    },
  },
  {
    ignores: ["dist/", "node_modules/", ".angular/", "coverage/", "src/app/api/"],
  },
);
