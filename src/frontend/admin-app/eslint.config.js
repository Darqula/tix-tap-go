import js from "@eslint/js";
import globals from "globals";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import tseslint from "typescript-eslint";
import prettier from "eslint-config-prettier";
import { defineConfig, globalIgnores } from "eslint/config";

const fsdLayerRestrictions = {
  "src/shared/**": ["@/app/**", "@/pages/**", "@/widgets/**", "@/features/**", "@/entities/**"],
  "src/entities/**": ["@/app/**", "@/pages/**", "@/widgets/**", "@/features/**"],
  "src/features/**": ["@/app/**", "@/pages/**", "@/widgets/**"],
  "src/widgets/**": ["@/app/**", "@/pages/**"],
  "src/pages/**": ["@/app/**"],
};

const fsdBoundaryConfigs = Object.entries(fsdLayerRestrictions).map(([files, forbidden]) => ({
  files: [files],
  rules: {
    "no-restricted-imports": [
      "error",
      {
        patterns: forbidden.map((group) => ({
          group: [group],
          message: `FSD violation: ${files} must not import from a higher layer (${group})`,
        })),
      },
    ],
  },
}));

export default defineConfig([
  globalIgnores(["dist", "node_modules"]),
  {
    files: ["**/*.{ts,tsx}"],
    extends: [
      js.configs.recommended,
      ...tseslint.configs.recommended,
      tseslint.configs.recommendedTypeChecked,
      reactHooks.configs.flat["recommended-latest"],
      reactRefresh.configs.vite,
      prettier,
    ],
    languageOptions: {
      ecmaVersion: "latest",
      globals: globals.browser,
      parserOptions: {
        projectService: {
          allowDefaultProject: ["vite.config.ts"],
        },
      },
    },
  },
  ...fsdBoundaryConfigs,
]);
