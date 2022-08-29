import React from "react";
import {View, Text} from "react-native";
import {Cache} from "../../helpers/cache";
import {Card} from "react-native-paper";
import {Avatar} from "react-native-paper";

const UserProfile = () => {
  return <View style={{padding: 16, alignItems: 'center'}}>
    <Card style={{paddingHorizontal: 16, paddingVertical: 36, alignItems: 'center', width: '100%'}}>
      <Avatar.Image size={160} source={{uri: `https://source.unsplash.com/random?sig=${Cache?.userData?.id}`}}/>
      <Text style={{paddingTop: 22, textAlign: 'center'}}>{Cache?.userData?.name} / {Cache?.userData?.role}</Text>
      <Text style={{paddingTop: 12, textAlign: 'center'}}>{Cache?.userData?.email}</Text>
    </Card>
  </View>;
};

export default UserProfile;
