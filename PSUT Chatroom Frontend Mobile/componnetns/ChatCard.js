import React from "react";
import { View, Text } from "react-native";
import { styles } from "../style/style";
import { Avatar, Card, TouchableRipple } from "react-native-paper";
import Gap from "./Gap";

const ChatCard = ({
  item: { avatar_url, name, id, adminName },
  index,
  onCardPress,
  onCardLongPress,
}) => {
  return (
    <TouchableRipple
      key={index}
      onPress={() => onCardPress()}
      onLongPress={() => onCardLongPress()}
      style={{ paddingHorizontal: 10 }}
    >
      <Card style={{ padding: 20, borderColor: "red" }}>
        <View style={styles.rowView}>
          <View
            style={{
              display: "flex",
              justifyContent: "center",
              alignItems: "center",
            }}
          >
            <Avatar.Image source={{ uri: avatar_url }} />
          </View>
          <Gap gapValue="6" type="row" />
          <View
            style={{
              display: "flex",
              justifyContent: "center",
            }}
          >
            <Text style={[styles.reqFont, { fontSize: 15, textAlign: "left" }]}>
              {name.length > 34 ? name.substring(0, 32) + "..." : name}
            </Text>
            <Gap gapValue="0.5" type="col" />
            <Text
              style={[
                styles.reqFont,
                { fontSize: 15, width: "100%", color: "grey" },
              ]}
            >
              Inst. {adminName}
            </Text>
          </View>
        </View>
      </Card>
    </TouchableRipple>
  );
};

export default ChatCard;
