import { AppProviders } from "./providers/AppProviders";
import { DashboardPage } from "@/pages/dashboard";

function App() {
  return (
    <AppProviders>
      <DashboardPage />
    </AppProviders>
  );
}

export default App;
