import { CreateVenue } from "@/features/create-venue/CreateVenue";
import { VenuesGrid } from "./ui/VenuesGrid";
import { Box, Button, Stack, Typography } from "@mui/material";
import { useState } from "react";
import { useTranslation } from "react-i18next";

const VenuesPage = () => {
  const { t } = useTranslation();
  const [isOnCreateFormOpen, setIsOnCreateFormOpen] = useState(false);
  return (
    <Box>
      <Stack direction="column" spacing={2}>
        <Stack direction="column">
          <Typography variant="h1"> {t("Venues.Header")} </Typography>
          <Typography variant="subtitle1">{t("Venues.Description")}</Typography>
        </Stack>
        <Button variant="contained" sx={{ width: "140px" }} onClick={() => { setIsOnCreateFormOpen(true) }}>
          + {t("Venues.Create")}
        </Button>
        <VenuesGrid />
      </Stack>
      {isOnCreateFormOpen && <CreateVenue isOpen={isOnCreateFormOpen} onClose={() => setIsOnCreateFormOpen(false)} />}
    </Box>
  );
};

export { VenuesPage };
