import React, { useState } from "react";
import { Platform, ScrollView, StatusBar, Text, View } from "react-native";
import { styles } from "../../style/style";
import LottieView from "lottie-react-native";
import { Button, HelperText, TextInput } from "react-native-paper";
import Gap from "../../componnetns/Gap";
import { userRoute } from "../../api/routeApi";
import * as GoogleSignIn from "expo-google-sign-in";
import * as SecureStore from "expo-secure-store";
import { Cache } from "../../helpers/cache";

// TODO ADD Google Sign in

const Login = ({ navigation }) => {
  const [userData, setUserData] = useState({
    email: "",
    password: "",
  });

  const [error, setError] = useState("");

  const signInAsync = async () => {
    const token = await SecureStore.getItemAsync("token");
    console.log("token------ from storage", token);
    if (!!token) {
      try {
        const { data } = await userRoute.GetUser();
        await SecureStore.setItemAsync("user", JSON.stringify(data));
        Cache.userData = data;
        console.log(Cache.userData);
        return navigation.navigate("TopStack");
      } catch (e) {
        console.log("else");
        await newLogin();
      }
    } else {
      await newLogin();
    }
  };

  const newLogin = async () => {
    try {
      if (
        userData.password !== "123" ||
        !userData.email.endsWith("@std.psut.edu.jo")
      ) {
        setError("Invalid Credentials");
        return;
      } else {
        setError("");
      }

      const { data } = await userRoute.LoginAuth(userData);
      console.log("token------ on login", data.token);
      await SecureStore.setItemAsync("token", data.token);
      await SecureStore.setItemAsync("user", JSON.stringify(data.user));
      Cache.userData = data.user;
      console.log(Cache.userData);
      navigation.navigate("TopStack");
    } catch {
      try {
        await userRoute.Logout();
        await SecureStore.setItemAsync("token", "");
        await SecureStore.setItemAsync("user", "");
        Cache.userData = null;
      } catch (error) {
        alert("Invalid Credentials");
      }
    }
  };

  return (
    <View style={styles.container}>
      <StatusBar backgroundColor="#128C7E" barStyle="light-content" />
      <View style={styles.header}>
        <LottieView
          loop={true}
          autoPlay={true}
          speed={0.4}
          style={{
            width: 200,
            height: 200,
            backgroundColor: "#128C7E",
          }}
          source={require("../../assets/jsonfile/Messenger.json")}
        />
        <Text style={styles.text_header}>{"PSUT Chatroom"}</Text>
      </View>
      <View style={styles.footer}>
        <ScrollView>
          <HelperText style={styles.helperTextStyle} type="info">
            Email Address
          </HelperText>
          <TextInput
            error={error}
            multiline={false}
            mode="outlined"
            enablesReturnKeyAutomatically={true}
            selectionColor={"#128C7E"}
            style={styles.textInputstyle}
            value={userData.email}
            onChangeText={(text) => setUserData({ ...userData, email: text })}
          />
          <HelperText style={styles.helperTextStyle} type="info">
            Password
          </HelperText>
          <TextInput
            error={error}
            multiline={false}
            mode="outlined"
            enablesReturnKeyAutomatically={true}
            selectionColor={"#128C7E"}
            style={styles.textInputstyle}
            value={userData.password}
            onChangeText={(text) =>
              setUserData({ ...userData, password: text })
            }
          />

          <Gap gapValue="10" type="col" />

          <Button
            mode="contained"
            theme={{ roundness: 10 }}
            labelStyle={{ fontFamily: "AppNormal" }}
            contentStyle={styles.loginButton}
            onPress={signInAsync}
          >
            LOGIN
          </Button>
        </ScrollView>
      </View>
    </View>
  );
};

export default Login;
