import React from "react";
import { View } from "react-native";
import { ActivityIndicator, Modal, Portal, Text } from "react-native-paper";

const LoadingScreen = ({ visible }) => {
  return (
    <Portal>
      <Modal visible={visible}>
        <View style={{ alignItems: "center", justifyContent: "center" }}>
          <ActivityIndicator animating={true} size="large" color="white" />
          <Text style={{ fontSize: 20, color: "white", marginTop: 20 }}>
            Processing
          </Text>
        </View>
      </Modal>
    </Portal>
  );
};

export default LoadingScreen;
