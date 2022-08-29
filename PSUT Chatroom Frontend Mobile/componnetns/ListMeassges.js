import React from "react";
import { View, Text } from "react-native";

const ListMeassges = ({
  item: { sender, isMe, message, sendingTime },
  index,
}) => {
  return (
    <View>
      <View
        style={{
          display: "flex",
          flexDirection: isMe ? "row-reverse" : "row",
          paddingHorizontal: 10,
          paddingVertical: 5,
        }}
      >
        <View
          style={{
            backgroundColor: "#128C7E",
            padding: 10,
            borderRadius: 10,
          }}
        >
          <Text
            style={{
              flex: 1,
              textAlign: isMe === 0 ? "right" : "left",
              fontFamily: "AppNormal",
              color: "white",
            }}
          >
            {message}
          </Text>
          <View
            style={{
              flex: 1,
              flexDirection: "row",
              minWidth: 65,
              justifyContent: "space-between",
            }}
          >
            <Text
              style={{
                flex: 1,
                textAlign: "left",
                fontFamily: "AppNormal",
                fontSize: 12,
                color: "yellow",
              }}
              numberOfLines={1}
            >
              {isMe ? "Me" : sender}
            </Text>
            <Text
              style={{
                flex: 1,
                textAlign: "right",
                fontFamily: "AppNormal",
                fontSize: 12,
                color: "yellow",
              }}
            >
              {sendingTime?.split("T")[1].split(".")[0]}
            </Text>
          </View>
        </View>
      </View>
    </View>
  );
};

export default ListMeassges;
