import { StyleSheet, Platform } from "react-native";
import {
  widthPercentageToDP as wp,
  heightPercentageToDP as hp,
} from "react-native-responsive-screen";

export const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "#128C7E",
  },
  header: {
    flex: 3,
    justifyContent: "center",
    alignItems: "center",
    paddingHorizontal: 20,
    paddingBottom: 50,
    backgroundColor: "#128C7E",
  },
  footer: {
    flex: Platform.OS === "ios" ? 3 : 5,
    backgroundColor: "#fff",
    borderTopLeftRadius: 30,
    borderTopRightRadius: 30,
    paddingHorizontal: 20,
    paddingVertical: 30,
  },
  text_header: {
    color: "#fff",
    // fontWeight: "bold",
    fontSize: 26,
    fontFamily: "AppLight",
  },
  textInputstyle: {
    // textAlign: "right",
    height: hp("6%"),
    lineHeight: hp(10),
  },
  helperTextStyle: {
    // textAlign: "right",
    fontFamily: "AppNormal",
    fontSize: 14,
    color: "#000",
    marginTop: 15,
  },
  loginButton: {
    height: hp("6%"),
    display: "flex",
    justifyContent: "center",
    alignItems: "center",
    textAlignVertical: "center",
    fontFamily: "AppNormal",
  },
  rowView: {
    display: "flex",
    flexDirection: "row",
  },

  reqFont: {
    fontFamily: "AppNormal",
    color: "#000",
  },
  fab: {
    position: "absolute",
    margin: 16,
    right: 0,
    bottom: 0,
  },
});
