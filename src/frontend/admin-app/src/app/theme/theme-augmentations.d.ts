import "@mui/material/styles";

declare module "@mui/material/styles" {
  interface Palette {
    highlight: Palette["primary"];
    surface: Palette["primary"];
    status: Record<
      "draft" | "active" | "upcoming" | "completed" | "cancelled" | "archived",
      { bg: string; color: string }
    >;
  }
  interface PaletteOptions {
    highlight?: PaletteOptions["primary"];
    surface?: PaletteOptions["primary"];
    status?: Partial<Palette["status"]>;
  }
}
