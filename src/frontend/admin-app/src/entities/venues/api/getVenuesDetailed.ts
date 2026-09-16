import type { VenueDetailed } from "../model/types";

const mockVenuesDetailed: VenueDetailed[] = [
  {
    id: "venue-001",
    title: "Madison Square Garden",
    address: "33 East 33rd Street, New York, NY 10001",
    description: "Premier multipurpose indoor arena",
    seatingMapVersionId: "map-v1-001",
    seatingMapTotalSeats: 20789,
    categories: [
      { id: "cat-001", title: "Standard", color: "#3498db" },
      { id: "cat-002", title: "VIP", color: "#f39c12" },
      { id: "cat-003", title: "Premium", color: "#e74c3c" },
    ],
  },
  {
    id: "venue-002",
    title: "Staples Center",
    address: "1111 South Figueroa Street, Los Angeles, CA 90015",
    description: "World-class arena in downtown LA",
    categories: [
      { id: "cat-004", title: "Lower Bowl", color: "#2ecc71" },
      { id: "cat-005", title: "Upper Level", color: "#9b59b6" },
    ],
  },
  {
    id: "venue-003",
    title: "The Fillmore",
    address: "1805 Geary Boulevard, San Francisco, CA 94115",
    description: "Intimate historic music venue",
    seatingMapVersionId: "map-v1-003",
    seatingMapTotalSeats: 1200,
    categories: [{ id: "cat-006", title: "General Admission", color: "#1abc9c" }],
  },
  {
    id: "venue-004",
    title: "Red Rocks Amphitheatre",
    address: "18300 West County Road 93, Morrison, CO 80465",
    description: "Natural outdoor amphitheater with stunning views",
    seatingMapVersionId: "map-v1-004",
    seatingMapTotalSeats: 9545,
    categories: [
      { id: "cat-007", title: "Center", color: "#e67e22" },
      { id: "cat-008", title: "Sides", color: "#34495e" },
    ],
  },
  {
    id: "venue-005",
    title: "The Ritz-Carlton Theater",
    address: "221 West 51st Street, New York, NY 10019",
    description: "Elegant Broadway theater",
    seatingMapVersionId: "map-v1-005",
    seatingMapTotalSeats: 1319,
    categories: [
      { id: "cat-009", title: "Orchestra", color: "#c0392b" },
      { id: "cat-010", title: "Mezzanine", color: "#16a085" },
    ],
  },
  {
    id: "venue-006",
    title: "American Airlines Center",
    address: "2500 Victory Avenue, Dallas, TX 75219",
    description: "State-of-the-art sports and entertainment arena",
    seatingMapVersionId: "map-v1-006",
    seatingMapTotalSeats: 20000,
    categories: [
      { id: "cat-011", title: "Courtside", color: "#8e44ad" },
      { id: "cat-012", title: "Club", color: "#d35400" },
      { id: "cat-013", title: "Standard", color: "#27ae60" },
    ],
  },
  {
    id: "venue-007",
    title: "Hollywood Bowl",
    address: "2301 North Highland Avenue, Los Angeles, CA 90068",
    description: "Iconic outdoor amphitheater in the Hollywood Hills",
    seatingMapVersionId: "map-v1-007",
    seatingMapTotalSeats: 17623,
    categories: [
      { id: "cat-014", title: "Box Seats", color: "#2980b9" },
      { id: "cat-015", title: "General Seating", color: "#95a5a6" },
    ],
  },
  {
    id: "venue-008",
    title: "The Beacon Theatre",
    address: "2124 Broadway, New York, NY 10023",
    description: "Manhattan concert hall with historic charm",
    seatingMapVersionId: "map-v1-008",
    seatingMapTotalSeats: 2894,
    categories: [
      { id: "cat-016", title: "Orchestra", color: "#7f8c8d" },
      { id: "cat-017", title: "Balcony", color: "#f1c40f" },
    ],
  },
  {
    id: "venue-009",
    title: "TD Garden",
    address: "100 Legends Way, Boston, MA 02114",
    description: "Home of the Celtics and Bruins",
    seatingMapVersionId: "map-v1-009",
    seatingMapTotalSeats: 19156,
    categories: [
      { id: "cat-018", title: "Suite", color: "#e74c3c" },
      { id: "cat-019", title: "Club", color: "#3498db" },
      { id: "cat-020", title: "Regular", color: "#2ecc71" },
    ],
  },
  {
    id: "venue-010",
    title: "Austin City Limits Live",
    address: "701 East 6th Street, Austin, TX 78702",
    description: "Legendary live music venue in downtown Austin",
    seatingMapVersionId: "map-v1-010",
    seatingMapTotalSeats: 3000,
    categories: [
      { id: "cat-021", title: "Main Floor", color: "#9b59b6" },
      { id: "cat-022", title: "Balcony", color: "#1abc9c" },
    ],
  },
  {
    id: "venue-011",
    title: "Chase Center",
    address: "1020 Irving Street, San Francisco, CA 94107",
    description: "State-of-the-art arena with bay views",
    seatingMapVersionId: "map-v1-011",
    seatingMapTotalSeats: 18064,
    categories: [
      { id: "cat-023", title: "Baseline", color: "#e67e22" },
      { id: "cat-024", title: "Sideline", color: "#34495e" },
      { id: "cat-025", title: "Upper", color: "#16a085" },
    ],
  },
  {
    id: "venue-012",
    title: "Metro Chicago",
    address: "3730 North Clark Street, Chicago, IL 60613",
    description: "Intimate venue in Wrigleyville neighborhood",
    seatingMapVersionId: "map-v1-012",
    seatingMapTotalSeats: 600,
    categories: [{ id: "cat-026", title: "General Admission", color: "#c0392b" }],
  },
];

const getVenuesDetailed = async (): Promise<VenueDetailed[]> => {
  return Promise.resolve(mockVenuesDetailed);
};

export { getVenuesDetailed };
