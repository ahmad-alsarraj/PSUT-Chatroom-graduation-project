import React from "react";
import { createMaterialTopTabNavigator } from "@react-navigation/material-top-tabs";
import { Dimensions } from "react-native";
import Chat from "../screen/chatScreen/Chat";
import UserProfile from "../screen/chatScreen/UserProfile";
import ManageScreen from "../screen/ManageScreen";
import { Cache } from "../helpers/cache";

const TopStack = () => {
  const Tab = createMaterialTopTabNavigator();
  return (
    <Tab.Navigator
      screenOptions={{
        tabBarItemStyle: {
          backgroundColor: "#128C7E",
        },
      }}
      initialLayout={{ width: Dimensions.get("window").width }}
    >
      {Cache.userData.role !== "Admin" && (
        <Tab.Screen
          name="Chat"
          options={{
            tabBarLabelStyle: {
              fontSize: 14,
              fontFamily: "AppNormal",
              color: "white",
            },
            tabBarActiveTintColor: "red",
            tabBarInactiveTintColor: "#ABACB0",
            tabBarAllowFontScaling: true,
            title: "Groups",
            tabBarBounces: true,
            lazy: true,
            tabBarIndicatorStyle: {
              backgroundColor: "red",
            },
          }}
          component={Chat}
        />
      )}
      {Cache.userData.role === "Admin" && (
        <Tab.Screen
          name="manageTab"
          options={{
            tabBarLabelStyle: {
              fontSize: 14,
              fontFamily: "AppNormal",
              color: "white",
            },
            tabBarActiveTintColor: "red",
            tabBarInactiveTintColor: "#ABACB0",
            tabBarAllowFontScaling: true,
            title: "Manage",
            tabBarBounces: true,
            lazy: true,
            tabBarIndicatorStyle: {
              backgroundColor: "red",
            },
          }}
          component={ManageScreen}
        />
      )}
      <Tab.Screen
        name="userProfile"
        options={{
          tabBarLabelStyle: {
            fontSize: 14,
            fontFamily: "AppNormal",
            color: "white",
          },
          tabBarActiveTintColor: "red",
          tabBarInactiveTintColor: "#ABACB0",
          tabBarAllowFontScaling: true,
          title: "Profile",
          tabBarBounces: true,
          lazy: true,
          tabBarIndicatorStyle: {
            backgroundColor: "red",
          },
        }}
        component={UserProfile}
      />
    </Tab.Navigator>
  );
};

export default TopStack;
