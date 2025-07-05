import axios from "../utils/axiosCustomize";
axios.defaults.timeout = 5000;
const getAllCourtCategories = (search, pageNumber, pageSize) => {
  return axios.get(
    `CourtCategory/get-all-court-categories?search=${search}&pageNumber=${pageNumber}&pageSize=${pageSize}`
  );
};
export { getAllCourtCategories };
