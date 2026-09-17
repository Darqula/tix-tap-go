import type { Options } from "orval";
import { appConfig } from "@/shared/config";
import { fileURLToPath } from "node:url";

// Resolved relative to this file instead of the config file that uses it
const transformerPath = fileURLToPath(new URL("./nullable-transform.ts", import.meta.url));

interface OrvalSpecConfig {
  /** URL or file path to the service's OpenAPI spec. */
  specEndpoint: string;
  /** Output path for the generated api client, relative to the entity's orval.config.ts. */
  target: string;
  /** Output path for generated model types, relative to the entity's orval.config.ts. */
  schemas: string;
}

export const createOrvalSpecConfig = ({
  specEndpoint,
  target,
  schemas,
}: OrvalSpecConfig): Options => ({
  input: {
    target: new URL(specEndpoint, appConfig.specBaseUrl).toString(),
    override: {
      transformer: transformerPath,
    },
  },
  output: {
    mode: "tags-split",
    target,
    schemas,
    client: "react-query",
    mock: false,
    formatter: "prettier",
    httpClient: "fetch",
    baseUrl: appConfig.apiBaseUrl,
    override: {
      fetch: {
        includeHttpResponseReturnType: false,
      },
    },
  },
});
