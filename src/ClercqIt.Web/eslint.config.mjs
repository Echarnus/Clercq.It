// ESLint flat config (issue #240). Policy: docs/development.md#frontend-lint-format-and-types
import nextVitals from "eslint-config-next/core-web-vitals";
import jsxA11y from "eslint-plugin-jsx-a11y";
import security from "eslint-plugin-security";
import prettier from "eslint-config-prettier";
import tseslint from "typescript-eslint";

export default tseslint.config(
  {
    ignores: [
      ".next/**",
      "node_modules/**",
      "out/**",
      "coverage/**",
      "public/**",
      "playwright-report/**",
      "test-results/**",
      "next-env.d.ts",
    ],
  },
  // next/typescript is replaced by typescript-eslint below (both register the plugin).
  ...nextVitals,
  {
    files: ["**/*.{ts,tsx}"],
    extends: [tseslint.configs.recommendedTypeChecked],
    languageOptions: {
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
    },
  },
  {
    files: ["**/*.{js,mjs,cjs}"],
    extends: [tseslint.configs.disableTypeChecked],
  },
  {
    rules: {
      ...jsxA11y.flatConfigs.recommended.rules,
    },
  },
  security.configs.recommended,
  prettier,
);
