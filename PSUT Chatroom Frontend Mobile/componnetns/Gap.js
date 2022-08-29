import React from "react";
import { View } from "react-native";
import {
  heightPercentageToDP as hp,
  widthPercentageToDP as wp,
} from "react-native-responsive-screen";

/**
 *
 * A Gap @component for created responsive gap between View React native
 * @name Gap
 * @prop {gapValue} React string value
 * @prop {type} type need to selected gap by def col
 *
 */

const Gap = ({ gapValue = "2", type = "col" }) => {
  return (
    <React.Fragment>
      {type === "col" ? (
        <View style={{ height: hp(`${gapValue}%`) }} />
      ) : (
        <View style={{ width: wp(`${gapValue}%`) }} />
      )}
    </React.Fragment>
  );
};

export default Gap;
