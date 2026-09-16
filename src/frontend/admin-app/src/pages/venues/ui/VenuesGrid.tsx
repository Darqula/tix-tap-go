import { Grid } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { VenueCard } from "./VenueCard/VenueCard";
import { getVenuesDetailed } from "@/entities/venues";

const VenuesGrid = () => {
  const {
    data: venues = [],
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["venues"],
    queryFn: () => getVenuesDetailed(),
  });
  if (isLoading) {
    return <div>Loading...</div>;
  }
  if (isError) {
    return <div>Error occurred while fetching venues.</div>;
  }
  return (
    <Grid container spacing={2} sx={{ width: "80%", alignSelf: "center" }}>
      {venues.map((venue) => (
        <Grid key={venue.id} size={4}>
          <VenueCard venue={venue} />
        </Grid>
      ))}
    </Grid>
  );
};

export { VenuesGrid };
