import React, {
  useState,
  useEffect,
  useCallback,
  useMemo,
  useRef,
} from "react";
import "./BookingManagement.scss";
import {
  Select,
  DatePicker,
  Modal,
  Button,
  Tag,
  Avatar,
  Spin,
  message,
  Form,
  Badge,
  notification as antdNotification,
  Statistic,
  Row,
  Col,
  Card,
  Typography,
  Menu,
  Dropdown,
} from "antd";
import {
  CalendarOutlined,
  ClockCircleOutlined,
  UserOutlined,
  PhoneOutlined,
  MailOutlined,
  DollarOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  HomeOutlined,
  PlusOutlined,
  StopOutlined,
  AppstoreOutlined,
  InfoCircleOutlined,
  FlagOutlined,
  LockOutlined,
  MoreOutlined,
} from "@ant-design/icons";
import dayjs from "dayjs";
import {
  getFacilitiesByCourtOwnerId,
  getAllCourts,
  getTimeSlotsByFacilityId,
  getBookingsByFacilityId,
  getAccountById,
  completeBooking,
  createBookingForCO,
  getFacilityDetailsById,
  createSimpleBooking,
  markSmartSlot,
} from "../../services/apiService";
import signalRService from "../../services/signalRService";
import { useAuth } from "../../contexts/AuthContext";

const { Option } = Select;
const { Text } = Typography;

const BookingManagement = () => {
  useEffect(() => {
    document.title = "Quản lý đơn - B2P";
  }, []);
  // ✅ STEP 1: All hooks first - Auth hooks
  const { user, userId, isLoading: authLoading, isLoggedIn } = useAuth();

  // ✅ STEP 2: All useState hooks
  const [hasInitialized, setHasInitialized] = useState(false);
  const [facilities, setFacilities] = useState([]);
  const [totalFacilities, setTotalFacilities] = useState(0);
  const [courts, setCourts] = useState([]);
  const [timeSlots, setTimeSlots] = useState([]);
  const [rawTimeSlots, setRawTimeSlots] = useState([]);
  const [selectedFacility, setSelectedFacility] = useState(null);
  const [selectedDate, setSelectedDate] = useState(dayjs());
  const [bookingData, setBookingData] = useState({});
  const [bookings, setBookings] = useState([]);
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [selectedBooking, setSelectedBooking] = useState(null);
  const [modalCategories, setModalCategories] = useState([]);
  const [selectedTimeSlots, setSelectedTimeSlots] = useState([]);
  const [selectedCategoryFilter, setSelectedCategoryFilter] = useState("all");
  const [isRealTimeConnected, setIsRealTimeConnected] = useState(false);
  const [customerCache, setCustomerCache] = useState({});
  const [notificationBookingDetail, setNotificationBookingDetail] =
    useState(null);
  const [isNotificationDetailVisible, setIsNotificationDetailVisible] =
    useState(false);
  const [slotContextMenu, setSlotContextMenu] = useState({
    visible: false,
    x: 0,
    y: 0,
    court: null,
    timeSlot: null,
    slotId: null,
  });
  const [loading, setLoading] = useState({
    facilities: false,
    courts: false,
    timeSlots: false,
    bookings: false,
    customer: false,
    completing: false,
    modalCategories: false,
    creating: false,
    markingCourt: false,
  });
  const [isCreateBookingModalVisible, setIsCreateBookingModalVisible] =
    useState(false);

  // ✅ STEP 3: Form hooks
  const [createBookingForm] = Form.useForm();

  // ✅ STEP 4: Auth computed values
  const getCourtOwnerId = useCallback(() => {
    console.log(
      "🔍 Getting court owner ID - isLoggedIn:",
      isLoggedIn,
      "userId:",
      userId
    );

    if (isLoggedIn && userId) {
      return userId;
    }

    console.warn("⚠️ Court owner not logged in");
    return null;
  }, [isLoggedIn, userId]);

  // ✅ STEP 5: Utility callbacks
  const updateLoading = useCallback((key, value) => {
    setLoading((prev) => ({ ...prev, [key]: value }));
  }, []);

  const formatTime = useCallback((time) => {
    return time ? time.substring(0, 5) : "";
  }, []);

  const getBookingStatusFromString = useCallback((status) => {
    const statusLower = status?.toLowerCase();
    switch (statusLower) {
      case "paid":
        return "paid";
      case "unpaid":
        return "unpaid";
      case "completed":
        return "completed";
      case "cancelled":
      case "canceled":
        return "cancelled";
      case "active":
        return "confirmed";
      default:
        return "confirmed";
    }
  }, []);

  const getStatusDisplayText = useCallback((status, originalStatus) => {
    switch (status) {
      case "paid":
        return "Đã thanh toán cọc";
      case "completed":
        return "Đã hoàn thành";
      case "unpaid":
        return "Chưa thanh toán";
      case "cancelled":
        return "Đã hủy";
      case "confirmed":
        return originalStatus === "Active" ? "Đã xác nhận" : "Đã xác nhận";
      default:
        return originalStatus || "Đã xác nhận";
    }
  }, []);

  const getSlotDisplayText = useCallback((status) => {
    switch (status) {
      case "available":
        return "Còn trống";
      case "paid":
        return "Đã Cọc";
      case "unpaid":
        return "Chưa thanh toán";
      case "completed":
        return "Đã hoàn thành";
      case "cancelled":
        return "Đã hủy";
      default:
        return "Chưa thanh toán";
    }
  }, []);

  const getTimeSlotId = useCallback(
    (timeSlotString) => {
      const foundSlot = rawTimeSlots.find((slot) => {
        const startTime = slot.startTime || slot.start || slot.timeStart;
        const endTime = slot.endTime || slot.end || slot.timeEnd;
        if (!startTime || !endTime) return false;
        const formattedSlot = `${formatTime(startTime)} - ${formatTime(
          endTime
        )}`;
        return formattedSlot === timeSlotString;
      });
      return foundSlot?.timeSlotId || foundSlot?.id || 1;
    },
    [rawTimeSlots, formatTime]
  );

  const getBookingKey = useCallback((courtId, date, timeSlot) => {
    return `${courtId}_${date.format("YYYY-MM-DD")}_${timeSlot}`;
  }, []);

  const getSelectedCategoryId = useCallback(() => {
    if (selectedCategoryFilter === "all" && modalCategories.length > 0) {
      return modalCategories[0].categoryId;
    }
    return selectedCategoryFilter;
  }, [selectedCategoryFilter, modalCategories]);

  const processBookingData = useCallback(
    (bookingsData) => {
      const processedBookings = {};

      bookingsData.forEach((booking) => {
        try {
          const bookingDate = dayjs(booking.checkInDate);
          if (!bookingDate.isValid()) return;

          const isDateMatch =
            bookingDate.format("YYYY-MM-DD") ===
            selectedDate.format("YYYY-MM-DD");
          if (!isDateMatch || !booking.slots?.length) return;

          booking.slots.forEach((slot) => {
            try {
              const startTime =
                slot.startTime || slot.timeStart || slot.fromTime;
              const endTime = slot.endTime || slot.timeEnd || slot.toTime;
              const courtId = slot.courtId || slot.court_id || slot.id;

              if (!startTime || !endTime || !courtId) return;

              const timeSlot = `${formatTime(startTime)} - ${formatTime(
                endTime
              )}`;
              const bookingKey = getBookingKey(courtId, bookingDate, timeSlot);

              const status = getBookingStatusFromString(booking.status);
              const processedBooking = createProcessedBooking(
                booking,
                slot,
                courtId,
                timeSlot,
                bookingDate,
                status
              );

              if (processedBooking) {
                processedBookings[bookingKey] = processedBooking;
              }
            } catch (error) {
              console.error("Error processing booking slot:", error);
            }
          });
        } catch (error) {
          console.error("Error processing booking:", error);
        }
      });

      setBookingData(processedBookings);
    },
    [selectedDate, formatTime, getBookingKey, getBookingStatusFromString]
  );
  useEffect(() => {
    const handleBookingPaidUpdate = (event) => {
      const notification = event.detail;
      console.log(
        "🎯 [BookingManagement] Payment update received:",
        notification
      );

      // ✅ Tìm tất cả các slot có bookingId trùng
      const matchingKeys = Object.keys(bookingData).filter((bookingKey) => {
        const booking = bookingData[bookingKey];
        const possibleIds = [
          booking?.bookingId,
          booking?.id,
          booking?.Id,
          booking?.booking?.id,
          booking?.booking?.bookingId,
        ].filter((id) => id !== undefined && id !== null);
        return possibleIds.some(
          (id) => id.toString() === notification.bookingId.toString()
        );
      });

      if (matchingKeys.length > 0) {
        setBookingData((prev) => {
          const updated = { ...prev };
          matchingKeys.forEach((key) => {
            updated[key] = {
              ...prev[key],
              status: "paid",
              statusId: 7,
              paymentStatus: "deposit",
              statusDescription: "Đã Cọc",
            };
          });
          return updated;
        });
        matchingKeys.forEach((key) => {
          console.log(`✅ Slot ${key} updated to PAID status`);
        });
      } else {
        console.log("❌ NO MATCHING SLOT FOUND!");
        Object.keys(bookingData).forEach((key) => {
          const booking = bookingData[key];
          console.log(
            `Slot ${key}: ${booking?.bookingId || booking?.id} vs ${notification.bookingId}`
          );
        });
      }
    };

    window.addEventListener("bookingPaidUpdate", handleBookingPaidUpdate);
    return () => {
      window.removeEventListener("bookingPaidUpdate", handleBookingPaidUpdate);
    };
  }, [bookingData]);
  // ✅ ADD THIS CANCELLATION HANDLER AFTER THE PAYMENT HANDLER
  useEffect(() => {
    const handleBookingCancelledUpdate = (event) => {
      const notification = event.detail;
      console.log('🎯 [BookingManagement] Cancellation update received:', notification);
      console.log('🔍 [DEBUG] BookingId from notification:', notification.bookingId);

      // ✅ IMMEDIATELY BLOCK THIS BOOKING GLOBALLY
      if (!window.globalBlockedBookings) {
        window.globalBlockedBookings = new Set();
      }
      window.globalBlockedBookings.add(notification.bookingId.toString());
      console.log(`🚫 BLOCKED booking ${notification.bookingId} from future payment updates`);

      console.log('🔍 [DEBUG] Current bookingData structure:');
      Object.keys(bookingData).forEach(slotKey => {
        const booking = bookingData[slotKey];
        console.log(`🔍 Slot [${slotKey}]:`, booking);
      });

      let foundSlot = null;
      let foundKey = null;

      Object.keys(bookingData).forEach(bookingKey => {
        const booking = bookingData[bookingKey];

        const possibleIds = [
          booking?.bookingId,
          booking?.id,
          booking?.Id
        ].filter(id => id !== undefined && id !== null);

        possibleIds.forEach(id => {
          if (id.toString() === notification.bookingId.toString()) {
            foundSlot = booking;
            foundKey = bookingKey;
            console.log(`✅ FOUND MATCH for cancellation! Slot: ${bookingKey}, ID: ${id}`);
          }
        });
      });

      if (foundKey) {
        console.log(`🔄 Updating slot to CANCELLED: ${foundKey}`);
        console.log('🔍 Before cancellation update:', foundSlot);

        setBookingData(prev => {
          const updated = {
            ...prev,
            [foundKey]: {
              ...prev[foundKey],
              status: 'cancelled',
              statusId: 9, // ✅ Use correct statusId for cancelled
              statusDescription: 'Đã Hủy',
              paymentStatus: 'cancelled',
              originalStatus: 'Cancelled'
            }
          };

          console.log(`✅ Slot ${foundKey} updated to CANCELLED status:`, updated[foundKey]);
          return updated;
        });
      } else {
        console.log('❌ No matching slot found for cancellation ID:', notification.bookingId);
      }
    };

    window.addEventListener('bookingCancelledUpdate', handleBookingCancelledUpdate);
    return () => {
      window.removeEventListener('bookingCancelledUpdate', handleBookingCancelledUpdate);
    };
  }, [bookingData]);

  const createProcessedBooking = useCallback(
    (booking, slot, courtId, timeSlot, bookingDate, status) => {
      try {
        let paymentStatus = "pending";
        let mappedStatusId = 1;

        switch (status) {
          case "paid":
            paymentStatus = "deposit";
            mappedStatusId = 7;
            break;
          case "completed":
            paymentStatus = "paid";
            mappedStatusId = 10;
            break;
          case "cancelled":
            paymentStatus = "cancelled";
            mappedStatusId = 9;
            break;
          default:
            paymentStatus = "pending";
            mappedStatusId = 1;
        }

        const rawBookingTime =
          booking.createDate || booking.createdAt || booking.checkInDate;
        let bookingTime = "Không có thông tin";

        if (
          rawBookingTime &&
          rawBookingTime !== "N/A" &&
          rawBookingTime !== "0001-01-01T00:00:00"
        ) {
          const parsedTime = dayjs(rawBookingTime);
          if (parsedTime.isValid()) {
            bookingTime = parsedTime.format("DD/MM/YYYY HH:mm:ss");
          }
        }

        return {
          id: booking.bookingId || booking.id,
          userId: booking.userId,
          courtId,
          courtName: slot.courtName || slot.court_name || `Court ${courtId}`,
          timeSlot,
          date: bookingDate.format("DD/MM/YYYY"),
          price: booking.totalPrice || booking.price || 0,
          status,
          paymentStatus,
          bookingTime,
          checkInDate: booking.checkInDate,
          statusId: mappedStatusId,
          originalStatus: booking.status,
          rawCreateDate: rawBookingTime,
          customerName: "Đang tải...",
          customerPhone: "Đang tải...",
          customerEmail: "Đang tải...",
        };
      } catch (error) {
        console.error("Error creating processed booking:", error);
        return null;
      }
    },
    []
  );

  // ✅ FIXED: Correct API function with proper parameters
  const loadFacilities = useCallback(async () => {
    try {
      updateLoading("facilities", true);
      const courtOwnerId = getCourtOwnerId();

      if (!courtOwnerId) {
        console.error("❌ No court owner ID available");
        message.error("Người dùng chưa đăng nhập");
        setFacilities([]);
        return;
      }

      console.log("🚀 Loading facilities for courtOwnerId:", courtOwnerId);

      // ✅ FIXED: Correct parameters order
      // getFacilitiesByCourtOwnerId(courtOwnerId, facilityName, statusId, currentPage, itemsPerPage)
      const response = await getFacilitiesByCourtOwnerId(
        courtOwnerId, // courtOwnerId
        "", // facilityName - empty string, not null
        null, // statusId
        1, // currentPage
        100 // itemsPerPage
      );

      console.log("📡 API Response:", response);

      const facilitiesData = response.data?.items || response.data || [];
      setFacilities(facilitiesData);

      if (facilitiesData.length > 0) {
        setSelectedFacility(facilitiesData[0].facilityId);
        console.log(
          "✅ Auto-selected facility:",
          facilitiesData[0].facilityId,
          facilitiesData[0].facilityName
        );
      }

      console.log(
        "✅ Facilities loaded successfully:",
        facilitiesData.length,
        "facilities for user",
        courtOwnerId
      );
    } catch (error) {
      console.error("💥 Error loading facilities:", error);
      console.error("💥 Error details:", error.response?.data);
      message.error("Không thể tải danh sách cơ sở");
      setFacilities([]);
    } finally {
      updateLoading("facilities", false);
    }
  }, [updateLoading, getCourtOwnerId]);

  const loadBookings = useCallback(
    async (facilityId) => {
      try {
        updateLoading("bookings", true);
        const response = await getBookingsByFacilityId(facilityId, 1, 1000);

        const bookingsData = response.data?.items || [];
        setBookings(bookingsData);
        processBookingData(bookingsData);
      } catch (error) {
        console.error("Error loading bookings:", error);
        message.error("Không thể tải dữ liệu đặt sân");
        setBookings([]);
        setBookingData({});
      } finally {
        updateLoading("bookings", false);
      }
    },
    [updateLoading, processBookingData]
  );

  const loadCourts = useCallback(
    async (facilityId) => {
      try {
        updateLoading("courts", true);
        const response = await getAllCourts({
          pageNumber: 1,
          pageSize: 100,
          facilityId,
          search: "",
          status: null,
          categoryId: null,
        });

        const courtsData =
          response.data?.data || response.data?.items || response.data || [];
        setCourts(courtsData);
      } catch (error) {
        console.error("Error loading courts:", error);
        message.error("Không thể tải danh sách sân");
        setCourts([]);
      } finally {
        updateLoading("courts", false);
      }
    },
    [updateLoading]
  );

  const loadTimeSlots = useCallback(
    async (facilityId) => {
      try {
        updateLoading("timeSlots", true);
        const response = await getTimeSlotsByFacilityId(facilityId);

        const timeSlotsData =
          response.data?.data ||
          response.data?.items ||
          response.data?.result ||
          response.data ||
          [];
        setRawTimeSlots(timeSlotsData);

        const formattedTimeSlots = timeSlotsData.map((slot) => {
          const startTime = slot.startTime || slot.start || slot.timeStart;
          const endTime = slot.endTime || slot.end || slot.timeEnd;
          return `${formatTime(startTime)} - ${formatTime(endTime)}`;
        });
        setTimeSlots(formattedTimeSlots);
      } catch (error) {
        console.error("Error loading time slots:", error);
        message.error("Không thể tải khung giờ");
        setTimeSlots([]);
        setRawTimeSlots([]);
      } finally {
        updateLoading("timeSlots", false);
      }
    },
    [updateLoading, formatTime]
  );

  const loadModalCategories = useCallback(
    async (facilityId) => {
      try {
        updateLoading("modalCategories", true);
        const response = await getFacilityDetailsById(facilityId);

        const categories = response?.data?.categories || [];
        setModalCategories(categories);
      } catch (error) {
        console.error("Error loading categories:", error);
        setModalCategories([]);
      } finally {
        updateLoading("modalCategories", false);
      }
    },
    [updateLoading]
  );

  const loadCustomerDetails = useCallback(
    async (userId) => {
      try {
        if (customerCache[userId]) {
          console.log(`✅ Using cached customer data for userId: ${userId}`);
          return customerCache[userId];
        }

        if (!userId || userId === 0) {
          return {
            customerName: "Khách hàng không xác định",
            customerPhone: "Không có thông tin",
            customerEmail: "Không có thông tin",
            customerAvatar: null,
          };
        }

        console.log(`🔍 Loading customer details for userId: ${userId}`);
        const response = await getAccountById(userId);

        const customerData = response?.data?.data || response?.data;

        const result = {
          customerName:
            customerData?.fullName ||
            customerData?.email?.split("@")[0] ||
            `User #${userId}`,
          customerPhone:
            customerData?.phoneNumber || customerData?.phone || "Chưa cập nhật",
          customerEmail: customerData?.email || "Chưa cập nhật",
          customerAvatar: customerData?.avatar || null,
        };

        setCustomerCache((prev) => ({
          ...prev,
          [userId]: result,
        }));

        return result;
      } catch (error) {
        console.error("❌ Error loading customer:", error);
        return {
          customerName: `User ID: ${userId}`,
          customerPhone: "Lỗi tải thông tin",
          customerEmail: "Lỗi tải thông tin",
          customerAvatar: null,
        };
      }
    },
    [customerCache]
  );

  const handleMarkCourt = useCallback(
    async (court, timeSlot) => {
      try {
        updateLoading("markingCourt", true);

        const timeSlotId = getTimeSlotId(timeSlot);
        const categoryId = getSelectedCategoryId();

        if (!categoryId) {
          message.error("Vui lòng chọn loại sân trước khi đánh dấu");
          return;
        }

        const USER_ID = getCourtOwnerId();
        const CUSTOMER_USER_ID = getCourtOwnerId();
        const CUSTOMER_EMAIL = user?.email || "admin@courtowner.com";
        const CUSTOMER_PHONE = user?.phone || user?.phoneNumber || "0000000000";

        const bookingRequestData = {
          userId: CUSTOMER_USER_ID,
          checkInDate: selectedDate.format("YYYY-MM-DDTHH:mm:ss.SSS") + "Z",
          facilityId: selectedFacility,
          categoryId: parseInt(categoryId),
          timeSlotId: timeSlotId,
          courtId: court.courtId || court.id,
        };

        console.log(
          "📡 Creating simple booking with data:",
          bookingRequestData
        );

        const response = await createSimpleBooking(bookingRequestData);

        if (response.status === 400) {
          console.error("❌ 400 Bad Request - Invalid data sent to server");
          let errorMessage = "Dữ liệu không hợp lệ (400 Bad Request)";

          if (response.message) {
            errorMessage = response.message;
          } else if (response.data?.message) {
            errorMessage = response.data.message;
          } else if (response.data?.error) {
            errorMessage = response.data.error;
          }

          message.error(`Không thể đánh dấu sân: ${errorMessage}`);
          return;
        }

        if (
          response.status !== 200 &&
          response.status !== 201 &&
          response.status !== 204
        ) {
          console.error("❌ API returned error status:", response.status);
          message.error(`Lỗi API: ${response.status}`);
          return;
        }

        console.log("✅ API call successful with status:", response.status);
        message.success("Đánh dấu sân thành công!");

        const bookingKey = getBookingKey(
          court.courtId || court.id,
          selectedDate,
          timeSlot
        );
        const newBooking = {
          id: response.data?.bookingId || Date.now(),
          userId: CUSTOMER_USER_ID,
          courtId: court.courtId || court.id,
          courtName: court.courtName || court.name,
          timeSlot: timeSlot,
          date: selectedDate.format("DD/MM/YYYY"),
          price: 0,
          status: "paid",
          paymentStatus: "deposit",
          bookingTime: dayjs().format("DD/MM/YYYY HH:mm:ss"),
          checkInDate: selectedDate.format("YYYY-MM-DD"),
          statusId: 7,
          originalStatus: "Paid",
          customerName: "Admin (Đánh dấu)",
          customerPhone: CUSTOMER_PHONE,
          customerEmail: CUSTOMER_EMAIL,
        };

        setBookingData((prev) => ({
          ...prev,
          [bookingKey]: newBooking,
        }));

        const isConnected = signalRService.connected;
        if (isConnected) {
          const notification = {
            bookingId: newBooking.id,
            facilityId: selectedFacility,
            courtId: court.courtId || court.id,
            timeSlot: timeSlot,
            date: selectedDate.format("DD/MM/YYYY"),
            checkInTime: timeSlot.split(" - ")[0],
            status: "paid",
            action: "created",
            message: `Sân ${court.courtName || court.name} đã được đánh dấu`,
            courtName: court.courtName || court.name,
            customerName: "Admin",
            customerEmail: CUSTOMER_EMAIL,
            customerPhone: CUSTOMER_PHONE,
            totalAmount: 0,
            timestamp: new Date().toISOString(),
          };

          await signalRService.sendBookingUpdate(notification);
        }
      } catch (error) {
        console.error("❌ Error marking court:", error);

        let errorMessage = "Có lỗi xảy ra khi đánh dấu sân";

        if (error.response?.status === 400) {
          if (error.response.data?.message) {
            errorMessage = error.response.data.message;
          } else if (error.message) {
            errorMessage = error.message;
          } else {
            errorMessage = "Dữ liệu không hợp lệ (400 Bad Request)";
          }
        } else if (error.response?.status === 401) {
          errorMessage = "Không có quyền thực hiện thao tác này";
        } else if (error.response?.status === 500) {
          errorMessage = "Lỗi server, vui lòng thử lại sau";
        } else if (error.message) {
          errorMessage = error.message;
        }

        message.error(errorMessage);
      } finally {
        updateLoading("markingCourt", false);
        setSlotContextMenu({
          visible: false,
          x: 0,
          y: 0,
          court: null,
          timeSlot: null,
          slotId: null,
        });
      }
    },
    [
      selectedFacility,
      selectedDate,
      getTimeSlotId,
      getSelectedCategoryId,
      getBookingKey,
      updateLoading,
      getCourtOwnerId,
      user,
    ]
  );

  const handleEmptySlotClick = useCallback(
    (event, court, timeSlot) => {
      event.preventDefault();
      event.stopPropagation();

      const slotId = getTimeSlotId(timeSlot);

      const clickedElement = event.currentTarget;
      const rect = clickedElement.getBoundingClientRect();

      const x = rect.left + window.scrollX;
      const y = rect.bottom + window.scrollY + 5;

      const menuHeight = 80;
      const finalY =
        y + menuHeight > window.innerHeight + window.scrollY
          ? rect.top + window.scrollY - menuHeight - 5
          : y;

      const menuWidth = 160;
      const finalX =
        x + menuWidth > window.innerWidth + window.scrollX
          ? window.innerWidth + window.scrollX - menuWidth - 10
          : x;

      console.log("🎯 Slot-relative menu position:", {
        slotRect: rect,
        finalX,
        finalY,
        court: court.courtName || court.name,
        timeSlot,
      });

      setSlotContextMenu({
        visible: true,
        x: finalX,
        y: finalY,
        court: court,
        timeSlot: timeSlot,
        slotId: slotId,
      });
    },
    [getTimeSlotId]
  );

  const hideContextMenu = useCallback(() => {
    setSlotContextMenu({
      visible: false,
      x: 0,
      y: 0,
      court: null,
      timeSlot: null,
      slotId: null,
    });
  }, []);

  const handleFacilityChange = useCallback((value) => {
    setSelectedFacility(value);
    setSelectedCategoryFilter("all");
  }, []);

  const handleDateChange = useCallback((date) => {
    if (date) {
      setSelectedDate(date);
    }
  }, []);

  const handleTimeSlotToggle = useCallback((timeSlot) => {
    setSelectedTimeSlots((prev) =>
      prev.includes(timeSlot)
        ? prev.filter((slot) => slot !== timeSlot)
        : [...prev, timeSlot]
    );
  }, []);

  const handleSlotClick = useCallback(
    async (court, timeSlot) => {
      const bookingKey = getBookingKey(
        court.courtId || court.id,
        selectedDate,
        timeSlot
      );
      const booking = bookingData[bookingKey];

      if (!booking) return;

      console.log(
        "🔍 DEBUG: Full booking object:",
        JSON.stringify(booking, null, 2)
      );

      updateLoading("customer", true);
      setIsModalVisible(true);
      setSelectedBooking({
        ...booking,
        timeSlot,
        date: selectedDate.format("DD/MM/YYYY"),
      });

      if (booking.userId && booking.userId !== 0 && booking.userId !== "0") {
        try {
          console.log(
            "🔍 DEBUG: Calling getAccountById with userId:",
            booking.userId
          );

          const response = await getAccountById(booking.userId);
          console.log(
            "🔍 DEBUG: getAccountById response:",
            JSON.stringify(response, null, 2)
          );

          const customerDetails = await loadCustomerDetails(booking.userId);
          console.log("🔍 DEBUG: Processed customer details:", customerDetails);

          if (customerDetails) {
            setSelectedBooking((prev) =>
              prev
                ? {
                  ...prev,
                  ...customerDetails,
                }
                : null
            );
            console.log("✅ Customer details loaded successfully");
          } else {
            console.log("⚠️ No customer details returned");
            setSelectedBooking((prev) =>
              prev
                ? {
                  ...prev,
                  customerName: "Không tìm thấy thông tin",
                  customerPhone: "Không tìm thấy thông tin",
                  customerEmail: "Không tìm thấy thông tin",
                }
                : null
            );
          }
        } catch (error) {
          console.error("❌ Error loading customer details:", error);
          setSelectedBooking((prev) =>
            prev
              ? {
                ...prev,
                customerName: "Lỗi tải thông tin",
                customerPhone: "Lỗi tải thông tin",
                customerEmail: "Lỗi tải thông tin",
              }
              : null
          );
        }
      } else {
        console.log("⚠️ Invalid userId:", booking.userId);

        const USER_ID = getCourtOwnerId();
        const CUSTOMER_EMAIL = user?.email || "admin@courtowner.com";
        const CUSTOMER_PHONE = user?.phone || user?.phoneNumber || "0000000000";

        if (
          booking.customerName &&
          booking.customerPhone &&
          booking.customerEmail
        ) {
          console.log("✅ Using existing customer info from booking object");
          setSelectedBooking((prev) =>
            prev
              ? {
                ...prev,
                customerName: booking.customerName,
                customerPhone: booking.customerPhone,
                customerEmail: booking.customerEmail,
                customerAvatar: null,
              }
              : null
          );
        } else {
          console.log(
            "🔄 Fallback to Admin info - no customer data in booking"
          );
          setSelectedBooking((prev) =>
            prev
              ? {
                ...prev,
                customerName: "Admin (Court Owner)",
                customerPhone: CUSTOMER_PHONE || "0000000000",
                customerEmail: CUSTOMER_EMAIL || "admin@courtowner.com",
              }
              : null
          );
        }
      }

      updateLoading("customer", false);
    },
    [
      bookingData,
      selectedDate,
      getBookingKey,
      loadCustomerDetails,
      updateLoading,
      getCourtOwnerId,
      user,
    ]
  );

  const openModal = useCallback(
    async (bookingId) => {
      try {
        const booking = Object.values(bookingData).find(
          (b) => b.id === bookingId
        );
        if (booking) {
          await handleSlotClick({ courtId: booking.courtId }, booking.timeSlot);
        }
      } catch (error) {
        console.error("Error opening modal:", error);
      }
    },
    [bookingData, handleSlotClick]
  );

  const handleCompleteBooking = useCallback(async () => {
    if (!selectedBooking?.id) {
      message.error("Không tìm thấy thông tin đơn đặt sân");
      return;
    }

    try {
      updateLoading("completing", true);
      const response = await completeBooking(selectedBooking.id);

      const isSuccess =
        response.status === 200 ||
        response.status === 201 ||
        response.data?.success === true ||
        !response.data?.error;

      if (isSuccess) {
        message.success("Hoàn thành đơn đặt sân thành công!");

        const newStatus = "completed";
        setSelectedBooking((prev) =>
          prev
            ? {
              ...prev,
              status: newStatus,
              statusId: 10,
              paymentStatus: "paid",
              originalStatus: "Completed",
            }
            : null
        );

        const bookingKey = `${selectedBooking.courtId}_${selectedDate.format(
          "YYYY-MM-DD"
        )}_${selectedBooking.timeSlot}`;
        setBookingData((prev) => ({
          ...prev,
          [bookingKey]: {
            ...prev[bookingKey],
            status: newStatus,
            statusId: 10,
            paymentStatus: "paid",
            originalStatus: "Completed",
          },
        }));

        const isConnected = signalRService.connected;
        if (isConnected) {
          const notification = {
            bookingId: selectedBooking.id,
            facilityId: selectedFacility,
            courtId: selectedBooking.courtId,
            timeSlot: selectedBooking.timeSlot,
            date: selectedDate.format("DD/MM/YYYY"),
            checkInTime: selectedBooking.timeSlot.split(" - ")[0],
            status: "completed",
            action: "completed",
            message: `Đơn đặt sân ${selectedBooking.courtName} đã được hoàn thành`,
            courtName: selectedBooking.courtName,
            customerName: selectedBooking.customerName,
          };
          await signalRService.sendBookingUpdate(notification);
        }

        setTimeout(() => closeModal(), 1000);
        setTimeout(
          () => selectedFacility && loadBookings(selectedFacility),
          1500
        );
      } else {
        const errorMessage =
          response.data?.message ||
          response.data?.error ||
          "Không thể hoàn thành đơn đặt sân";
        message.error(errorMessage);
      }
    } catch (error) {
      const errorMessage =
        error.response?.data?.message || "Có lỗi xảy ra. Vui lòng thử lại.";
      message.error(errorMessage);
    } finally {
      updateLoading("completing", false);
    }
  }, [
    selectedBooking,
    selectedDate,
    selectedFacility,
    loadBookings,
    updateLoading,
  ]);

  const handleCreateBooking = useCallback(
    async (values) => {
      if (selectedTimeSlots.length === 0) {
        message.error("Vui lòng chọn ít nhất một khung giờ");
        return;
      }

      try {
        updateLoading("creating", true);

        const USER_ID = getCourtOwnerId();
        const CUSTOMER_USER_ID = getCourtOwnerId();
        const CUSTOMER_EMAIL = user?.email || "admin@courtowner.com";
        const CUSTOMER_PHONE = user?.phone || user?.phoneNumber || "0000000000";

        const timeSlotIds = selectedTimeSlots.map((timeSlot) =>
          getTimeSlotId(timeSlot)
        );
        const checkInDate =
          selectedDate.format("YYYY-MM-DDTHH:mm:ss.SSS") + "Z";

        const bookingRequestData = {
          userId: CUSTOMER_USER_ID,
          email: CUSTOMER_EMAIL,
          phone: CUSTOMER_PHONE,
          checkInDate,
          timeSlotIds,
          facilityId: selectedFacility,
          categoryId: values.categoryId,
        };

        console.log("📡 Creating smart booking with data:", bookingRequestData);

        const response = await markSmartSlot(bookingRequestData);
        console.log("📡 MarkSmartSlot response:", response);

        const isSuccess =
          response.status === 200 ||
          response.status === 201 ||
          response.data?.success === true ||
          !response.data?.error;

        if (isSuccess) {
          message.success("Tạo đơn đặt sân thành công với trạng thái đã cọc!");

          const responseData = response.data?.data || response.data;

          if (responseData?.slots?.length > 0) {
            for (const slot of responseData.slots) {
              const timeSlot = `${formatTime(slot.startTime)} - ${formatTime(
                slot.endTime
              )}`;
              const bookingKey = getBookingKey(
                slot.courtId,
                selectedDate,
                timeSlot
              );

              const newBooking = {
                id: responseData.bookingId,
                userId: CUSTOMER_USER_ID,
                courtId: slot.courtId,
                courtName: slot.courtName,
                timeSlot: timeSlot,
                date: selectedDate.format("DD/MM/YYYY"),
                price: 0,
                status: "paid",
                paymentStatus: "deposit",
                bookingTime: dayjs().format("DD/MM/YYYY HH:mm:ss"),
                checkInDate: selectedDate.format("YYYY-MM-DD"),
                statusId: 7,
                originalStatus: "Paid",
                customerName:
                  responseData.user?.email?.split("@")[0] || "Admin",
                customerPhone: responseData.user?.phone || CUSTOMER_PHONE,
                customerEmail: responseData.user?.email || CUSTOMER_EMAIL,
              };

              setBookingData((prev) => ({
                ...prev,
                [bookingKey]: newBooking,
              }));
            }
          } else if (responseData?.bookingId) {
            for (const timeSlot of selectedTimeSlots) {
              const courtId =
                responseData.courtId ||
                (courts.length > 0 ? courts[0].courtId : null);
              const courtName =
                responseData.courtName ||
                (courts.length > 0 ? courts[0].courtName : "Unknown Court");

              if (courtId) {
                const bookingKey = getBookingKey(
                  courtId,
                  selectedDate,
                  timeSlot
                );

                const newBooking = {
                  id: responseData.bookingId,
                  userId: CUSTOMER_USER_ID,
                  courtId: courtId,
                  courtName: courtName,
                  timeSlot: timeSlot,
                  date: selectedDate.format("DD/MM/YYYY"),
                  price: 0,
                  status: "paid",
                  paymentStatus: "deposit",
                  bookingTime: dayjs().format("DD/MM/YYYY HH:mm:ss"),
                  checkInDate: selectedDate.format("YYYY-MM-DD"),
                  statusId: 7,
                  originalStatus: "Paid",
                  customerName: "Admin (Smart Booking)",
                  customerPhone: CUSTOMER_PHONE,
                  customerEmail: CUSTOMER_EMAIL,
                };

                setBookingData((prev) => ({
                  ...prev,
                  [bookingKey]: newBooking,
                }));
              }
            }
          }

          setTimeout(() => {
            setIsCreateBookingModalVisible(false);
            createBookingForm.resetFields();
            setSelectedTimeSlots([]);
          }, 3000);

          setTimeout(() => {
            if (selectedFacility) {
              loadBookings(selectedFacility);
            }
          }, 3000);
        } else {
          const errorMessage =
            response.data?.message ||
            response.data?.error ||
            "Không thể tạo đơn đặt sân";
          message.error(errorMessage);
        }
      } catch (error) {
        console.error("❌ Error creating smart booking:", error);

        if (
          error.code === "ECONNABORTED" &&
          error.message.includes("timeout")
        ) {
          message.warning("Yêu cầu quá thời gian chờ. Vui lòng kiểm tra lại!");

          setTimeout(() => {
            setIsCreateBookingModalVisible(false);
            createBookingForm.resetFields();
            setSelectedTimeSlots([]);
          }, 3000);

          setTimeout(() => {
            if (selectedFacility) {
              loadBookings(selectedFacility);
            }
          }, 3000);
        } else {
          const errorMessage =
            error.response?.data?.message || "Có lỗi xảy ra. Vui lòng thử lại.";
          message.error(errorMessage);
        }
      } finally {
        setTimeout(() => {
          updateLoading("creating", false);
        }, 2500);
      }
    },
    [
      selectedTimeSlots,
      selectedDate,
      selectedFacility,
      getTimeSlotId,
      loadBookings,
      updateLoading,
      formatTime,
      courts,
      createBookingForm,
      getCourtOwnerId,
      user,
    ]
  );

  const openCreateBookingModal = useCallback(async () => {
    if (!selectedFacility) {
      message.warning("Vui lòng chọn cơ sở trước");
      return;
    }

    setSelectedTimeSlots([]);
    setIsCreateBookingModalVisible(true);
    await loadModalCategories(selectedFacility);
    createBookingForm.setFieldsValue({ categoryId: undefined });
  }, [selectedFacility, loadModalCategories, createBookingForm]);

  const closeCreateBookingModal = useCallback(() => {
    setIsCreateBookingModalVisible(false);
    createBookingForm.resetFields();
    setSelectedTimeSlots([]);
    updateLoading("creating", false);
  }, [createBookingForm, updateLoading]);

  const closeModal = useCallback(() => {
    setIsModalVisible(false);
    setSelectedBooking(null);
    updateLoading("customer", false);
    updateLoading("completing", false);
  }, [updateLoading]);

  const getSlotStatus = useCallback(
    (court, timeSlot) => {
      const bookingKey = getBookingKey(
        court.courtId || court.id,
        selectedDate,
        timeSlot
      );
      const booking = bookingData[bookingKey];
      return booking ? booking.status : "available";
    },
    [bookingData, selectedDate, getBookingKey]
  );

  const handleCategoryChange = useCallback(
    (categoryId) => {
      createBookingForm.setFieldsValue({ categoryId });
      setSelectedTimeSlots([]);
    },
    [createBookingForm]
  );

  // ✅ STEP 6: All useMemo hooks - BEFORE render callbacks
  const contextMenuItems = useMemo(
    () => [
      {
        key: "mark",
        icon: <FlagOutlined />,
        label: "Đánh dấu sân",
        onClick: () => {
          if (slotContextMenu.court && slotContextMenu.timeSlot) {
            handleMarkCourt(slotContextMenu.court, slotContextMenu.timeSlot);
          }
        },
      },
    ],
    [
      slotContextMenu.court,
      slotContextMenu.timeSlot,
      handleMarkCourt,
      hideContextMenu,
    ]
  );

  const filteredCourts = useMemo(() => {
    if (selectedCategoryFilter === "all") return courts;

    const selectedCategory = modalCategories.find(
      (cat) => String(cat.categoryId) === String(selectedCategoryFilter)
    );

    if (!selectedCategory) return [];

    // So sánh categoryId thay vì categoryName để chắc chắn đúng
    return courts.filter(
      (court) =>
        String(court.categoryId) === String(selectedCategory.categoryId)
    );
  }, [courts, selectedCategoryFilter, modalCategories]);

  const bookingStatusCounts = useMemo(() => {
    const totalSlots = courts.length * timeSlots.length;
    const bookingValues = Object.values(bookingData);

    return {
      available: totalSlots - bookingValues.length,
      unpaid: bookingValues.filter((b) => b.status === "unpaid").length,
      paid: bookingValues.filter((b) => b.status === "paid").length,
      completed: bookingValues.filter((b) => b.status === "completed").length,
      cancelled: bookingValues.filter((b) => b.status === "cancelled").length,
    };
  }, [courts.length, timeSlots.length, bookingData]);

  // ✅ Render callbacks AFTER useMemo
  const getAvailableTimeSlots = useCallback(() => {
    const selectedCategoryId = createBookingForm.getFieldValue("categoryId");

    if (!selectedCategoryId) {
      return [];
    }

    // Lấy đúng categoryId (dạng string hoặc number)
    const courtsOfSelectedCategory = courts.filter(
      (court) => String(court.categoryId) === String(selectedCategoryId)
    );

    if (courtsOfSelectedCategory.length === 0) return [];

    const availableTimeSlots = timeSlots.filter((timeSlot) => {
      const hasAvailableSlot = courtsOfSelectedCategory.some((court) => {
        const bookingKey = getBookingKey(
          court.courtId || court.id,
          selectedDate,
          timeSlot
        );
        return (
          !bookingData[bookingKey] ||
          bookingData[bookingKey]?.status === "cancelled"
        );
      });

      return hasAvailableSlot;
    });

    return availableTimeSlots;
  }, [
    courts,
    timeSlots,
    selectedDate,
    getBookingKey,
    bookingData,
    createBookingForm,
  ]);

  const renderTimeSlotGrid = useCallback(() => {
    if (loading.timeSlots) {
      return (
        <div className="loading-slots">
          <Spin size="large" />
          <span>Đang tải khung giờ...</span>
        </div>
      );
    }

    const availableSlots = getAvailableTimeSlots();

    if (availableSlots.length === 0) {
      const selectedCategoryId = createBookingForm.getFieldValue("categoryId");

      return (
        <div className="empty-slots">
          <ClockCircleOutlined className="empty-icon" />
          <span>
            {!selectedCategoryId
              ? "Vui lòng chọn loại sân trước"
              : "Không có khung giờ trống nào"}
          </span>
        </div>
      );
    }

    return availableSlots.map((slot) => {
      const selectedCategoryId = createBookingForm.getFieldValue("categoryId");
      // Sửa đoạn này: so sánh categoryId thay vì categoryName
      const courtsOfCategory = courts.filter(
        (court) => String(court.categoryId) === String(selectedCategoryId)
      );

      const availableCourtsCount = courtsOfCategory.filter((court) => {
        const bookingKey = getBookingKey(
          court.courtId || court.id,
          selectedDate,
          slot
        );
        return (
          !bookingData[bookingKey] ||
          bookingData[bookingKey]?.status === "cancelled"
        );
      }).length;

      return (
        <div
          key={slot}
          className={`time-slot-card ${selectedTimeSlots.includes(slot) ? "selected" : ""
            }`}
          onClick={() => handleTimeSlotToggle(slot)}
        >
          <div className="slot-time">
            <ClockCircleOutlined className="slot-icon" />
            <span>{slot}</span>
          </div>
          <div className="slot-info">
            <span className="available-count">
              {availableCourtsCount} sân trống
            </span>
          </div>
          <div className="slot-indicator">
            {selectedTimeSlots.includes(slot) && (
              <CheckCircleOutlined className="check-icon" />
            )}
          </div>
        </div>
      );
    });
  }, [
    timeSlots,
    selectedTimeSlots,
    loading.timeSlots,
    handleTimeSlotToggle,
    getAvailableTimeSlots,
    modalCategories,
    courts,
    selectedDate,
    getBookingKey,
    bookingData,
    createBookingForm,
  ]);

  const renderBookingTable = useCallback(() => {
    if (loading.courts || loading.bookings) {
      return (
        <div className="loading-container">
          <div>
            <Spin size="large" />
            <span>Đang tải dữ liệu...</span>
          </div>
        </div>
      );
    }

    if (filteredCourts.length === 0) {
      return (
        <div className="no-data-container">
          <div>
            <span>
              {selectedCategoryFilter === "all"
                ? selectedFacility
                  ? "Không có sân nào"
                  : "Vui lòng chọn cơ sở"
                : "Không có sân nào thuộc loại này"}
            </span>
          </div>
        </div>
      );
    }

    if (timeSlots.length === 0) {
      return (
        <div className="no-data-container">
          <div>
            <span>Không có khung giờ</span>
          </div>
        </div>
      );
    }

    return filteredCourts.map((court) => (
      <div key={court.courtId || court.id} className="court-row">
        <div className="court-cell">
          <HomeOutlined />
          <span>{court.courtName || court.name}</span>
        </div>
        {timeSlots.map((slot) => {
          const status = getSlotStatus(court, slot);
          const isAvailable = status === "available";

          return (
            <div
              key={`${court.courtId || court.id}-${slot}`}
              className={`slot-cell ${status}`}
              onClick={
                isAvailable
                  ? (e) => handleEmptySlotClick(e, court, slot)
                  : () => handleSlotClick(court, slot)
              }
              onContextMenu={
                isAvailable
                  ? (e) => handleEmptySlotClick(e, court, slot)
                  : undefined
              }
              title={`${court.courtName || court.name
                } - ${slot} - ${getSlotDisplayText(status)}`}
              style={{
                cursor: isAvailable ? "context-menu" : "pointer",
                position: "relative",
              }}
            >
              <span className="slot-text">{getSlotDisplayText(status)}</span>
              {isAvailable && (
                <MoreOutlined
                  className="slot-menu-icon"
                  style={{
                    position: "absolute",
                    top: "2px",
                    right: "2px",
                    fontSize: "10px",
                    opacity: 0.6,
                  }}
                />
              )}
            </div>
          );
        })}
      </div>
    ));
  }, [
    loading.courts,
    loading.bookings,
    filteredCourts,
    timeSlots,
    selectedCategoryFilter,
    selectedFacility,
    getSlotStatus,
    handleSlotClick,
    handleEmptySlotClick,
    getSlotDisplayText,
  ]);

  // ✅ STEP 7: All useEffect hooks
  useEffect(() => {
    console.log("🔄 Auth state changed:", {
      authLoading,
      isLoggedIn,
      userId,
      hasInitialized,
    });

    if (!authLoading && isLoggedIn && userId && !hasInitialized) {
      console.log("🚀 Conditions met, loading facilities...");
      loadFacilities();
      setHasInitialized(true);
    }
  }, [authLoading, isLoggedIn, userId, hasInitialized, loadFacilities]);

  useEffect(() => {
    if (!authLoading && (!isLoggedIn || !userId)) {
      console.log("🔄 User logged out or changed, resetting...");
      setHasInitialized(false);
      setFacilities([]);
    }
  }, [authLoading, isLoggedIn, userId]);

  useEffect(() => {
    console.log("📋 BookingManagement - Current Values:", {
      authLoading,
      isLoggedIn,
      userId,
      hasInitialized,
      userEmail: user?.email,
      facilitiesCount: facilities.length,
    });
  }, [
    authLoading,
    isLoggedIn,
    userId,
    hasInitialized,
    user,
    facilities.length,
  ]);

  useEffect(() => {
    if (!selectedFacility) return;

    const manageFacilityGroups = async () => {
      await signalRService.joinFacilityGroup(selectedFacility);
      console.log(`📍 LOCAL: Joined facility group ${selectedFacility}`);
    };

    if (signalRService.connected) {
      manageFacilityGroups();
    }

    return () => {
      if (selectedFacility) {
        signalRService.leaveFacilityGroup(selectedFacility);
        console.log(`📤 LOCAL: Left facility group ${selectedFacility}`);
      }
    };
  }, [selectedFacility]);

  useEffect(() => {
    const handleOpenNotificationDetail = (event) => {
      console.log("🔔 Received notification detail event:", event.detail);
      setNotificationBookingDetail(event.detail);
      setIsNotificationDetailVisible(true);
    };

    window.addEventListener(
      "openNotificationDetail",
      handleOpenNotificationDetail
    );

    return () => {
      window.removeEventListener(
        "openNotificationDetail",
        handleOpenNotificationDetail
      );
    };
  }, []);

  useEffect(() => {
    const handleClickOutside = () => {
      if (slotContextMenu.visible) {
        hideContextMenu();
      }
    };

    document.addEventListener("click", handleClickOutside);
    return () => document.removeEventListener("click", handleClickOutside);
  }, [slotContextMenu.visible, hideContextMenu]);

  useEffect(() => {
    const handleGlobalBookingCreated = (notification) => {
      console.log(
        "🔔 LOCAL UI: Global booking created received!",
        notification
      );

      if (notification.facilityId === selectedFacility) {
        const notificationDate = dayjs(notification.date, "DD/MM/YYYY");
        const currentDate = selectedDate;

        if (
          notificationDate.format("YYYY-MM-DD") ===
          currentDate.format("YYYY-MM-DD")
        ) {
          console.log("📱 LOCAL UI: Updating slot status directly...");

          const timeSlot = notification.timeSlot;
          const courtId = notification.courtId;

          if (courtId && timeSlot) {
            const bookingKey = `${courtId}_${currentDate.format(
              "YYYY-MM-DD"
            )}_${timeSlot}`;

            const actualStatus = getBookingStatusFromString(
              notification.status
            );

            console.log("🔍 DEBUG: Notification status:", notification.status);
            console.log("🔍 DEBUG: Mapped status:", actualStatus);

            let paymentStatus = "pending";
            let statusId = 8;
            let originalStatus = notification.status || "Unpaid";

            switch (actualStatus) {
              case "paid":
                paymentStatus = "deposit";
                statusId = 7;
                break;
              case "completed":
                paymentStatus = "paid";
                statusId = 10;
                break;
              case "cancelled":
                paymentStatus = "cancelled";
                statusId = 9;
                break;
              case "unpaid":
              default:
                paymentStatus = "pending";
                statusId = 8;
                break;
            }

            const newBooking = {
              id: notification.bookingId || Date.now(),
              userId: notification.userId,
              courtId: courtId,
              courtName: notification.courtName || "Sân thể thao",
              timeSlot: timeSlot,
              date: currentDate.format("DD/MM/YYYY"),
              price: notification.totalAmount || 0,
              status: actualStatus,
              paymentStatus: paymentStatus,
              bookingTime: dayjs().format("DD/MM/YYYY HH:mm:ss"),
              checkInDate: currentDate.format("YYYY-MM-DD"),
              statusId: statusId,
              originalStatus: originalStatus,
              customerName: notification.customerName || "Admin",
              customerPhone: notification.customerPhone || "N/A",
              customerEmail: notification.customerEmail || "N/A",
            };

            setBookingData((prev) => ({
              ...prev,
              [bookingKey]: newBooking,
            }));

            console.log(
              `✅ LOCAL UI: Slot ${timeSlot} updated to ${actualStatus.toUpperCase()} status`
            );
          }
        }
      }
    };

    const handleGlobalBookingUpdated = (notification) => {
      console.log(
        "🔔 LOCAL UI: Global booking updated received!",
        notification
      );

      if (notification.facilityId === selectedFacility) {
        const notificationDate = dayjs(notification.date, "DD/MM/YYYY");
        const currentDate = selectedDate;

        if (
          notificationDate.format("YYYY-MM-DD") ===
          currentDate.format("YYYY-MM-DD")
        ) {
          console.log(
            "📱 LOCAL UI: Updating slot status to:",
            notification.status
          );

          const timeSlot = notification.timeSlot;
          const courtId = notification.courtId;

          if (courtId && timeSlot) {
            const bookingKey = `${courtId}_${currentDate.format(
              "YYYY-MM-DD"
            )}_${timeSlot}`;

            setBookingData((prev) => {
              if (prev[bookingKey]) {
                const newStatus = getBookingStatusFromString(
                  notification.status
                );
                let paymentStatus = prev[bookingKey].paymentStatus;
                let statusId = prev[bookingKey].statusId;

                if (newStatus === "completed") {
                  paymentStatus = "paid";
                  statusId = 10;
                } else if (newStatus === "cancelled") {
                  paymentStatus = "cancelled";
                  statusId = 9;
                }

                const updatedBooking = {
                  ...prev[bookingKey],
                  status: newStatus,
                  originalStatus: notification.status,
                  paymentStatus,
                  statusId,
                };

                console.log(
                  `✅ LOCAL UI: Slot ${timeSlot} updated to ${notification.status.toUpperCase()} status`
                );

                return {
                  ...prev,
                  [bookingKey]: updatedBooking,
                };
              }
              return prev;
            });

            if (
              selectedBooking &&
              selectedBooking.id.toString() ===
              notification.bookingId.toString()
            ) {
              const newStatus = getBookingStatusFromString(notification.status);
              let paymentStatus = selectedBooking.paymentStatus;
              let statusId = selectedBooking.statusId;

              if (newStatus === "completed") {
                paymentStatus = "paid";
                statusId = 10;
              } else if (newStatus === "cancelled") {
                paymentStatus = "cancelled";
                statusId = 9;
              }

              setSelectedBooking((prev) =>
                prev
                  ? {
                    ...prev,
                    status: newStatus,
                    originalStatus: notification.status,
                    paymentStatus,
                    statusId,
                  }
                  : null
              );
            }
          }
        }
      }
    };

    if (signalRService.connection) {
      signalRService.connection.on(
        "BookingCreated",
        handleGlobalBookingCreated
      );
      signalRService.connection.on(
        "BookingUpdated",
        handleGlobalBookingUpdated
      );
      signalRService.connection.on(
        "BookingCompleted",
        handleGlobalBookingUpdated
      );
      signalRService.connection.on(
        "BookingCancelled",
        handleGlobalBookingUpdated
      );

      console.log(
        "✅ LOCAL UI: Added SignalR event listeners for slot updates"
      );
    }

    return () => {
      if (signalRService.connection) {
        signalRService.connection.off(
          "BookingCreated",
          handleGlobalBookingCreated
        );
        signalRService.connection.off(
          "BookingUpdated",
          handleGlobalBookingUpdated
        );
        signalRService.connection.off(
          "BookingCompleted",
          handleGlobalBookingUpdated
        );
        signalRService.connection.off(
          "BookingCancelled",
          handleGlobalBookingUpdated
        );

        console.log("🧹 LOCAL UI: Removed SignalR event listeners");
      }
    };
  }, [
    selectedFacility,
    selectedDate,
    selectedBooking,
    getBookingStatusFromString,
  ]);

  useEffect(() => {
    const updateConnectionState = () => {
      const currentConnected = signalRService.connected;
      if (currentConnected !== isRealTimeConnected) {
        setIsRealTimeConnected(currentConnected);
        console.log(`🔗 LOCAL: Connection state updated: ${currentConnected}`);
      }
    };

    updateConnectionState();

    const interval = setInterval(updateConnectionState, 2000);

    return () => clearInterval(interval);
  }, [isRealTimeConnected]);

  useEffect(() => {
    if (selectedFacility) {
      loadCourts(selectedFacility);
      loadTimeSlots(selectedFacility);
      loadBookings(selectedFacility);
      loadModalCategories(selectedFacility);
    } else {
      setCourts([]);
      setTimeSlots([]);
      setRawTimeSlots([]);
      setBookings([]);
      setBookingData({});
      setModalCategories([]);
    }
  }, [
    selectedFacility,
    loadCourts,
    loadTimeSlots,
    loadBookings,
    loadModalCategories,
  ]);

  useEffect(() => {
    if (selectedFacility) {
      loadBookings(selectedFacility);
    }
  }, [selectedDate, selectedFacility, loadBookings]);

  useEffect(() => {
    if (isCreateBookingModalVisible) {
      createBookingForm.setFieldsValue({ categoryId: undefined });
    }
  }, [modalCategories, isCreateBookingModalVisible, createBookingForm]);

  // ✅ STEP 8: Early returns AFTER all hooks
  if (authLoading) {
    return (
      <div style={{ padding: "20px", textAlign: "center" }}>
        <div>⏳ Đang tải thông tin người dùng...</div>
        <div style={{ fontSize: "12px", marginTop: "10px" }}>
          Auth Loading: true | User ID: {userId || "null"}
        </div>
      </div>
    );
  }
  if (!isLoggedIn || !getCourtOwnerId()) {
    return (
      <div style={{ padding: "20px", textAlign: "center" }}>
        <div>❌ Vui lòng đăng nhập để tiếp tục</div>
        <div style={{ fontSize: "12px", marginTop: "10px" }}>
          Is Logged In: {isLoggedIn ? "true" : "false"} | User ID:{" "}
          {getCourtOwnerId() || "null"}
        </div>
      </div>
    );
  }

  // ✅ STEP 9: Main render
  return (
    <div className="booking-management" onClick={hideContextMenu}>
      {/* ✅ DEBUG: Show current user */}
      <div className="main-container">
        {/* Header */}
        <div className="header">
          <div className="header-content">
            <HomeOutlined className="header-icon" />
            <div className="header-text">
              <div
                style={{ display: "flex", alignItems: "center", gap: "12px" }}
              >
                <h1 style={{ color: "white", margin: 0 }}>
                  Quản Lý Đơn Đặt Sân
                </h1>
              </div>
              <div className="subtitle">
                Theo dõi và quản lý lịch đặt sân hiệu quả
              </div>
            </div>
          </div>
        </div>

        {/* Filters */}
        <div className="filters-section">
          <div className="filters-container">
            <div className="filter-item">
              <HomeOutlined className="filter-icon" />
              <span className="filter-label">Chọn Cơ Sở</span>
              <Select
                value={selectedFacility}
                onChange={handleFacilityChange}
                className="filter-select"
                loading={loading.facilities}
                placeholder="Chọn cơ sở"
                notFoundContent={
                  loading.facilities ? (
                    <Spin size="small" />
                  ) : (
                    "Không có dữ liệu"
                  )
                }
              >
                {facilities.map((facility) => (
                  <Option key={facility.facilityId} value={facility.facilityId}>
                    {facility.facilityName}
                  </Option>
                ))}
              </Select>
            </div>

            <div className="filter-item">
              <CalendarOutlined className="filter-icon" />
              <span className="filter-label">Chọn ngày</span>
              <DatePicker
                value={selectedDate}
                onChange={handleDateChange}
                format="DD/MM/YYYY"
                className="filter-date"
              />
            </div>

            <div className="filter-item">
              <Button
                type="primary"
                icon={<PlusOutlined />}
                onClick={openCreateBookingModal}
                className="mark-court-button"
              >
                Đánh Dấu Sân Thông Minh
              </Button>
            </div>
          </div>
        </div>

        {/* Status indicators + Category Filter */}
        <div className="status-indicator" style={{ flexWrap: "wrap" }}>
          <div className="status-item">
            <div className="status-dot available"></div>
            <span>Còn trống ({bookingStatusCounts.available} slots)</span>
          </div>
          <div className="status-item">
            <div className="status-dot paid"></div>
            <span>Đã Cọc ({bookingStatusCounts.paid} slots)</span>
          </div>
          <div className="status-item">
            <div className="status-dot unpaid"></div>
            <span>Chưa thanh toán ({bookingStatusCounts.unpaid} slots)</span>
          </div>
          <div className="status-item">
            <div className="status-dot completed"></div>
            <span>Đã hoàn thành ({bookingStatusCounts.completed} slots)</span>
          </div>
          <div className="status-item">
            <div className="status-dot cancelled"></div>
            <span>Đã hủy ({bookingStatusCounts.cancelled} slots)</span>
          </div>

          <div
            className="category-filter"
            style={{
              display: "flex",
              alignItems: "center",
              gap: 12,
              marginLeft: 32,
            }}
          >
            <span style={{ fontWeight: 500 }}>Loại sân:</span>
            <select
              style={{
                minWidth: 160,
                padding: "6px 12px",
                borderRadius: 6,
                border: "1px solid #d9d9d9",
                fontSize: 15,
                background: "#fff",
              }}
              value={selectedCategoryFilter}
              onChange={(e) => setSelectedCategoryFilter(e.target.value)}
              disabled={loading.modalCategories}
            >
              <option value="all">Tất cả</option>
              {modalCategories.map((category) => (
                <option key={category.categoryId} value={category.categoryId}>
                  {category.categoryName}
                </option>
              ))}
            </select>
            {loading.modalCategories && (
              <Spin size="small" style={{ marginLeft: 8 }} />
            )}
          </div>
        </div>

        {/* Booking Table */}
        <div className="booking-table" style={{ width: "95%" }}>
          <div className="table-scroll-container">
            <div className="table-content">
              {/* Table Header */}
              <div className="table-header">
                <div className="header-row">
                  <div className="header-cell time-header">
                    <ClockCircleOutlined />
                    <span>KHUNG GIỜ</span>
                  </div>
                  {loading.timeSlots ? (
                    <div className="header-cell loading-header">
                      <Spin size="small" />
                      <span>Đang tải...</span>
                    </div>
                  ) : timeSlots.length === 0 ? (
                    <div className="header-cell empty-header">
                      <span>Không có khung giờ</span>
                    </div>
                  ) : (
                    timeSlots.map((slot) => (
                      <div key={slot} className="header-cell time-slot">
                        {slot}
                      </div>
                    ))
                  )}
                </div>
              </div>

              {/* Table Body */}
              <div className="table-body">{renderBookingTable()}</div>
            </div>
          </div>
        </div>
      </div>

      {/* ✅ Context Menu for Empty Slots */}
      {slotContextMenu.visible && (
        <div
          className="slot-context-menu"
          style={{
            position: "fixed",
            top: slotContextMenu.y,
            left: slotContextMenu.x,
            zIndex: 1000,
            backgroundColor: "white",
            border: "1px solid #d9d9d9",
            borderRadius: "6px",
            boxShadow: "0 4px 12px rgba(0, 0, 0, 0.15)",
            padding: "8px 0",
            minWidth: "160px",
          }}
          onClick={(e) => e.stopPropagation()}
        >
          {contextMenuItems.map((item) => (
            <div
              key={item.key}
              className={`context-menu-item ${item.disabled ? "disabled" : ""}`}
              style={{
                padding: "8px 16px",
                cursor: item.disabled ? "not-allowed" : "pointer",
                display: "flex",
                alignItems: "center",
                gap: "8px",
                opacity: item.disabled ? 0.5 : 1,
                transition: "background-color 0.2s",
              }}
              onClick={item.disabled ? undefined : item.onClick}
              onMouseEnter={(e) => {
                if (!item.disabled) {
                  e.target.style.backgroundColor = "#f5f5f5";
                }
              }}
              onMouseLeave={(e) => {
                e.target.style.backgroundColor = "transparent";
              }}
            >
              {item.icon}
              <span>{item.label}</span>
              {loading.markingCourt && item.key === "mark" && (
                <Spin size="small" style={{ marginLeft: "auto" }} />
              )}
            </div>
          ))}
        </div>
      )}

      {/* Booking Detail Modal */}
      <Modal
        title={null}
        open={isModalVisible}
        onCancel={closeModal}
        footer={null}
        width={650}
        className="booking-detail-modal"
        centered
      >
        {selectedBooking && (
          <div className="booking-detail">
            <div className="modal-header">
              <h2>Chi tiết đặt sân</h2>
              <div className="status-tag">
                {getStatusDisplayText(
                  selectedBooking.status,
                  selectedBooking.originalStatus
                )}
              </div>
            </div>

            <div className="booking-info">
              <div className="info-row">
                <div className="info-item">
                  <div className="info-icon">
                    <HomeOutlined />
                  </div>
                  <div className="info-content">
                    <div className="label">Sân</div>
                    <div className="value">{selectedBooking.courtName}</div>
                  </div>
                </div>
                <div className="info-item">
                  <div className="info-icon">
                    <CalendarOutlined />
                  </div>
                  <div className="info-content">
                    <div className="label">Ngày</div>
                    <div className="value">{selectedBooking.date}</div>
                  </div>
                </div>
              </div>

              <div className="info-row">
                <div className="info-item">
                  <div className="info-icon">
                    <ClockCircleOutlined />
                  </div>
                  <div className="info-content">
                    <div className="label">Giờ</div>
                    <div className="value">{selectedBooking.timeSlot}</div>
                  </div>
                </div>
                <div className="info-item">
                  <div className="info-icon">
                    <DollarOutlined />
                  </div>
                  <div className="info-content">
                    <div className="label">Giá</div>
                    <div className="value price">
                      {selectedBooking.price.toLocaleString("vi-VN")} VNĐ
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div className="customer-info">
              <h3>Thông tin khách hàng</h3>
              <div className="customer-details">
                {loading.customer ? (
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "16px",
                      padding: "20px",
                    }}
                  >
                    <Spin size="large" />
                    <span>Đang tải thông tin khách hàng...</span>
                  </div>
                ) : (
                  <>
                    <Avatar
                      size={64}
                      src={selectedBooking.customerAvatar}
                      icon={!selectedBooking.customerAvatar && <UserOutlined />}
                      className="avatar"
                    />
                    <div className="customer-data">
                      <div className="customer-item">
                        <UserOutlined className="customer-icon" />
                        <span>{selectedBooking.customerName}</span>
                      </div>
                      <div className="customer-item">
                        <PhoneOutlined className="customer-icon" />
                        <span>{selectedBooking.customerPhone}</span>
                      </div>
                      <div className="customer-item">
                        <MailOutlined className="customer-icon" />
                        <span>{selectedBooking.customerEmail}</span>
                      </div>
                    </div>
                  </>
                )}
              </div>
            </div>

            <div className="payment-status">
              <div className="payment-info">
                {selectedBooking.paymentStatus === "deposit" ? (
                  <>
                    <CheckCircleOutlined className="payment-icon deposit" />
                    <span className="payment-text deposit">
                      Đã thanh toán cọc
                    </span>
                  </>
                ) : selectedBooking.paymentStatus === "paid" ? (
                  <>
                    <CheckCircleOutlined className="payment-icon paid" />
                    <span className="payment-text paid">Đã thanh toán</span>
                  </>
                ) : selectedBooking.paymentStatus === "cancelled" ? (
                  <>
                    <CloseCircleOutlined className="payment-icon cancelled" />
                    <span className="payment-text cancelled">Đã hủy</span>
                  </>
                ) : (
                  <>
                    <CloseCircleOutlined className="payment-icon pending" />
                    <span className="payment-text pending">
                      Chưa thanh toán
                    </span>
                  </>
                )}
              </div>
              <div className="booking-time">
                Đặt lúc: {selectedBooking.bookingTime}
              </div>
            </div>

            <div className="modal-actions">
              <Button onClick={closeModal}>Đóng</Button>
              {selectedBooking.status === "paid" &&
                !dayjs(selectedBooking.checkInDate).isAfter(dayjs(), "day") ? (
                <Button
                  type="primary"
                  className="action-button"
                  onClick={handleCompleteBooking}
                  loading={loading.completing}
                  icon={<CheckCircleOutlined />}
                >
                  {loading.completing ? "Đang hoàn thành..." : "Hoàn Thành Đơn"}
                </Button>
              ) : selectedBooking.status === "completed" ? (
                <Button
                  type="primary"
                  className="action-button completed"
                  disabled
                  icon={<CheckCircleOutlined />}
                >
                  Đã Hoàn Thành
                </Button>
              ) : selectedBooking.status === "cancelled" ? (
                <Button
                  type="default"
                  className="action-button cancelled"
                  disabled
                  icon={<StopOutlined />}
                >
                  Đơn Đã Hủy
                </Button>
              ) : null}
            </div>
          </div>
        )}
      </Modal>

      {/* Modal Chi tiết Booking từ Notification */}
      <Modal
        title={
          <div
            style={{
              display: "flex",
              alignItems: "center",
              color: "#52c41a",
              fontSize: "18px",
              fontWeight: "bold",
            }}
          >
            <CalendarOutlined
              style={{ marginRight: "12px", fontSize: "20px" }}
            />
            Chi tiết đơn đặt sân mới
          </div>
        }
        open={isNotificationDetailVisible}
        onCancel={() => {
          setIsNotificationDetailVisible(false);
          setNotificationBookingDetail(null);
        }}
        footer={[
          <Button
            key="close"
            onClick={() => {
              setIsNotificationDetailVisible(false);
              setNotificationBookingDetail(null);
            }}
          >
            Đóng
          </Button>,
          notificationBookingDetail?.bookingId && (
            <Button
              key="detail"
              type="primary"
              onClick={async () => {
                setIsNotificationDetailVisible(false);
                await openModal(notificationBookingDetail.bookingId);
                setNotificationBookingDetail(null);
              }}
            >
              Xem chi tiết đầy đủ
            </Button>
          ),
        ]}
        width={500}
        centered
        styles={{ body: { padding: "24px" } }}
      >
        {notificationBookingDetail &&
          (() => {
            const getStatusInfo = (statusId) => {
              switch (statusId) {
                case 7:
                  return {
                    text: "Đã đặt cọc",
                    color: "#faad14",
                    backgroundColor: "#fff7e6",
                    borderColor: "#ffd591",
                    icon: <InfoCircleOutlined />,
                  };
                case 8:
                  return {
                    text: "Chưa thanh toán",
                    color: "#ff4d4f",
                    backgroundColor: "#fff1f0",
                    borderColor: "#ffccc7",
                    icon: <CloseCircleOutlined />,
                  };
                case 9:
                  return {
                    text: "Đã hủy",
                    color: "#8c8c8c",
                    backgroundColor: "#f5f5f5",
                    borderColor: "#d9d9d9",
                    icon: <CloseCircleOutlined />,
                  };
                case 10:
                  return {
                    text: "Đã thanh toán",
                    color: "#52c41a",
                    backgroundColor: "#f6ffed",
                    borderColor: "#b7eb8f",
                    icon: <CheckCircleOutlined />,
                  };
                default:
                  return {
                    text: "Chưa xác định",
                    color: "#8c8c8c",
                    backgroundColor: "#f5f5f5",
                    borderColor: "#d9d9d9",
                    icon: <InfoCircleOutlined />,
                  };
              }
            };

            const statusInfo = getStatusInfo(
              notificationBookingDetail.statusId || 8
            );

            return (
              <div className="notification-booking-detail">
                <Row gutter={[16, 16]}>
                  <Col span={24}>
                    <Card
                      size="small"
                      style={{
                        backgroundColor: statusInfo.backgroundColor,
                        border: `1px solid ${statusInfo.borderColor}`,
                      }}
                    >
                      <Statistic
                        title="Trạng thái"
                        value={statusInfo.text}
                        valueStyle={{
                          color: statusInfo.color,
                          fontSize: "16px",
                        }}
                        prefix={statusInfo.icon}
                      />
                    </Card>
                  </Col>

                  <Col span={12}>
                    <div className="detail-item">
                      <Text strong>Mã booking:</Text>
                      <br />
                      <Text>#{notificationBookingDetail.bookingId}</Text>
                    </div>
                  </Col>

                  <Col span={12}>
                    <div className="detail-item">
                      <Text strong>Sân:</Text>
                      <br />
                      <Text>{notificationBookingDetail.courtName}</Text>
                    </div>
                  </Col>

                  <Col span={12}>
                    <div className="detail-item">
                      <Text strong>Khách hàng:</Text>
                      <br />
                      <Text>{notificationBookingDetail.customerName}</Text>
                    </div>
                  </Col>

                  <Col span={12}>
                    <div className="detail-item">
                      <Text strong>Điện thoại:</Text>
                      <br />
                      <Text>
                        {notificationBookingDetail.customerPhone || "Chưa có"}
                      </Text>
                    </div>
                  </Col>

                  <Col span={12}>
                    <div className="detail-item">
                      <Text strong>Ngày đặt:</Text>
                      <br />
                      <Text>{notificationBookingDetail.date}</Text>
                    </div>
                  </Col>

                  <Col span={12}>
                    <div className="detail-item">
                      <Text strong>Khung Giờ:</Text>
                      <br />
                      <Text>{notificationBookingDetail.timeSlot}</Text>
                    </div>
                  </Col>

                  {notificationBookingDetail.totalAmount != null && (
                    <Col span={24}>
                      <Card
                        size="small"
                        style={{
                          backgroundColor: "#fff7e6",
                          border: "1px solid #ffd591",
                        }}
                      >
                        <Statistic
                          title="Tổng tiền"
                          value={notificationBookingDetail.totalAmount}
                          suffix="VND"
                          valueStyle={{
                            color: "#fa8c16",
                            fontSize: "18px",
                            fontWeight: "bold",
                          }}
                          formatter={(value) => value.toLocaleString()}
                        />
                      </Card>
                    </Col>
                  )}

                  <Col span={24}>
                    <div className="detail-item">
                      <Text strong>Thời gian tạo:</Text>
                      <br />
                      <Text>
                        {dayjs(notificationBookingDetail.timestamp).format(
                          "DD/MM/YYYY HH:mm:ss"
                        )}
                      </Text>
                    </div>
                  </Col>
                </Row>
              </div>
            );
          })()}
      </Modal>

      {/* Create Booking Modal */}
      <Modal
        title={null}
        open={isCreateBookingModalVisible}
        onCancel={closeCreateBookingModal}
        footer={null}
        width={700}
        className="create-booking-modal-modern"
        centered
        styles={{ body: { padding: 0 } }}
      >
        <div className="modern-modal-wrapper">
          <div className="modern-modal-header">
            <div className="header-background">
              <div className="header-pattern"></div>
            </div>
            <div className="header-content">
              <div className="header-icon-wrapper">
                <PlusOutlined className="header-icon" />
              </div>
              <div className="header-text">
                <h2>Tạo Đơn Đặt Sân Mới</h2>
                <p>Vui lòng điền thông tin để tạo đơn đặt sân</p>
              </div>
            </div>
          </div>

          <div className="modern-modal-body">
            <Form
              form={createBookingForm}
              layout="vertical"
              onFinish={handleCreateBooking}
              className="modern-booking-form"
              preserve={false}
            >
              <div className="form-section-modern">
                <div className="section-header">
                  <div className="section-icon">
                    <AppstoreOutlined />
                  </div>
                  <div className="section-title">
                    <h3>Loại Sân</h3>
                    <span>Chọn loại sân phù hợp</span>
                  </div>
                </div>
                <Form.Item
                  name="categoryId"
                  label="Loại sân"
                  rules={[
                    { required: true, message: "Vui lòng chọn loại sân" },
                  ]}
                  className="form-item-half"
                >
                  <select
                    style={{ width: "100%", padding: 8, borderRadius: 4 }}
                    value={createBookingForm.getFieldValue("categoryId") || ""}
                    onChange={(e) => handleCategoryChange(e.target.value)}
                  >
                    <option value="">Chọn loại sân</option>
                    {modalCategories.map((category) => (
                      <option
                        key={category.categoryId}
                        value={category.categoryId}
                      >
                        {category.categoryName}
                      </option>
                    ))}
                  </select>
                </Form.Item>
              </div>

              <div className="form-section-modern">
                <div className="section-header">
                  <div className="section-icon">
                    <ClockCircleOutlined />
                  </div>
                  <div className="section-title">
                    <h3>Khung Giờ</h3>
                    <span>Chọn các khung giờ muốn đặt</span>
                  </div>
                </div>
                <div className="time-slots-grid">{renderTimeSlotGrid()}</div>
                {selectedTimeSlots.length > 0 && (
                  <div className="selected-summary">
                    <div className="summary-header">
                      <CheckCircleOutlined className="summary-icon" />
                      <span>Đã chọn {selectedTimeSlots.length} khung giờ</span>
                    </div>
                    <div className="summary-slots">
                      {selectedTimeSlots.map((slot) => (
                        <Tag key={slot} color="blue" className="summary-tag">
                          {slot}
                        </Tag>
                      ))}
                    </div>
                  </div>
                )}
              </div>

              <div className="modern-form-actions">
                <Button
                  size="large"
                  onClick={closeCreateBookingModal}
                  className="cancel-button"
                >
                  Hủy Bỏ
                </Button>
                <Button
                  type="primary"
                  size="large"
                  htmlType="submit"
                  loading={loading.creating}
                  icon={<PlusOutlined />}
                  className="submit-button"
                  disabled={selectedTimeSlots.length === 0}
                  style={{ marginTop: "2.5%" }}
                >
                  {loading.creating ? "Đang tạo đơn..." : "Tạo Đơn Đặt Sân"}
                </Button>
              </div>
            </Form>
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default BookingManagement;
