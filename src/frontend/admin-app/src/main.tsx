import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import App from "./app/App";
import { decorateWithIdempotencyKeys } from "@/shared/api/custom-fetch";

globalThis.fetch = decorateWithIdempotencyKeys(globalThis.fetch);

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
