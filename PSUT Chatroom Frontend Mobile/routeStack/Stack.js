import React from "react";
import {createNativeStackNavigator} from "@react-navigation/native-stack";
import Login from "../screen/authScreen/Login";
import TopStack from "./TopStack";
import {View, Text} from "react-native";
import Meassging from "../screen/chatScreen/Meassging";
import {Button} from "react-native-paper";
import {userRoute} from "../api/routeApi";
import * as SecureStore from "expo-secure-store";
import {useNavigation} from "@react-navigation/native";
import {Cache} from "../helpers/cache";

const Stack = () => {
    const StackApp = createNativeStackNavigator();

    const nav = useNavigation();

    const onLogout = async () => {
        await SecureStore.setItemAsync("token", "");
        await SecureStore.setItemAsync("user", "");
        Cache.userData = null;

        try {
            await userRoute.Logout();
        } catch (error) {
        }

        nav.reset({
            index: 0,
            routes: [{name: "Login"}],
        });
    };

    return (
        <StackApp.Navigator initialRouteName="Login">
            <StackApp.Screen
                options={{headerShown: false}}
                name="Login"
                component={Login}
            />
            <StackApp.Screen
                name="TopStack"
                options={{
                    headerTitleAlign: "center",
                    headerTitle: "PSUT Chatroom",
                    title: "PSUT Chatroom",
                    headerBackVisible: false,
                    headerLeft: () => (
                        <View>
                            <Button icon={"logout"} onPress={onLogout}>
                                <Text style={{fontFamily: "AppNormal"}}>Logut</Text>
                            </Button>
                        </View>
                    ),
                    headerTitleStyle: {fontFamily: "AppNormal", fontSize: 16},
                }}
                component={TopStack}
            />
            <StackApp.Screen
                name="Meassge"
                component={Meassging}
                options={{
                    headerTitleAlign: "center",
                    headerTitle: "Conversations",
                    title: "Conversations",
                    headerTitleStyle: {fontFamily: "AppNormal", fontSize: 16},
                }}
            />
        </StackApp.Navigator>
    );
};

export default Stack;
