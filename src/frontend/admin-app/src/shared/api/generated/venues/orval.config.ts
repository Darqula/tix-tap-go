import { createOrvalSpecConfig } from "@/shared/api/orval/base-config";
import { defineConfig } from "orval";

export default defineConfig({
  venue: createOrvalSpecConfig({
    specEndpoint: "/openapi/venues.json",
    target: "./api/venues.ts",
    schemas: "./model",
  }),
});
