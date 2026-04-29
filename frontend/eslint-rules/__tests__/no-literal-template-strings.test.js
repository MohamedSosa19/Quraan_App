// @ts-check
const { RuleTester } = require("eslint");
const rule = require("../no-literal-template-strings.js");

const tester = new RuleTester();

tester.run("no-literal-template-strings", rule, {
  valid: [
    { code: "<div>{{ 'app.title' | translate }}</div>" },
    { code: "<div>{{ count }}</div>" },
    { code: "<div>   42   </div>" },
    { code: "<img alt=\"{{ 'a.alt' | translate }}\" />" },
  ],
  invalid: [
    {
      code: "<div>Hello world</div>",
      errors: [{ messageId: "literal" }],
    },
    {
      code: "<div>السلام</div>",
      errors: [{ messageId: "literal" }],
    },
  ],
});
