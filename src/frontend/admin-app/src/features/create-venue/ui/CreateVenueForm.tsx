import type { CreateVenueRequestData } from "@/entities/venues";
import { TextField } from "@/shared/ui";
import { Button, Dialog, DialogTitle, Stack } from "@mui/material";
import { Controller, useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";

type CreateVenuFormProps = {
    isOpen: boolean;
    onClose: () => void;
    onSubmit: (data: CreateVenueRequestData) => void;
}

const CreateVenueForm = ({ isOpen, onClose, onSubmit }: CreateVenuFormProps) => {
    const { t } = useTranslation();
    const { control, handleSubmit } = useForm<CreateVenueRequestData>({
        defaultValues: {
            title: "",
            address: "",
            description: null
        },
    });

    return (
        <Dialog open={isOpen} onClose={onClose}>
            <DialogTitle>{t("Venues.Create")}</DialogTitle>
            <form onSubmit={handleSubmit(onSubmit)}>
                <Stack direction="column" spacing={2} sx={{ paddingBottom: 2, paddingX: 2 }}>
                    <Controller
                        name="title"
                        control={control}
                        rules={{ required: true }}
                        render={({ field }) => <TextField size="small" label="Title" required {...field} />}
                    />
                    <Controller
                        name="address"
                        control={control}
                        rules={{ required: true }}
                        render={({ field }) => <TextField size="small" label="Address" required {...field} />}
                    />
                    <Controller
                        name="description"
                        control={control}
                        render={({ field }) => <TextField size="small" label="Description" {...field} />}
                    />

                    <Stack direction="row" spacing={1}>
                        <Button size="small" variant="contained" type="submit">
                            {t("General.Create")}
                        </Button>
                        <Button size="small" variant="outlined" onClick={onClose}>
                            {t("General.Cancel")}
                        </Button>
                    </Stack>
                </Stack>
            </form>
        </Dialog>
    );
};

export { CreateVenueForm };