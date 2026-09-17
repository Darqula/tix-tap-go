import { createOrvalSpecConfig } from "@/shared/api/orval/base-config";
import { defineConfig } from "orval";

export default defineConfig({
  event: createOrvalSpecConfig({
    specEndpoint: "/openapi/events.json",
    target: "./api/events.ts",
    schemas: "./model",
  }),
});
