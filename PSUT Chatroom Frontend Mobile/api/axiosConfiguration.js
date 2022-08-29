import Axios from "axios";
import { port, REACT_APP_GATEWAY_URL } from "./apiKey";
import * as SecureStore from "expo-secure-store";

const axiosConfiguration = (apiName) => {
  const axiosConfig = {
    baseURL: `http://${REACT_APP_GATEWAY_URL}:${port}/${apiName}`,
  };

  const axios = Axios.create(axiosConfig);

  const requestHandler = async (request) => {
    const userToken = await SecureStore.getItemAsync("token");
    // alert(userToken);
    if (!!userToken) request.headers.Authorization = `${userToken}`;

    console.log("TOKEN-----", userToken);
    return request;
  };

  const onResponseError = async (error) => {
    console.log(`${JSON.stringify(error)}`);
    return Promise.reject(error);
  };

  axios.interceptors.request.use(
    (request) => requestHandler(request),
    (error) => Promise.reject(error)
  );

  axios.interceptors.response.use(
    (response) => response,
    (error) => onResponseError(error)
  );

  return axios;
};

export default axiosConfiguration;
