import React, { useState, useRef, useEffect } from "react";
import { FlatList, View, Image } from "react-native";
import { Avatar, Button, TextInput, TouchableRipple } from "react-native-paper";
import Gap from "../../componnetns/Gap";
import ListMeassges from "../../componnetns/ListMeassges";
import { styles } from "../../style/style";
import EmojiSelector, { Categories } from "react-native-emoji-selector";
import * as ImagePicker from "expo-image-picker";
import LottieView from "lottie-react-native";
import { Audio } from "expo-av";
import { useNavigation } from "@react-navigation/native";
import { PSUT_ChatroomApi } from "../../api/routeApi";
import { Cache } from "../../helpers/cache";
import bg from "../../assets/chat_bg.png";

let Player = new Audio.Sound();

const Meassging = ({ route }) => {
  const nav = useNavigation();
  if (!route?.params?.conversationId) nav.goBack();

  const [messagesState, setMessagesState] = useState([]);
  const [showEmoji, setShowEmoji] = useState(false);
  const [emojiSelected, setEmojiSelected] = useState("");
  const [textMeassging, setTextMeassging] = useState("");
  const [image, setImage] = useState(null);
  const [onRecored, setOnRecored] = useState(false);
  const [visible, setVisible] = useState(false);
  const [recoredFile, setRecoredFile] = useState(null);
  const [recoredingUrl, setRecoredingUrl] = useState(null);
  const flatRef = useRef(null);

  const onChange = (text) => {
    setTextMeassging(text);
  };

  // useEffect(() => {
  //   console.log(route.params);
  //   if (route.params.adminId === Cache.userData.id){

  //   }
  // }, []);

  useEffect(() => {
    setTextMeassging((pre) => pre + emojiSelected);
  }, [emojiSelected]);

  const showModal = () => setVisible(true);
  const hideModal = () => setVisible(false);

  const timerRef = useRef(null);
  useEffect(() => {
    getMessages();
    timerRef.current = setInterval(() => getMessages(), 4000);

    return () => clearInterval(timerRef.current);
  }, []);

  const getMessages = async () => {
    try {
      const { conversationId } = route.params;
      const messages = await PSUT_ChatroomApi.GetMessagesInConversation(
        conversationId
      );

      const userId = Cache.userData.id;
      const data = messages.data.map((msg) => {
        return {
          id: msg.id,
          sender: msg.sender.name,
          message: msg.content,
          sendingTime: msg.sendingTime,
          isMe: userId === msg.sender.id,
        };
      });

      if (messagesState.length !== data.length) setMessagesState(data);
    } catch (error) {
      console.log(error);
    }
  };

  useEffect(() => {
    scrollToIndex();
  }, [messagesState]);

  const getPickerPermissions = async () => {
    const { status } = await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (status !== "granted") {
      Alert(
        "تنبيه",
        "للتتمكن من استخدام اختيار الصور من المعرض يجب ان تعطي الصلاحيه للوصول",
        [
          {
            text: "اعادة طلب الصلاحية",
            onPress: () => getPickerPermissions(),
            style: "default",
          },
          {
            text: "حسنا",
            style: "cancel",
          },
        ],
        { cancelable: false }
      );
    } else pickImage();
  };

  const pickImage = async () => {
    let result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ImagePicker.MediaTypeOptions.All,
      allowsEditing: false,
      aspect: [4, 3],
      quality: 1,
    });

    console.log(result);

    if (!result.cancelled) {
      setImage(result.uri);
    }
  };

  const startVoiceRecording = async () => {
    try {
      setOnRecored(true);
      console.log("Requesting permissions..");
      await Audio.requestPermissionsAsync();
      await Audio.setAudioModeAsync({
        allowsRecordingIOS: true,
        playsInSilentModeIOS: true,
        shouldDuckAndroid: true,
      });
      console.log("Starting recording ....");
      const { recording } = await Audio.Recording.createAsync(
        Audio.RECORDING_OPTIONS_PRESET_HIGH_QUALITY
      );
      setRecoredFile(recording);
      console.log("Recording started");
    } catch (err) {
      console.error("Failed to start recording", err);
    }
  };

  const stopRecording = async () => {
    setOnRecored(false);
    await recoredFile.stopAndUnloadAsync();
    const uri = recoredFile.getURI();
    setRecoredingUrl(uri);
    showModal();
    console.log("Recording stopped and stored at", uri);
  };

  const playSound = async () => {
    try {
      const result = await Player.loadAsync({ uri: recoredingUrl }, {}, true);
      const response = await Player.getStatusAsync();
      Player.setOnPlaybackStatusUpdate((status) => {
        if (status.didJustFinish) {
          stopSound(recoredingUrl);
        }
      });
      if (response.isLoaded) {
        if (!response.isPlaying) Player.playAsync();
      }
    } catch (error) {
      console.log(error);
    }
  };

  const stopSound = async () => {
    try {
      const stusts = await Player.getStatusAsync();
      console.log("stusts", stusts);
      if (stusts.isLoaded) {
        await Player.pauseAsync();
        recoredingUrl ? Player.unloadAsync() : undefined;
      }
    } catch (error) {
      console.log(error);
    }
  };

  const onPressSend = () => {
    // if (!textMeassging) onRecored ? stopRecording() : startVoiceRecording();
    // else {
    const time = new Date();
    const data = {
      id: Math.random(),
      sender: "Me",
      message: textMeassging,
      sendingTime: `T${time.getHours()}:${time.getMinutes()}:${time.getSeconds()}.`,
      isMe: true,
    };

    const dto = {
      content: textMeassging,
      conversationId: route.params.conversationId,
    };

    setMessagesState([...messagesState, data]);
    setTextMeassging("");
    scrollToIndex();

    PSUT_ChatroomApi.sendMessage(dto);
    // TODO: Send to the backend
    // }
  };

  const scrollToIndex = () => {
    if (messagesState.length - 1 < 0 || !flatRef.current) return;
    try {
      flatRef.current.scrollToIndex({
        animated: true,
        index: messagesState.length - 1,
      });
    } catch (e) {}
  };

  return (
    <View style={[styles.container, { backgroundColor: "#FFF" }]}>
      <Image
        style={{
          width: "100%",
          height: "100%",
          position: "absolute",
          transform: [{ scale: 2 }],
        }}
        resizeMethod="auto"
        resizeMode="repeat"
        source={bg}
      />
      {/* TODO ADD CUSTOM HEADER */}
      {!!image ? (
        <View
          style={{
            flex: 1,
            // backgroundColor: "black",
            justifyContent: "space-around",
          }}
        >
          <View
            style={{
              paddingHorizontal: 20,
              display: "flex",
              justifyContent: "center",
              alignItems: "center",
            }}
          >
            <Image
              style={{ width: 300, height: 300 }}
              source={{ uri: `${image}` }}
            />
          </View>

          <View style={{ paddingHorizontal: 20 }}>
            <Button
              mode="contained"
              theme={{ roundness: 10 }}
              labelStyle={{ fontFamily: "AppNormal" }}
              contentStyle={styles.loginButton}
              onPress={() => setImage(null)}
            >
              ارسال
            </Button>
          </View>
        </View>
      ) : (
        <React.Fragment>
          <FlatList
            ref={flatRef}
            data={messagesState}
            style={{ height: "10%" }}
            renderItem={({ item, index }) => (
              <ListMeassges key={index} item={item} index={index} />
            )}
            keyExtractor={(item, index) =>
              item?.id?.toString() ?? index.toString()
            }
          />
          <View
            style={{
              padding: 10,
              justifyContent: "flex-end",
              alignItems: "baseline",
            }}
          >
            <View style={styles.rowView}>
              {onRecored ? (
                <View style={{ width: "80%" }}>
                  <LottieView
                    loop={true}
                    autoPlay={true}
                    speed={0.65}
                    style={{
                      width: 100,
                      height: 100,
                      backgroundColor: "white",
                    }}
                    source={require("../../assets/jsonfile/loadingRecord.json")}
                  />
                </View>
              ) : (
                <TextInput
                  theme={{ roundness: 50 }}
                  multiline={false}
                  mode="outlined"
                  enablesReturnKeyAutomatically={true}
                  keyboardType="twitter"
                  onChangeText={(text) => onChange(text)}
                  value={textMeassging}
                  left={
                    <TextInput.Icon
                      name={"emoticon"}
                      onPress={() => setShowEmoji((prev) => !prev)}
                      color={"gray"}
                      style={{
                        top: 5,
                      }}
                    />
                  }
                  selectionColor={"#128C7E"}
                  style={[styles.textInputstyle, { width: "85%" }]}
                />
              )}
              <Gap gapValue="3" type="row" />
              <TouchableRipple onPress={onPressSend}>
                <Avatar.Icon
                  style={{ top: 7 }}
                  size={45}
                  icon={"arrow-right-thick"}
                />
              </TouchableRipple>
            </View>
          </View>
        </React.Fragment>
      )}
      {showEmoji && (
        <EmojiSelector
          showSearchBar={false}
          showSectionTitles={false}
          category={Categories.emotion}
          onEmojiSelected={(emoji) => setEmojiSelected(emoji)}
        />
      )}
    </View>
  );
};

export default Meassging;
