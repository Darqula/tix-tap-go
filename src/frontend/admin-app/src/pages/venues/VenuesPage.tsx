import { VenuesGrid } from "./ui/VenuesGrid";
import { Button, Stack, Typography } from "@mui/material";
import { useTranslation } from "react-i18next";

const VenuesPage = () => {
  var { t } = useTranslation();
  return (
    <Stack direction="column" spacing={2}>
      <Stack direction="column">
        <Typography variant="h1"> {t("Venues.Header")} </Typography>
        <Typography variant="subtitle1">{t("Venues.Description")}</Typography>
      </Stack>
      <Button variant="contained" sx={{ width: "140px" }} onClick={() => {}}>
        + {t("Venues.Create")}
      </Button>
      <VenuesGrid />
    </Stack>
  );
};

export { VenuesPage };
