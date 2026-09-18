import { createTheme } from "@mui/material/styles";

export const theme = createTheme({
  palette: {
    mode: "light",
    background: { default: "#f2f7f7", paper: "#ffffff" },
    primary: { main: "#0f766e", contrastText: "#ffffff" },
    secondary: { main: "#dc004e" },
    highlight: { main: "#14b8a6", light: "#5eead4" },
    surface: { main: "#fbfdfd" },
    error: { main: "#dc2626" },
    warning: { main: "#d97706" },
    success: { main: "#059669" },
    text: { primary: "#14343b", secondary: "#5f7a80" },
    divider: "#dce5e5",
    status: {
      draft: { bg: "#fef3c7", color: "#d97706" },
      active: { bg: "#ccfbf1", color: "#0f766e" },
      upcoming: { bg: "#ccfbf1", color: "#0f766e" },
      completed: { bg: "#d1fae5", color: "#059669" },
      cancelled: { bg: "#fee2e2", color: "#dc2626" },
      archived: { bg: "#e6ecec", color: "#5f7a80" },
    },
  },
  typography: {
    fontFamily: '"Segoe UI", system-ui, sans-serif',
    fontSize: 14,
    h1: { fontSize: "22px", fontWeight: 700 },
    h2: { fontSize: "15px", fontWeight: 700 },
    button: { textTransform: "none", fontWeight: 600 },
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: { borderRadius: 10, padding: "9px 18px" },
        sizeSmall: { padding: "4px 10px", fontSize: "12px" },
        outlined: ({ theme }) => ({
          color: theme.palette.text.secondary,
          borderColor: theme.palette.divider,
          backgroundColor: theme.palette.background.paper,
          "&:hover": { color: theme.palette.text.primary },
        }),
        contained: ({ theme }) => ({
          "&:hover": { backgroundColor: theme.palette.highlight.main },
        }),
      },
    },
    MuiCard: {
      styleOverrides: {
        root: ({ theme }) => ({
          borderRadius: 4,
          border: `1px solid ${theme.palette.divider}`,
          boxShadow: "0 1px 3px rgba(20, 52, 59, 0.06)",
        }),
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: {
          backgroundImage: "linear-gradient(90deg, #0f766e, #115e59 60%, #134e4a)",
          boxShadow: "0 2px 12px rgba(0, 0, 0, 0.15)",
        },
      },
    },
    MuiTabs: {
      styleOverrides: {
        indicator: ({ theme }) => ({ backgroundColor: theme.palette.highlight.light }),
      },
    },
    MuiTab: {
      styleOverrides: {
        root: {
          color: "#ccfbf1",
          fontWeight: 600,
          "&:hover": { color: "#ffffff" },
          "&.Mui-selected": { color: "#ffffff" },
        },
      },
    },
    MuiOutlinedInput: {
      styleOverrides: {
        root: ({ theme }) => ({
          borderRadius: 10,
          backgroundColor: theme.palette.surface.main,
        }),
      },
    },
    MuiFormLabel: {
      styleOverrides: {
        root: ({ theme }) => ({
          fontSize: "12px",
          fontWeight: 600,
          color: theme.palette.text.secondary,
        }),
      },
    },
    MuiTableCell: {
      styleOverrides: {
        head: ({ theme }) => ({
          fontSize: "11px",
          fontWeight: 600,
          textTransform: "uppercase",
          letterSpacing: "0.5px",
          color: theme.palette.text.secondary,
        }),
      },
    },
  },
});
