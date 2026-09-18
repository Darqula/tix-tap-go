import type { VenueDetailed } from "@/entities/venues";
import { Stack, Typography } from "@/shared/ui";
import { Box, Chip, useTheme } from "@mui/material";
import { styles as stylesTemplate } from "./VenueCard.styles";

type VenueCardProps = {
  venue: VenueDetailed;
};

const VenueCard = ({ venue }: VenueCardProps) => {
  const theme = useTheme();
  const styles = stylesTemplate(theme);
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
