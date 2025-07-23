import { combineReducers } from "redux";
// Reducer cho searchParams
const initialSearchState = {
  searchText: "",
  categoryId: [
    1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
  ],
  province: "",
  district: "",
};

const searchFacilityReducer = (state = initialSearchState, action) => {
  switch (action.type) {
    case "SET_SEARCH_FACILITY":
      return {
        ...state,
        ...action.payload, // cập nhật searchParams với payload
      };
    default:
      return state;
  }
};

const rootReducer = combineReducers({
  searchFacility: searchFacilityReducer,
  // Add your reducers here
  // Example: user: userReducer, products: productReducer
  // user: userReducer,
  // products: productReducer,
  // cart: cartReducer,
  // orders: orderReducer,
  // etc.
});

export default rootReducer;
