import type { Theme } from "@mui/material";
import type { SxProps } from "@mui/material";

const chip: SxProps<Theme> = {
  fontSize: "10px",
  padding: "0px",
};

export const styles = (theme: Theme) => ({
  frame: {
    border: "1px solid",
    borderColor: "divider",
    borderRadius: 4,
    padding: "12px",
    height: "100%",
    boxShadow: `0 4px 14px ${theme.palette.primary.main}26`,
    "&:hover": {
      boxShadow: 2,
      borderColor: "highlight.main",
    },
  },
  heading: {
    display: "-webkit-box",
    WebkitLineClamp: 2,
    WebkitBoxOrient: "vertical",
    overflow: "hidden",
    textOverflow: "ellipsis",
    fontWeight: "bold",
    color: "text.primary",
  },
  caption: {
    display: "-webkit-box",
    WebkitLineClamp: 2,
    WebkitBoxOrient: "vertical",
    overflow: "hidden",
    textOverflow: "ellipsis",
    lineHeight: 1.5,
    color: "text.secondary",
  },
  "chips-stack": {
    flexWrap: "wrap",
  },
  chip,
  "chip-active": {
    ...chip,
    "& .MuiChip-label": {
      color: "primary.main",
    },
  },
}) satisfies SxProps<Theme>;
