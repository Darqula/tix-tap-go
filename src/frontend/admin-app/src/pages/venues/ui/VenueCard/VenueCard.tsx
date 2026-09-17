import type { GetVenueDetailedResponse } from "@/entities/venues";
import { Stack, Typography } from "@/shared/ui";
import { Box, Chip } from "@mui/material";
import { styles } from "./VenueCard.styles";

type VenueCardProps = {
  venue: GetVenueDetailedResponse;
};

const VenueCard = ({ venue }: VenueCardProps) => {
  return (
    <Box sx={styles.frame}>
      <Stack direction="column" spacing={1}>
        <Typography variant="h6" sx={styles.heading}>
          {venue.title}
        </Typography>
        <Typography variant="caption" sx={styles.caption}>
          {venue.address}
        </Typography>
        <Stack direction="row" spacing={1} useFlexGap sx={styles["chips-stack"]}>
          {venue.categories?.length > 0 && (
            <Chip
              color="primary"
              size="small"
              sx={styles.chip}
              label={`${venue.categories.length} categories`}
            />
          )}
          {!!venue.seatingMapTotalSeats && (
            <Chip
              color="primary"
              size="small"
              sx={styles.chip}
              label={`${venue.seatingMapTotalSeats} seats`}
            />
          )}
          {venue.seatingMapVersionId ? (
            <Chip
              color="default"
              size="small"
              sx={styles["chip-active"]}
              label={`\u2022 active map`}
            />
          ) : (
            <Chip color="default" size="small" sx={styles.chip} label={`no active map`} />
          )}
        </Stack>
      </Stack>
    </Box>
  );
};

export { VenueCard };
