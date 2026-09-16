import { AppProviders } from "./providers/AppProviders";
import { TabLayout } from "@/widgets/tab-layout";
import { TabItem } from "@/widgets/tab-layout/ui/TabLayout";
import { VenuesPage } from "@/pages/venues/VenuesPage";
import "./locales/i18n";

const App = () => {
  return (
    <AppProviders>
      <TabLayout>
        <TabItem title="Venues.TabTitle">
          <VenuesPage />
        </TabItem>
      </TabLayout>
    </AppProviders>
  );
};

export default App;
