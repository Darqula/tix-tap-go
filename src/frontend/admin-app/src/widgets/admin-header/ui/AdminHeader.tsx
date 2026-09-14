import { AppBar, Toolbar, Typography } from "@/shared/ui";
import { appConfig } from "@/shared/config";

export function AdminHeader() {
  return (
    <AppBar position="static" enableColorOnDark>
      <Toolbar>
        <Typography variant="h6" component="div">
          {appConfig.appName}
        </Typography>
      </Toolbar>
    </AppBar>
  );
}
