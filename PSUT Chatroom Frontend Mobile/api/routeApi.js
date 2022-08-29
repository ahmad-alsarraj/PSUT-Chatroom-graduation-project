import axiosConfiguration from "./axiosConfiguration";

const axios = axiosConfiguration("");

export const PSUT_ChatroomApi = {
  createGroup: (name) => axios.post("Group/Create", { name }),
  updateGroup: (id, name) => axios.patch("Group/Update", { name, id }),
  deleteGroup: (ids) => axios.delete("Group/Delete", { data: ids }),
  removeMember: (dto) => axios.delete("GroupMember/DeleteOne", { data: dto }),
  addMember: (dto) => axios.post("GroupMember/Create", dto),
  getNonExistingMembers: (groupId) =>
    axios.get(`GroupMember/GetStudentsNotInGroup/${groupId}`),
  getGroups: () =>
    axios.post("Group/GetGroups", {
      offset: 0,
      count: 100,
    }),
  GetMessagesInConversation: (conversationId) =>
    axios.post("Message/GetMessagesInConversation", {
      conversationId,
      offset: 0,
      count: 999,
    }),
  sendMessage: (msg) => axios.post("Message/Create", msg),
  sendPing: (msg) => axios.post("Ping/Create", msg),
  createBackup: () => axios.post("Admin/CreateBackup"),
  patchDatabase: () => axios.post("Admin/PatchDb"),
  clearDatabase: () => axios.post("Admin/ClearDb"),
};

export const userRoute = {
  LoginAuth: (data) =>
    axios.post(`User/Login`, {
      email: data.email,
      googleToken: data.password,
    }),
  GetUser: () => axios.post("User/GetLoggedIn"),
  Logout: () => axios.post("User/Logout"),
};
