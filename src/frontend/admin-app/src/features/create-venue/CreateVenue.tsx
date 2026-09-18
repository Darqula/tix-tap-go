import type { CreateVenueRequestData } from "@/entities/venues";
import { CreateVenueForm } from "./ui/CreateVenueForm";
import { useCallback } from "react";
import { useCreateVenue } from "@/shared/api/generated/venues/api";

type CreateVenueProps = {
    isOpen: boolean;
    onClose: () => void;
}

const CreateVenue = ({ isOpen, onClose }: CreateVenueProps) => {
    const mutation = useCreateVenue();
    const onSubmit = useCallback((data: CreateVenueRequestData) => {
        mutation.mutate({ data });
    }, [mutation]);

    return (
        <CreateVenueForm isOpen={isOpen} onClose={onClose} onSubmit={onSubmit} />
    );
}

export { CreateVenue }