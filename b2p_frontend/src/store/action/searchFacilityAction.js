export const setSearchFacility = (params) => {
  return {
    type: "SET_SEARCH_FACILITY", // Sử dụng tên action mới
    payload: params, // Truyền thông tin tìm kiếm vào payload
  };
};
