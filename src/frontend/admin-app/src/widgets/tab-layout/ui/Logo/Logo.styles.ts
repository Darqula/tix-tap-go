import type { Theme } from "@mui/material";
import type { SxProps } from "@mui/material";

export const styles = (theme: Theme) =>
  ({
    fontWeight: "800",
    fontSize: "17px",
    letterSpacing: "0.3px",
    "& em": {
      fontStyle: "normal",
      color: theme.palette.highlight.main,
    },
  }) satisfies SxProps<Theme>;
