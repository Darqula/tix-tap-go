import { createTheme } from "@mui/material/styles";

export const theme = createTheme({
  palette: {
    mode: "light",
    background: { default: "#f2f7f7" },
    primary: { main: "#0f766e" },
    secondary: { main: "#dc004e" },
    highlight: { main: "#14b8a6" },
  },
  components: {
    MuiTab: {
      styleOverrides: {
        root: ({ theme }) => ({
          color: `${theme.palette.primary.contrastText}99`,
          "&.Mui-selected": { color: theme.palette.primary.contrastText },
        }),
      },
    },
  },
});
