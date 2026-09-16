import { httpClient } from "@/shared/api";
import type { Venue } from "../model/types";

export const getVenues = async (): Promise<Venue[]> => {
  return Promise.resolve([
    { id: "1", title: "Venue 1", address: "Address 1", description: "Description 1" },
    { id: "2", title: "Venue 2", address: "Address 2", description: "Description 2" },
    { id: "3", title: "Venue 3", address: "Address 3", description: "Description 3" },
    { id: "4", title: "Venue 4", address: "Address 4", description: "Description 4" },
    { id: "5", title: "Venue 5", address: "Address 5", description: "Description 5" },
    { id: "6", title: "Venue 6", address: "Address 6", description: "Description 6" },
    { id: "7", title: "Venue 7", address: "Address 7", description: "Description 7" },
    { id: "8", title: "Venue 8", address: "Address 8", description: "Description 8" },
  ]);
  return httpClient<Venue[]>("/venues");
};
