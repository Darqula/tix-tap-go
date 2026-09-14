import { Button, Stack, Typography } from "@/shared/ui";
import { useCounterStore } from "../model/counter-store";

export function CounterPanel() {
  const count = useCounterStore((state) => state.count);
  const increment = useCounterStore((state) => state.increment);
  const reset = useCounterStore((state) => state.reset);

  return (
    <Stack spacing={2} sx={{ alignItems: "flex-start" }}>
      <Typography variant="h6">Counter: {count}</Typography>
      <Stack direction="row" spacing={1}>
        <Button variant="contained" onClick={increment}>
          Increment
        </Button>
        <Button variant="outlined" onClick={reset}>
          Reset
        </Button>
      </Stack>
    </Stack>
  );
}
