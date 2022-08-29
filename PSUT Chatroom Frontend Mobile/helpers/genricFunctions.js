import qs from "qs";

export const fillChatList = (array = [], valueKey = "", textKey = "") => {
  return array.map((item, index) => {
    return {
      key: index,
      value: item[valueKey],
      label: item[textKey],
    };
  });
};

export const createQueryString = (obj) =>
  qs.stringify({ ...obj }, { addQueryPrefix: true });
