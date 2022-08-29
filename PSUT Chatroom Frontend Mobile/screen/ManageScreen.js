import React, { useState } from "react";
import { View, StyleSheet } from "react-native";
import { Button, Modal, Portal, Text } from "react-native-paper";
import { PSUT_ChatroomApi } from "../api/routeApi";
import LoadingScreen from "../componnetns/LoadingScreen";
import { Cache } from "../helpers/cache";

const ManageScreen = () => {
  const [modalVisible, setModalVisible] = useState(false);
  const [backupFileName, setBackupFileName] = useState("");
  const [loading, setLoading] = useState(false);

  const onBackup = async () => {
    setLoading(true);

    try {
      const name = await PSUT_ChatroomApi.createBackup();
      console.log(name.data);
      setLoading(false);
      setBackupFileName(name.data);
    } catch (e) {
      setLoading(false);
      if (e?.response?.status === 403)
        alert("You don't have permission to create backup");
      else alert("Failed to create the backup");
    }
  };

  const onPatch = async () => {
    setLoading(true);
    try {
      await PSUT_ChatroomApi.patchDatabase();
      setLoading(false);
      alert("Patched Successfully");
    } catch (e) {
      setLoading(false);
      if (e?.response?.status === 403)
        alert("You don't have permission to patch the database");
      else alert("Failed to patch the database");
    }
  };

  const onClear = async () => {
    setModalVisible(false);
    setLoading(true);

    try {
      await PSUT_ChatroomApi.clearDatabase();
      setLoading(false);
      alert("Cleared Successfully");
    } catch (e) {
      setLoading(false);
      if (e?.response?.status === 403)
        alert("You don't have permission to clear the database");
      else alert("Failed to clear the database");
    }
  };

  return (
    <View style={styles.container}>
      <Portal>
        <Modal visible={modalVisible} onDismiss={() => setModalVisible(false)}>
          <View style={styles.modal_ClearDbWarning}>
            <Text style={{ fontSize: 16, textAlign: "center" }}>
              Are you sure you want to continue?
            </Text>
            <Text style={{ fontSize: 16, textAlign: "center" }}>
              This action cannot be undone!
            </Text>

            <View style={styles.modal_btnsContainer}>
              <Button color="crimson" onPress={() => onClear()}>
                Delete, All
              </Button>
              <Button onPress={() => setModalVisible(false)}>Cancel</Button>
            </View>
          </View>
        </Modal>
      </Portal>
      <Portal>
        <Modal
          visible={!!backupFileName}
          onDismiss={() => setBackupFileName("")}
        >
          <View style={styles.modal_ClearDbWarning}>
            <Text style={{ fontSize: 16, textAlign: "center" }}>
              Database backup completed successfully
            </Text>
            <Text
              style={{
                fontSize: 16,
                marginTop: 6,
                textAlign: "center",
                color: "crimson",
              }}
            >
              FILE: {backupFileName}
            </Text>
            <View style={styles.modal_btnsContainer}>
              <Button
                style={{ width: "100%" }}
                onPress={() => setBackupFileName("")}
              >
                OK
              </Button>
            </View>
          </View>
        </Modal>
      </Portal>
      <LoadingScreen visible={loading} />
      <Text style={styles.title}>SYSTEM ADMINISTRATION</Text>
      <View>
        <Button onPress={() => onBackup()} style={styles.btn} mode="contained">
          Create Database Backup
        </Button>
        <Button onPress={() => onPatch()} style={styles.btn} mode="contained">
          Patch Database
        </Button>
        <Button
          onPress={() => setModalVisible(true)}
          color="#cc0000"
          style={styles.btn}
          mode="contained"
        >
          Clear Database
        </Button>
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    alignItems: "center",
    paddingVertical: 36,
    paddingHorizontal: 30,
  },
  title: {
    fontSize: 24,
    marginBottom: 14,
  },
  btn: {
    padding: 10,
    marginTop: 20,
  },
  modal_ClearDbWarning: {
    backgroundColor: "white",
    justifyContent: "center",
    alignItems: "center",
    padding: 15,
    paddingTop: 20,
    margin: 15,
    borderRadius: 6,
  },
  modal_btnsContainer: {
    flexDirection: "row",
    justifyContent: "center",
    marginTop: 20,
  },
});

export default ManageScreen;
