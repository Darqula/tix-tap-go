import { Trans } from "react-i18next";
import { VenuesGrid } from "./ui/VenuesGrid";
import { Button, Stack } from "@mui/material";

const VenuesPage = () => {
  return (
    <Stack direction="column" spacing={2}>
      <h1>
        <Trans>Venues.Header</Trans>
      </h1>
      <Trans>Venues.Description</Trans>
      <Button onClick={() => {}}>
        <Trans>Venues.Create</Trans>
      </Button>
      <VenuesGrid />
    </Stack>
  );
};

export { VenuesPage };
