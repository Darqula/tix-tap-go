import { Children, useCallback, useState, type ReactElement, type ReactNode } from "react";
import { AppBar, Box } from "@/shared/ui";
import { Tab, Tabs } from "@mui/material";
import { useTranslation } from "react-i18next";
import { Logo } from "./Logo";

type TabItemProps = {
  title: string;
  children: ReactNode;
};

export const TabItem = ({ children }: TabItemProps) => {
  return children;
};

export const TabLayout = ({
  children,
}: {
  children: ReactElement<TabItemProps> | ReactElement<TabItemProps>[];
}) => {
  const { t } = useTranslation();
  const [currentTab, setCurrentTab] = useState(0);
  const handleTabChange = useCallback((_event: React.SyntheticEvent, newValue: number) => {
    setCurrentTab(newValue);
  }, []);
  const items = Children.toArray(children) as ReactElement<TabItemProps>[];

  return (
    <Box sx={{ minHeight: "100dvh", bgcolor: "background.default" }}>
      <AppBar
        position="static"
        enableColorOnDark
        sx={{ display: "flex", flexDirection: "row", alignItems: "center", px: 2, gap: 3 }}
      >
        <Logo />
        <Tabs value={currentTab} onChange={handleTabChange}>
          {items.map((child) => (
            <Tab key={child.props.title} label={t(child.props.title)} />
          ))}
        </Tabs>
      </AppBar>
      {items.map((child, index) => (
        <Box key={child.props.title} sx={{ display: currentTab === index ? "block" : "none" }}>
          {child}
        </Box>
      ))}
    </Box>
  );
};
