import { ThemeProvider as MuiThemeProvider, CssBaseline } from "@mui/material";
import type { ReactNode } from "react";
import { theme } from "../theme/theme";

export const ThemeProvider = ({ children }: { children: ReactNode }) => {
  return (
    <MuiThemeProvider theme={theme}>
      <CssBaseline />
      {children}
    </MuiThemeProvider>
  );
};
