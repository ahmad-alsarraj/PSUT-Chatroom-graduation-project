import { Provider as PaperProvider, DefaultTheme } from "react-native-paper";
import { StatusBar } from "expo-status-bar";
import { NavigationContainer } from "@react-navigation/native";
import Stack from "./routeStack/Stack";
import { useFonts } from "expo-font";
import {
  SafeAreaProvider,
  SafeAreaView,
  initialWindowMetrics,
} from "react-native-safe-area-context";

export default function App() {
  const theme = {
    ...DefaultTheme,
    colors: {
      ...DefaultTheme.colors,
      primary: "#128C7E",
      accent: "#25D366",
    },
  };

  const [loaded] = useFonts({
    AppNormal: require("./assets/font/Almarai-Regular.ttf"),
    AppLight: require("./assets/font/Almarai-Light.ttf"),
  });

  if (!loaded) return null;

  return (
    <SafeAreaProvider initialMetrics={initialWindowMetrics}>
      <SafeAreaView style={{ flex: 1 }} edges={["top", "left", "right"]}>
        <PaperProvider theme={theme}>
          <NavigationContainer>
            <Stack />
          </NavigationContainer>
        </PaperProvider>
      </SafeAreaView>
    </SafeAreaProvider>
  );
}
