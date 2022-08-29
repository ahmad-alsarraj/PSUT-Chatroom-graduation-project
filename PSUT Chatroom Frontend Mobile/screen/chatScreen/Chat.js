import { useNavigation } from "@react-navigation/native";
import React, { useState, useEffect, useCallback } from "react";
import { View, FlatList, RefreshControl, StyleSheet } from "react-native";
import ChatCard from "../../componnetns/ChatCard";
import Gap from "../../componnetns/Gap";
import { styles } from "../../style/style";
import {
  FAB,
  Modal,
  Portal,
  Text,
  Button,
  Card,
  TouchableRipple,
  TextInput,
  IconButton,
} from "react-native-paper";
import { PSUT_ChatroomApi } from "../../api/routeApi";
import { Cache } from "../../helpers/cache";
import LoadingScreen from "../../componnetns/LoadingScreen";
import { heightPercentageToDP as hp } from "react-native-responsive-screen";

const groupSettingState = {
  Hidden: 1,
  Options: 2,
  ChangeName: 3,
  DeleteGroup: 4,
  AddMember: 5,
  RemoveMember: 6,
  GroupMembers: 7,
  SelectMemberToAdd: 8,
};

const Chat = () => {
  const [refreshing, setRefreshing] = useState(false);
  const [showFab, setShowFab] = useState(true);
  const [groupsList, setGroupsList] = useState([]);
  const [admins, setAdmins] = useState([]);
  const [groupsMembers, setGroupsMembers] = useState({});
  const [pingModalVisible, setPingModalVisible] = useState(false);
  const [createGroupModalVisible, setCreateGroupModalVisible] = useState(false);
  const [newGroupName, setNewGroupName] = useState("");
  const [loading, setLoading] = useState(false);
  const [groupSetting, setGroupSetting] = useState({
    currentState: groupSettingState.Hidden,
  });
  const [newMembers, setNewMembers] = useState(null);
  const nav = useNavigation();

  const [state, setState] = useState({ open: false });
  const onStateChange = ({ open }) => setState({ open });
  const { open } = state;

  const fabActions =
    Cache.userData.role === "Student"
      ? [
          {
            icon: "bell",
            label: "Ping Instructor",
            onPress: () => togglePingModal(true),
            small: false,
          },
        ]
      : Cache.userData.role === "Instructor"
      ? [
          {
            icon: "message-plus-outline",
            label: "Create new group",
            onPress: () => setCreateGroupModalVisible(true),
            small: false,
          },
        ]
      : [];

  const onCreateGroup = async () => {
    if (!newGroupName) return;
    setCreateGroupModalVisible(false);
    setLoading(true);
    try {
      const { data } = await PSUT_ChatroomApi.createGroup(newGroupName);
      await getGroupsList();
      setLoading(false);
      setNewGroupName("");
      alert(`Created Successfully, with ID: ${data.id}`);
    } catch (error) {
      setLoading(false);
      alert("Failed to create");
    }
  };

  const onUpdateGroup = async () => {
    if (!newGroupName) return;
    setGroupSetting({
      ...groupSetting,
      currentState: groupSettingState.Hidden,
    });
    setLoading(true);
    try {
      await PSUT_ChatroomApi.updateGroup(groupSetting.id, newGroupName);
      await getGroupsList();
      setLoading(false);
      setNewGroupName("");
      alert(`Updated Successfully`);
    } catch (error) {
      setLoading(false);
      alert("Failed to update");
    }
  };

  const onDeleteGroup = async () => {
    const groupId = groupSetting.id;
    setGroupSetting({ currentState: groupSettingState.Hidden });
    setLoading(true);
    try {
      await PSUT_ChatroomApi.deleteGroup([groupId]);
      await getGroupsList();
      setLoading(false);
      alert(`Deleted Successfully`);
    } catch (error) {
      setLoading(false);
      alert("Failed to delete");
    }
  };

  const onMemberRemove = async () => {
    const groupId = groupSetting.id;
    const userId = groupSetting.memberToRemove.id;
    setGroupSetting({ currentState: groupSettingState.Hidden });
    setLoading(true);
    try {
      await PSUT_ChatroomApi.removeMember({ groupId, userId });
      await getGroupsList();
      setLoading(false);
      alert(`Removed Successfully`);
    } catch (error) {
      setLoading(false);
      alert("Failed to remove");
    }
  };

  const onMemberAdd = async () => {
    const groupId = groupSetting.id;
    const userId = groupSetting.memberToAdd.id;
    setGroupSetting({ currentState: groupSettingState.Hidden });
    setLoading(true);
    try {
      await PSUT_ChatroomApi.addMember({ groupId, userId, isAdmin: false });
      await getGroupsList();
      setLoading(false);
      alert(`Added Successfully`);
    } catch (error) {
      setLoading(false);
      alert("Failed to add");
    }
  };

  const fetchNewMembers = async () => {
    try {
      const { data } = await PSUT_ChatroomApi.getNonExistingMembers(groupSetting.id);
      setNewMembers(data);
    } catch (error) {
      setNewMembers([]);
    }
  };

  const filterDataDto = (array = []) => {
    const mGroups = [];
    const mAdmins = [];
    const mGroupsMembers = {};
    for (const group of array) {
      mGroups.push({
        id: group.id,
        name: group.name,
        conversationId: group.conversationId,
        avatar_url: `https://source.unsplash.com/random?sig=${group.id}`,
        adminName: group.members.find((m) => m.isAdmin).user.name,
        adminId: group.members.find((m) => m.isAdmin).user.id,
      });

      mGroupsMembers[group.id] = [
        ...group.members.filter((m) => !m.isAdmin).map((u) => u.user),
      ];

      mAdmins.push(
        ...group.members
          .filter((m) => m.isAdmin && !mAdmins.find((a) => a.id === m.user.id))
          .map((u) => u.user)
      );
    }

    setGroupsList(mGroups);
    setAdmins(mAdmins);
    setGroupsMembers(mGroupsMembers);
  };

  useEffect(() => {
    nav.addListener("blur", () => setShowFab(false));
    const cleanUp = nav.addListener("focus", () => {
      setShowFab(true);
      getGroupsList();
    });

    return () => cleanUp();
  }, [nav]); 

  const getGroupsList = async () => {
    try {
      const res = await PSUT_ChatroomApi.getGroups();
      filterDataDto(res.data);
    } catch (error) {
      console.log(error);
    }
  };

  const onRefresh = useCallback(() => {
    setRefreshing(true);
    getGroupsList().then(
      response => {
        setRefreshing(false);
      }, error => {
        setRefreshing(false);
      }
    );
  }, []);

  const onCardPress = async (groupData) => {
    nav.navigate("Meassge", groupData);
  };

  const onCardLongPress = async (groupData) => {
    if (Cache.userData.id === groupData.adminId) {
      setGroupSetting({
        ...groupData,
        currentState: groupSettingState.Options,
      });
    } else {
      alert("Only group admin can access it's settings");
    }
  };

  async function pingInstructor(instructorId) {
    togglePingModal();
    const dto = {
      content: "PING",
      recipientId: instructorId,
    };

    console.log(dto);
    try {
      await PSUT_ChatroomApi.sendPing(dto);
      alert("Ping sent successfully");
    } catch (e) {
      if (e?.response?.status === 422) {
        alert("Selected instructor has already been pinged before");
      }
      console.log(JSON.stringify(e));
    }
  }

  const togglePingModal = (state = undefined) =>
    setPingModalVisible((pre) => (state !== undefined ? state : !pre));

  return (
    <View style={[styles.container, { backgroundColor: "#F8F8F8" }]}>
      <LoadingScreen visible={loading} />
      {/* Delete */}
      <Portal>
        <Modal
          visible={groupSetting.currentState === groupSettingState.DeleteGroup}
          contentContainerStyle={screenStyles.pingModalContainerStyle}
        >
          <View style={screenStyles.pingModalContent}>
            <Text style={{ fontSize: 18, marginBottom: 20 }}>
              Are you sure you want to delete the group?
            </Text>
          </View>
          <View
            style={{ flexDirection: "row", justifyContent: "space-between" }}
          >
            <Button
              style={{ padding: 8 }}
              onPress={() =>
                setGroupSetting({ currentState: groupSettingState.Hidden })
              }
            >
              Cancel
            </Button>
            <Button
              color="crimson"
              style={{ padding: 8 }}
              onPress={() => onDeleteGroup()}
            >
              Yes, Delete it!
            </Button>
          </View>
        </Modal>
      </Portal>
      {/* Remove Member */}
      <Portal>
        <Modal
          visible={groupSetting.currentState === groupSettingState.RemoveMember}
          contentContainerStyle={screenStyles.pingModalContainerStyle}
        >
          <View style={screenStyles.pingModalContent}>
            <Text style={{ fontSize: 18, marginBottom: 20 }}>
              Are you sure you want to remove{" "}
              {groupSetting?.memberToRemove?.name} from the group?
            </Text>
          </View>
          <View
            style={{ flexDirection: "row", justifyContent: "space-between" }}
          >
            <Button
              style={{ padding: 8 }}
              onPress={() =>
                setGroupSetting({ currentState: groupSettingState.Hidden })
              }
            >
              Cancel
            </Button>
            <Button
              color="crimson"
              style={{ padding: 8 }}
              onPress={() => onMemberRemove()}
            >
              Yes, Remove!
            </Button>
          </View>
        </Modal>
      </Portal>
      {/* Add Member */}
      <Portal>
        <Modal
          visible={groupSetting.currentState === groupSettingState.AddMember}
          contentContainerStyle={screenStyles.pingModalContainerStyle}
        >
          <View style={screenStyles.pingModalContent}>
            <Text style={{ fontSize: 18, marginBottom: 20 }}>
              Are you sure you want to add {groupSetting?.memberToAdd?.name} to
              the group?
            </Text>
          </View>
          <View
            style={{ flexDirection: "row", justifyContent: "space-between" }}
          >
            <Button
              style={{ padding: 8 }}
              onPress={() =>
                setGroupSetting({ currentState: groupSettingState.Hidden })
              }
            >
              Cancel
            </Button>
            <Button
              color="green"
              style={{ padding: 8 }}
              onPress={() => onMemberAdd()}
            >
              Yes, Add
            </Button>
          </View>
        </Modal>
      </Portal>
      {/* Change Name */}
      <Portal>
        <Modal
          visible={groupSetting.currentState === groupSettingState.ChangeName}
          contentContainerStyle={screenStyles.pingModalContainerStyle}
        >
          <View style={screenStyles.pingModalContent}>
            <Text style={{ fontSize: 18 }}>Change Group Name</Text>
            <TextInput
              placeholder="New Group Name"
              multiline={false}
              mode="outlined"
              style={{ height: 48, marginVertical: 12 }}
              value={newGroupName}
              onChangeText={(text) => setNewGroupName(text)}
            />
          </View>
          <View
            style={{ flexDirection: "row", justifyContent: "space-between" }}
          >
            <Button
              style={{ padding: 8 }}
              onPress={() =>
                setGroupSetting({ currentState: groupSettingState.Hidden })
              }
            >
              Cancel
            </Button>
            <Button style={{ padding: 8 }} onPress={() => onUpdateGroup()}>
              Submit
            </Button>
          </View>
        </Modal>
      </Portal>
      {/* Members */}
      <Portal>
        <Modal
          visible={groupSetting.currentState === groupSettingState.GroupMembers}
          contentContainerStyle={screenStyles.pingModalContainerStyle}
        >
          <View style={screenStyles.pingModalContent}>
            <Text style={{ fontSize: 18, textAlign: "center" }}>
              Group Members
            </Text>
            <FlatList
              style={{ maxHeight: hp(60) }}
              data={groupsMembers[groupSetting.id]}
              ListEmptyComponent={
                <Text style={{ paddingLeft: 10, textAlign: "center" }}>
                  No members yet
                </Text>
              }
              renderItem={({ item, index }) => (
                <Card style={{ paddingLeft: 10 }} key={item.id}>
                  <View
                    style={{
                      flexDirection: "row",
                      justifyContent: "space-between",
                      alignItems: "center",
                    }}
                  >
                    <Text>{item.name}</Text>
                    <IconButton
                      color="crimson"
                      icon="close"
                      onPress={() => {
                        setGroupSetting({
                          ...groupSetting,
                          currentState: groupSettingState.RemoveMember,
                          memberToRemove: item,
                        });
                      }}
                    />
                  </View>
                </Card>
              )}
              ItemSeparatorComponent={() => <Gap gapValue="1" type="col" />}
              keyExtractor={(item) => item.id.toString()}
              ListFooterComponent={() => <Gap gapValue="2" type="col" />}
              ListHeaderComponent={() => <Gap gapValue="1" type="col" />}
            />
          </View>
          <Button
            onPress={() =>
              setGroupSetting({ currentState: groupSettingState.Hidden })
            }
            style={screenStyles.pingModalButton}
          >
            Close
          </Button>
        </Modal>
      </Portal>
      {/* Select Member to add */}
      <Portal>
        <Modal
          visible={
            groupSetting.currentState === groupSettingState.SelectMemberToAdd
          }
          contentContainerStyle={screenStyles.pingModalContainerStyle}
        >
          <View style={screenStyles.pingModalContent}>
            <Text style={{ fontSize: 18, textAlign: "center" }}>
              Add New Group Member
            </Text>
            <FlatList
              style={{ maxHeight: hp(60) }}
              data={newMembers}
              ListEmptyComponent={
                newMembers === null ? (
                  <Text
                    style={{
                      paddingLeft: 10,
                      fontSize: 20,
                      textAlign: "center",
                    }}
                  >
                    Loading, Please Wait...
                  </Text>
                ) : (
                  <Text style={{ paddingLeft: 10, textAlign: "center" }}>
                    No members to add
                  </Text>
                )
              }
              renderItem={({ item, index }) => (
                <Card style={{ paddingLeft: 10 }} key={item.id}>
                  <View
                    style={{
                      flexDirection: "row",
                      justifyContent: "space-between",
                      alignItems: "center",
                    }}
                  >
                    <Text>{item.name}</Text>
                    <IconButton
                      color="green"
                      icon="check"
                      onPress={() => {
                        setGroupSetting({
                          ...groupSetting,
                          currentState: groupSettingState.AddMember,
                          memberToAdd: item,
                        });
                      }}
                    />
                  </View>
                </Card>
              )}
              ItemSeparatorComponent={() => <Gap gapValue="1" type="col" />}
              keyExtractor={(item) => item.id.toString()}
              ListFooterComponent={() => <Gap gapValue="2" type="col" />}
              ListHeaderComponent={() => <Gap gapValue="1" type="col" />}
            />
          </View>
          <Button
            onPress={() =>
              setGroupSetting({ currentState: groupSettingState.Hidden })
            }
            style={screenStyles.pingModalButton}
          >
            Close
          </Button>
        </Modal>
      </Portal>
      {/* Settings */}
      <Portal>
        <Modal
          visible={groupSetting.currentState === groupSettingState.Options}
          contentContainerStyle={screenStyles.pingModalContainerStyle}
        >
          <View style={screenStyles.pingModalContent}>
            <Text
              style={{ fontSize: 18, marginBottom: 10, textAlign: "center" }}
            >
              Group Settings
            </Text>
            <TouchableRipple
              onPress={() =>
                setGroupSetting({
                  ...groupSetting,
                  currentState: groupSettingState.ChangeName,
                })
              }
            >
              <Card style={{ padding: 16, marginVertical: 3 }}>
                <Text>Change Group Name</Text>
              </Card>
            </TouchableRipple>
            <TouchableRipple
              onPress={() =>
                setGroupSetting({
                  ...groupSetting,
                  currentState: groupSettingState.GroupMembers,
                })
              }
            >
              <Card style={{ padding: 16, marginVertical: 3 }}>
                <Text>Members</Text>
              </Card>
            </TouchableRipple>
            <TouchableRipple
              onPress={() => {
                setGroupSetting({
                  ...groupSetting,
                  currentState: groupSettingState.SelectMemberToAdd,
                });
                fetchNewMembers();
              }}
            >
              <Card style={{ padding: 16, marginVertical: 3 }}>
                <Text>Add New Member</Text>
              </Card>
            </TouchableRipple>
            <TouchableRipple
              onPress={() =>
                setGroupSetting({
                  ...groupSetting,
                  currentState: groupSettingState.DeleteGroup,
                })
              }
            >
              <Card mode="elevated" style={{ padding: 16, marginVertical: 3 }}>
                <Text>Delete Group</Text>
              </Card>
            </TouchableRipple>
          </View>
          <Button
            onPress={() =>
              setGroupSetting({ currentState: groupSettingState.Hidden })
            }
            style={screenStyles.pingModalButton}
          >
            Cancel
          </Button>
        </Modal>
      </Portal>
      {/* Ping modal */}
      <Portal>
        <Modal
          visible={pingModalVisible}
          onDismiss={() => togglePingModal(false)}
          contentContainerStyle={screenStyles.pingModalContainerStyle}
        >
          <View style={screenStyles.pingModalContent}>
            <Text style={{ fontSize: 18 }}>Select an instructor to ping</Text>
            <FlatList
              data={admins}
              renderItem={({ item, index }) => (
                <TouchableRipple onPress={() => pingInstructor(item.id)}>
                  <Card style={{ padding: 16 }} key={item.id}>
                    <Text>{item.name}</Text>
                  </Card>
                </TouchableRipple>
              )}
              ItemSeparatorComponent={() => <Gap gapValue="1" type="col" />}
              keyExtractor={(item) => item.id.toString()}
              ListFooterComponent={() => <Gap gapValue="2" type="col" />}
              ListHeaderComponent={() => <Gap gapValue="1" type="col" />}
            />
          </View>
          <Button
            onPress={() => togglePingModal()}
            style={screenStyles.pingModalButton}
          >
            Cancel
          </Button>
        </Modal>
      </Portal>
      {/* Create Group */}
      <Portal>
        <Modal
          visible={createGroupModalVisible}
          onDismiss={() => setCreateGroupModalVisible(false)}
          contentContainerStyle={screenStyles.pingModalContainerStyle}
        >
          <View style={screenStyles.pingModalContent}>
            <Text style={{ fontSize: 18 }}>Create Group</Text>
            <TextInput
              placeholder="Group Name"
              multiline={false}
              mode="outlined"
              style={{ height: 48, marginVertical: 12 }}
              value={newGroupName}
              onChangeText={(text) => setNewGroupName(text)}
            />
          </View>
          <View
            style={{ flexDirection: "row", justifyContent: "space-between" }}
          >
            <Button
              style={{ padding: 8 }}
              onPress={() => setCreateGroupModalVisible(false)}
            >
              Cancel
            </Button>
            <Button style={{ padding: 8 }} onPress={() => onCreateGroup()}>
              Submit
            </Button>
          </View>
        </Modal>
      </Portal>
      <FlatList
        refreshControl={
          <RefreshControl
            colors={["#9Bd35A", "#689F38"]}
            refreshing={refreshing}
            onRefresh={onRefresh}
          />
        }
        data={groupsList}
        renderItem={({ item, index }) => (
          <ChatCard
            key={index}
            item={item}
            index={index}
            onCardPress={() => onCardPress(item)}
            onCardLongPress={() => onCardLongPress(item)}
          />
        )}
        ItemSeparatorComponent={() => <Gap gapValue="1" type="col" />}
        keyExtractor={(_, index) => index.toString()}
        ListFooterComponent={() => <Gap gapValue="3" type="col" />}
        ListHeaderComponent={() => <Gap gapValue="1" type="col" />}
      />
      <Portal>
        <FAB.Group
          visible={showFab}
          open={open}
          icon={open ? "window-close" : "plus"}
          actions={fabActions}
          onStateChange={onStateChange}
        />
      </Portal>
    </View>
  );
};

const screenStyles = StyleSheet.create({
  pingModalContainerStyle: {
    margin: 20,
    backgroundColor: "white",
    borderRadius: 6,
  },
  pingModalContent: {
    paddingTop: 20,
    paddingHorizontal: 20,
  },
  pingModalButton: {
    width: "100%",
    paddingVertical: 10,
  },
  flexBetween: {
    flexDirection: "row",
    justifyContent: "space-between",
    width: "100%",
  },
});

export default Chat;
