export interface Venue {
  id: string;
  title: string;
  address: string;
  description: string;
}

export interface VenueDetailed extends Venue {
  seatingMapVersionId?: string;
  seatingMapTotalSeats?: number;
  categories: VenueCategory[];
}

export interface VenueCategory {
  id: string;
  title: string;
  color: string;
}
