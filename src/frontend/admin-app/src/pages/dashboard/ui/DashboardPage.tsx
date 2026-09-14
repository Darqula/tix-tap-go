import { Box, Container, Paper, Typography } from "@/shared/ui";
import { AdminHeader } from "@/widgets/admin-header";
import { CounterPanel } from "@/features/counter";

export function DashboardPage() {
  return (
    <Box sx={{ minHeight: "100dvh", bgcolor: "background.default" }}>
      <AdminHeader />
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Typography variant="h4" gutterBottom>
          Dashboard
        </Typography>
        <Paper sx={{ p: 3 }}>
          <CounterPanel />
        </Paper>
      </Container>
    </Box>
  );
}
