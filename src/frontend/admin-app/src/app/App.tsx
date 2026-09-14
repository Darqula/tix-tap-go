import { AppProviders } from "./providers/AppProviders";
import { DashboardPage } from "@/pages/dashboard";
import './locales/i18n';

function App() {
  return (
    <AppProviders>
      <DashboardPage />
    </AppProviders>
  );
}

export default App;
