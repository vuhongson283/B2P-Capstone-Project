import React, { useState, useEffect, useCallback, useMemo, useRef } from "react";
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
    Typography
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
    WifiOutlined,
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
    getFacilityDetailsById
} from "../../services/apiService";
// ✅ THAY ĐỔI: Import signalRService trực tiếp thay vì useSignalR
import signalRService from "../../services/signalRService";

const { Option } = Select;
const { Text } = Typography;

// Constants
const USER_ID = 13;
const CUSTOMER_USER_ID = 16;
const CUSTOMER_EMAIL = "admin@courtowner.com";
const CUSTOMER_PHONE = "0000000000";

const BookingManagement = () => {
    // State for facilities and courts data
    const [facilities, setFacilities] = useState([]);
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
    const [selectedCategoryFilter, setSelectedCategoryFilter] = useState('all');
    const [isRealTimeConnected, setIsRealTimeConnected] = useState(false);

    // Modal states for notification detail
    const [notificationBookingDetail, setNotificationBookingDetail] = useState(null);
    const [isNotificationDetailVisible, setIsNotificationDetailVisible] = useState(false);

    // Loading states
    const [loading, setLoading] = useState({
        facilities: false,
        courts: false,
        timeSlots: false,
        bookings: false,
        customer: false,
        completing: false,
        modalCategories: false,
        creating: false,
    });

    // Modal states
    const [isCreateBookingModalVisible, setIsCreateBookingModalVisible] = useState(false);
    const [createBookingForm] = Form.useForm();

    // ✅ THAY ĐỔI: Lấy trực tiếp từ signalRService thay vì useSignalR
    const isConnected = signalRService.connected;
    const connectionState = signalRService.connectionState;
    const sendBookingUpdate = signalRService.sendBookingUpdate.bind(signalRService);

    // Utility functions
    const updateLoading = useCallback((key, value) => {
        setLoading(prev => ({ ...prev, [key]: value }));
    }, []);

    const formatTime = useCallback((time) => {
        return time ? time.substring(0, 5) : '';
    }, []);

    const getBookingStatusFromString = useCallback((status) => {
        const statusLower = status?.toLowerCase();
        switch (statusLower) {
            case 'paid':
                return 'paid';
            case 'completed':
                return 'completed';
            case 'cancelled':
            case 'canceled':
                return 'cancelled';
            case 'active':
                return 'confirmed';
            default:
                return 'confirmed';
        }
    }, []);

    const getStatusDisplayText = useCallback((status, originalStatus) => {
        switch (status) {
            case 'paid':
                return 'Đã thanh toán cọc';
            case 'completed':
                return 'Đã hoàn thành';
            case 'cancelled':
                return 'Đã hủy';
            case 'confirmed':
                return originalStatus === 'Active' ? 'Đã xác nhận' : 'Đã xác nhận';
            default:
                return originalStatus || 'Đã xác nhận';
        }
    }, []);

    const getSlotDisplayText = useCallback((status) => {
        switch (status) {
            case 'available':
                return 'Còn trống';
            case 'paid':
                return 'Đã Đặt';
            case 'completed':
                return 'Đã hoàn thành';
            case 'cancelled':
                return 'Đã hủy';
            default:
                return 'Đã xác nhận';
        }
    }, []);

    const getTimeSlotId = useCallback((timeSlotString) => {
        const foundSlot = rawTimeSlots.find(slot => {
            const startTime = slot.startTime || slot.start || slot.timeStart;
            const endTime = slot.endTime || slot.end || slot.timeEnd;
            if (!startTime || !endTime) return false;
            const formattedSlot = `${formatTime(startTime)} - ${formatTime(endTime)}`;
            return formattedSlot === timeSlotString;
        });
        return foundSlot?.timeSlotId || foundSlot?.id || 1;
    }, [rawTimeSlots, formatTime]);

    const getBookingKey = useCallback((courtId, date, timeSlot) => {
        return `${courtId}_${date.format('YYYY-MM-DD')}_${timeSlot}`;
    }, []);

    // Process booking data
    const processBookingData = useCallback((bookingsData) => {
        const processedBookings = {};

        bookingsData.forEach((booking) => {
            try {
                const bookingDate = dayjs(booking.checkInDate);
                if (!bookingDate.isValid()) return;

                const isDateMatch = bookingDate.format('YYYY-MM-DD') === selectedDate.format('YYYY-MM-DD');
                if (!isDateMatch || !booking.slots?.length) return;

                booking.slots.forEach((slot) => {
                    try {
                        const startTime = slot.startTime || slot.timeStart || slot.fromTime;
                        const endTime = slot.endTime || slot.timeEnd || slot.toTime;
                        const courtId = slot.courtId || slot.court_id || slot.id;

                        if (!startTime || !endTime || !courtId) return;

                        const timeSlot = `${formatTime(startTime)} - ${formatTime(endTime)}`;
                        const bookingKey = getBookingKey(courtId, bookingDate, timeSlot);

                        const status = getBookingStatusFromString(booking.status);
                        const processedBooking = createProcessedBooking(booking, slot, courtId, timeSlot, bookingDate, status);

                        if (processedBooking) {
                            processedBookings[bookingKey] = processedBooking;
                        }
                    } catch (error) {
                        console.error('Error processing booking slot:', error);
                    }
                });
            } catch (error) {
                console.error('Error processing booking:', error);
            }
        });

        setBookingData(processedBookings);
    }, [selectedDate, formatTime, getBookingKey, getBookingStatusFromString]);

    const createProcessedBooking = useCallback((
        booking,
        slot,
        courtId,
        timeSlot,
        bookingDate,
        status
    ) => {
        try {
            let paymentStatus = 'pending';
            let mappedStatusId = 1;

            switch (status) {
                case 'paid':
                    paymentStatus = 'deposit';
                    mappedStatusId = 7;
                    break;
                case 'completed':
                    paymentStatus = 'paid';
                    mappedStatusId = 10;
                    break;
                case 'cancelled':
                    paymentStatus = 'cancelled';
                    mappedStatusId = 9;
                    break;
                default:
                    paymentStatus = 'pending';
                    mappedStatusId = 1;
            }

            const rawBookingTime = booking.createDate || booking.createdAt || booking.checkInDate;
            let bookingTime = 'Không có thông tin';

            if (rawBookingTime && rawBookingTime !== 'N/A' && rawBookingTime !== '0001-01-01T00:00:00') {
                const parsedTime = dayjs(rawBookingTime);
                if (parsedTime.isValid()) {
                    bookingTime = parsedTime.format('DD/MM/YYYY HH:mm:ss');
                }
            }

            return {
                id: booking.bookingId || booking.id,
                userId: booking.userId,
                courtId,
                courtName: slot.courtName || slot.court_name || `Court ${courtId}`,
                timeSlot,
                date: bookingDate.format('DD/MM/YYYY'),
                price: booking.totalPrice || booking.price || 0,
                status,
                paymentStatus,
                bookingTime,
                checkInDate: booking.checkInDate,
                statusId: mappedStatusId,
                originalStatus: booking.status,
                rawCreateDate: rawBookingTime,
                customerName: 'Đang tải...',
                customerPhone: 'Đang tải...',
                customerEmail: 'Đang tải...'
            };
        } catch (error) {
            console.error('Error creating processed booking:', error);
            return null;
        }
    }, []);

    // API calls with error handling
    const loadBookings = useCallback(async (facilityId) => {
        try {
            updateLoading('bookings', true);
            const response = await getBookingsByFacilityId(facilityId, 1, 1000);

            const bookingsData = response.data?.items || [];
            setBookings(bookingsData);
            processBookingData(bookingsData);
        } catch (error) {
            console.error('Error loading bookings:', error);
            message.error('Không thể tải dữ liệu đặt sân');
            setBookings([]);
            setBookingData({});
        } finally {
            updateLoading('bookings', false);
        }
    }, [updateLoading, processBookingData]);

    // ✅ Quản lý facility groups trực tiếp
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

    // ✅ Event listener cho global notifications
    useEffect(() => {
        const handleOpenNotificationDetail = (event) => {
            console.log('🔔 Received notification detail event:', event.detail);
            setNotificationBookingDetail(event.detail);
            setIsNotificationDetailVisible(true);
        };

        window.addEventListener('openNotificationDetail', handleOpenNotificationDetail);

        return () => {
            window.removeEventListener('openNotificationDetail', handleOpenNotificationDetail);
        };
    }, []);

    // ✅ CÁCH 1: Listen for global SignalR events to update UI slots
    useEffect(() => {
        // Global booking created handler for UI updates
        const handleGlobalBookingCreated = (notification) => {
            console.log('🔔 LOCAL UI: Global booking created received!', notification);

            // Cập nhật UI slots
            if (notification.facilityId === selectedFacility) {
                const notificationDate = dayjs(notification.date, 'DD/MM/YYYY');
                const currentDate = selectedDate;

                if (notificationDate.format('YYYY-MM-DD') === currentDate.format('YYYY-MM-DD')) {
                    console.log('📱 LOCAL UI: Updating slot status directly...');

                    const timeSlot = notification.timeSlot;
                    const courtId = notification.courtId;

                    if (courtId && timeSlot) {
                        const bookingKey = `${courtId}_${currentDate.format('YYYY-MM-DD')}_${timeSlot}`;

                        const newBooking = {
                            id: notification.bookingId || Date.now(),
                            userId: CUSTOMER_USER_ID,
                            courtId: courtId,
                            courtName: notification.courtName || 'Sân thể thao',
                            timeSlot: timeSlot,
                            date: currentDate.format('DD/MM/YYYY'),
                            price: notification.totalAmount || 0,
                            status: 'paid',
                            paymentStatus: 'deposit',
                            bookingTime: dayjs().format('DD/MM/YYYY HH:mm:ss'),
                            checkInDate: currentDate.format('YYYY-MM-DD'),
                            statusId: 7,
                            originalStatus: 'Paid',
                            customerName: notification.customerName || 'Admin',
                            customerPhone: notification.customerPhone || 'N/A',
                            customerEmail: notification.customerEmail || 'N/A'
                        };

                        setBookingData(prev => ({
                            ...prev,
                            [bookingKey]: newBooking
                        }));

                        console.log(`✅ LOCAL UI: Slot ${timeSlot} updated to PAID status`);
                    }

                    // Backup reload sau 2 giây
                    setTimeout(() => {
                        if (selectedFacility) {
                            console.log('🔄 LOCAL UI: Backup reload booking data...');
                            loadBookings(selectedFacility);
                        }
                    }, 2000);
                }
            }
        };

        // Global booking updated handler for UI updates
        const handleGlobalBookingUpdated = (notification) => {
            console.log('🔔 LOCAL UI: Global booking updated received!', notification);

            // Cập nhật UI slots
            if (notification.facilityId === selectedFacility) {
                const notificationDate = dayjs(notification.date, 'DD/MM/YYYY');
                const currentDate = selectedDate;

                if (notificationDate.format('YYYY-MM-DD') === currentDate.format('YYYY-MM-DD')) {
                    console.log('📱 LOCAL UI: Updating slot status to:', notification.status);

                    const timeSlot = notification.timeSlot;
                    const courtId = notification.courtId;

                    if (courtId && timeSlot) {
                        const bookingKey = `${courtId}_${currentDate.format('YYYY-MM-DD')}_${timeSlot}`;

                        setBookingData(prev => {
                            if (prev[bookingKey]) {
                                const updatedBooking = {
                                    ...prev[bookingKey],
                                    status: getBookingStatusFromString(notification.status),
                                    originalStatus: notification.status,
                                    paymentStatus: notification.status === 'completed' ? 'paid' : prev[bookingKey].paymentStatus,
                                    statusId: notification.status === 'completed' ? 10 : prev[bookingKey].statusId
                                };

                                console.log(`✅ LOCAL UI: Slot ${timeSlot} updated to ${notification.status.toUpperCase()} status`);

                                return {
                                    ...prev,
                                    [bookingKey]: updatedBooking
                                };
                            }
                            return prev;
                        });

                        if (selectedBooking && selectedBooking.id.toString() === notification.bookingId.toString()) {
                            setSelectedBooking(prev => prev ? {
                                ...prev,
                                status: getBookingStatusFromString(notification.status),
                                originalStatus: notification.status,
                                paymentStatus: notification.status === 'completed' ? 'paid' : prev.paymentStatus,
                                statusId: notification.status === 'completed' ? 10 : prev.statusId
                            } : null);
                        }
                    }
                }
            }
        };

        // ✅ TAP INTO EXISTING SIGNALR CONNECTION EVENTS
        if (signalRService.connection) {
            // Add listeners directly to SignalR connection for UI updates
            signalRService.connection.on('BookingCreated', handleGlobalBookingCreated);
            signalRService.connection.on('BookingUpdated', handleGlobalBookingUpdated);
            signalRService.connection.on('BookingCompleted', handleGlobalBookingUpdated);
            signalRService.connection.on('BookingCancelled', handleGlobalBookingUpdated);

            console.log('✅ LOCAL UI: Added SignalR event listeners for slot updates');
        }

        return () => {
            // Cleanup
            if (signalRService.connection) {
                signalRService.connection.off('BookingCreated', handleGlobalBookingCreated);
                signalRService.connection.off('BookingUpdated', handleGlobalBookingUpdated);
                signalRService.connection.off('BookingCompleted', handleGlobalBookingUpdated);
                signalRService.connection.off('BookingCancelled', handleGlobalBookingUpdated);

                console.log('🧹 LOCAL UI: Removed SignalR event listeners');
            }
        };
    }, [selectedFacility, selectedDate, selectedBooking, getBookingStatusFromString, loadBookings]);

    // ✅ Monitor connection state  
    useEffect(() => {
        const updateConnectionState = () => {
            const currentConnected = signalRService.connected;
            if (currentConnected !== isRealTimeConnected) {
                setIsRealTimeConnected(currentConnected);
                console.log(`🔗 LOCAL: Connection state updated: ${currentConnected}`);
            }
        };

        // Check immediately
        updateConnectionState();

        // Check periodically
        const interval = setInterval(updateConnectionState, 2000);

        return () => clearInterval(interval);
    }, [isRealTimeConnected]);

    // API calls khác
    const loadFacilities = useCallback(async () => {
        try {
            updateLoading('facilities', true);
            const response = await getFacilitiesByCourtOwnerId(USER_ID, "", null, 1, 100);

            const facilitiesData = response.data?.items || [];
            setFacilities(facilitiesData);

            if (facilitiesData.length > 0) {
                setSelectedFacility(facilitiesData[0].facilityId);
            }
        } catch (error) {
            console.error('Error loading facilities:', error);
            message.error('Không thể tải danh sách cơ sở');
            setFacilities([]);
        } finally {
            updateLoading('facilities', false);
        }
    }, [updateLoading]);

    const loadCourts = useCallback(async (facilityId) => {
        try {
            updateLoading('courts', true);
            const response = await getAllCourts({
                pageNumber: 1,
                pageSize: 100,
                facilityId,
                search: "",
                status: null,
                categoryId: null
            });

            const courtsData = response.data?.data || response.data?.items || response.data || [];
            setCourts(courtsData);
        } catch (error) {
            console.error('Error loading courts:', error);
            message.error('Không thể tải danh sách sân');
            setCourts([]);
        } finally {
            updateLoading('courts', false);
        }
    }, [updateLoading]);

    const loadTimeSlots = useCallback(async (facilityId) => {
        try {
            updateLoading('timeSlots', true);
            const response = await getTimeSlotsByFacilityId(facilityId);

            const timeSlotsData = response.data?.data || response.data?.items || response.data?.result || response.data || [];
            setRawTimeSlots(timeSlotsData);

            const formattedTimeSlots = timeSlotsData.map((slot) => {
                const startTime = slot.startTime || slot.start || slot.timeStart;
                const endTime = slot.endTime || slot.end || slot.timeEnd;
                return `${formatTime(startTime)} - ${formatTime(endTime)}`;
            });
            setTimeSlots(formattedTimeSlots);
        } catch (error) {
            console.error('Error loading time slots:', error);
            message.error('Không thể tải khung giờ');
            setTimeSlots([]);
            setRawTimeSlots([]);
        } finally {
            updateLoading('timeSlots', false);
        }
    }, [updateLoading, formatTime]);

    const loadModalCategories = useCallback(async (facilityId) => {
        try {
            updateLoading('modalCategories', true);
            const response = await getFacilityDetailsById(facilityId);

            const categories = response?.data?.categories || [];
            setModalCategories(categories);
        } catch (error) {
            console.error('Error loading categories:', error);
            setModalCategories([]);
        } finally {
            updateLoading('modalCategories', false);
        }
    }, [updateLoading]);

    const loadCustomerDetails = useCallback(async (userId) => {
        try {
            const response = await getAccountById(userId);
            const customerData = response.data?.data || response.data?.user || response.data;

            if (customerData) {
                return {
                    customerName: customerData.fullName || customerData.name || customerData.userName || 'N/A',
                    customerPhone: customerData.phoneNumber || customerData.phone || 'N/A',
                    customerEmail: customerData.email || 'N/A',
                    customerAvatar: customerData.avatar || customerData.profilePicture || null
                };
            }
            return null;
        } catch (error) {
            console.error('Error loading customer details:', error);
            return null;
        }
    }, []);

    // Event handlers
    const handleFacilityChange = useCallback((value) => {
        setSelectedFacility(value);
        setSelectedCategoryFilter('all');
    }, []);

    const handleDateChange = useCallback((date) => {
        if (date) {
            setSelectedDate(date);
        }
    }, []);

    const handleTimeSlotToggle = useCallback((timeSlot) => {
        setSelectedTimeSlots(prev =>
            prev.includes(timeSlot)
                ? prev.filter(slot => slot !== timeSlot)
                : [...prev, timeSlot]
        );
    }, []);

    const handleSlotClick = useCallback(async (court, timeSlot) => {
        const bookingKey = getBookingKey(court.courtId || court.id, selectedDate, timeSlot);
        const booking = bookingData[bookingKey];

        if (!booking) return;

        updateLoading('customer', true);
        setIsModalVisible(true);
        setSelectedBooking({
            ...booking,
            timeSlot,
            date: selectedDate.format('DD/MM/YYYY')
        });

        if (booking.userId) {
            try {
                const customerDetails = await loadCustomerDetails(booking.userId);
                if (customerDetails) {
                    setSelectedBooking(prev => prev ? { ...prev, ...customerDetails } : null);
                } else {
                    setSelectedBooking(prev => prev ? {
                        ...prev,
                        customerName: 'Không tìm thấy thông tin',
                        customerPhone: 'Không tìm thấy thông tin',
                        customerEmail: 'Không tìm thấy thông tin'
                    } : null);
                }
            } catch (error) {
                setSelectedBooking(prev => prev ? {
                    ...prev,
                    customerName: 'Lỗi tải thông tin',
                    customerPhone: 'Lỗi tải thông tin',
                    customerEmail: 'Lỗi tải thông tin'
                } : null);
            }
        }
        updateLoading('customer', false);
    }, [bookingData, selectedDate, getBookingKey, loadCustomerDetails, updateLoading]);

    const openModal = useCallback(async (bookingId) => {
        try {
            const booking = Object.values(bookingData).find(b => b.id === bookingId);
            if (booking) {
                await handleSlotClick({ courtId: booking.courtId }, booking.timeSlot);
            }
        } catch (error) {
            console.error('Error opening modal:', error);
        }
    }, [bookingData, handleSlotClick]);

    const handleCompleteBooking = useCallback(async () => {
        if (!selectedBooking?.id) {
            message.error('Không tìm thấy thông tin đơn đặt sân');
            return;
        }

        try {
            updateLoading('completing', true);
            const response = await completeBooking(selectedBooking.id);

            const isSuccess = response.status === 200 || response.status === 201 ||
                response.data?.success === true || !response.data?.error;

            if (isSuccess) {
                message.success('Hoàn thành đơn đặt sân thành công!');

                const newStatus = 'completed';
                setSelectedBooking(prev => prev ? {
                    ...prev,
                    status: newStatus,
                    statusId: 10,
                    paymentStatus: 'paid',
                    originalStatus: 'Completed'
                } : null);

                const bookingKey = `${selectedBooking.courtId}_${selectedDate.format('YYYY-MM-DD')}_${selectedBooking.timeSlot}`;
                setBookingData(prev => ({
                    ...prev,
                    [bookingKey]: {
                        ...prev[bookingKey],
                        status: newStatus,
                        statusId: 10,
                        paymentStatus: 'paid',
                        originalStatus: 'Completed'
                    }
                }));

                // Send SignalR notification
                if (isConnected) {
                    const notification = {
                        bookingId: selectedBooking.id,
                        facilityId: selectedFacility,
                        courtId: selectedBooking.courtId,
                        timeSlot: selectedBooking.timeSlot,
                        date: selectedDate.format('DD/MM/YYYY'),
                        checkInTime: selectedBooking.timeSlot.split(' - ')[0],
                        status: 'completed',
                        action: 'completed',
                        message: `Đơn đặt sân ${selectedBooking.courtName} đã được hoàn thành`,
                        courtName: selectedBooking.courtName,
                        customerName: selectedBooking.customerName
                    };
                    await sendBookingUpdate(notification);
                }

                setTimeout(() => closeModal(), 1000);
                setTimeout(() => selectedFacility && loadBookings(selectedFacility), 1500);
            } else {
                const errorMessage = response.data?.message || response.data?.error || 'Không thể hoàn thành đơn đặt sân';
                message.error(errorMessage);
            }
        } catch (error) {
            const errorMessage = error.response?.data?.message || 'Có lỗi xảy ra. Vui lòng thử lại.';
            message.error(errorMessage);
        } finally {
            updateLoading('completing', false);
        }
    }, [selectedBooking, selectedDate, selectedFacility, loadBookings, updateLoading, isConnected, sendBookingUpdate]);

    const handleCreateBooking = useCallback(async (values) => {
        if (selectedTimeSlots.length === 0) {
            message.error('Vui lòng chọn ít nhất một khung giờ');
            return;
        }

        try {
            updateLoading('creating', true);
            const timeSlotIds = selectedTimeSlots.map(timeSlot => getTimeSlotId(timeSlot));
            const checkInDate = selectedDate.format('YYYY-MM-DDTHH:mm:ss.SSS') + 'Z';

            const bookingRequestData = {
                userId: CUSTOMER_USER_ID,
                email: CUSTOMER_EMAIL,
                phone: CUSTOMER_PHONE,
                checkInDate,
                timeSlotIds,
                facilityId: selectedFacility,
                categoryId: values.categoryId
            };

            const response = await createBookingForCO(bookingRequestData);
            const isSuccess = response.status === 200 || response.status === 201 ||
                response.data?.success === true || !response.data?.error;

            if (isSuccess) {
                message.success('Tạo đơn đặt sân thành công!');

                // 🔔 FIX: LẤY THÔNG TIN THỰC TỪ RESPONSE
                const responseData = response.data?.data;
                if (responseData?.slots?.length > 0 && isConnected) {
                    // Gửi notification cho từng slot được tạo
                    for (const slot of responseData.slots) {
                        const notification = {
                            bookingId: responseData.bookingId, // ✅ 1125 từ response
                            facilityId: selectedFacility,
                            courtId: slot.courtId, // ✅ 19 từ response
                            timeSlot: `${formatTime(slot.startTime)} - ${formatTime(slot.endTime)}`, // ✅ "08:00 - 10:00"
                            date: selectedDate.format('DD/MM/YYYY'),
                            checkInTime: formatTime(slot.startTime), // ✅ "08:00"
                            status: 'paid',
                            action: 'created',
                            message: `Đơn đặt sân mới được tạo cho ${slot.courtName}`,
                            courtName: slot.courtName, // ✅ "Sân 5 ngu?i - CMT8 - 2"
                            customerName: responseData.user?.email?.split('@')[0] || 'Admin', // ✅ "trong1m"
                            customerEmail: responseData.user?.email || CUSTOMER_EMAIL, // ✅ "trong1m@gmail.com"
                            customerPhone: responseData.user?.phone || CUSTOMER_PHONE, // ✅ "0944126356"
                            totalAmount: 0, // Không có trong response, để 0
                            timestamp: new Date().toISOString()
                        };

                        console.log('🔔 Sending notification with real data:', notification);
                        await sendBookingUpdate(notification);
                    }
                }

                setTimeout(() => closeCreateBookingModal(), 1000);
                setTimeout(() => selectedFacility && loadBookings(selectedFacility), 1500);
            } else {
                const errorMessage = response.data?.message || response.data?.error || 'Không thể tạo đơn đặt sân';
                message.error(errorMessage);
            }
        } catch (error) {
            const errorMessage = error.response?.data?.message || 'Có lỗi xảy ra. Vui lòng thử lại.';
            message.error(errorMessage);
        } finally {
            updateLoading('creating', false);
        }
    }, [selectedTimeSlots, selectedDate, selectedFacility, getTimeSlotId, loadBookings, updateLoading, isConnected, sendBookingUpdate, formatTime]);

    // Modal handlers
    const openCreateBookingModal = useCallback(async () => {
        if (!selectedFacility) {
            message.warning('Vui lòng chọn cơ sở trước');
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
        updateLoading('creating', false);
    }, [createBookingForm, updateLoading]);

    const closeModal = useCallback(() => {
        setIsModalVisible(false);
        setSelectedBooking(null);
        updateLoading('customer', false);
        updateLoading('completing', false);
    }, [updateLoading]);

    // Memoized computations
    const getSlotStatus = useCallback((court, timeSlot) => {
        const bookingKey = getBookingKey(court.courtId || court.id, selectedDate, timeSlot);
        const booking = bookingData[bookingKey];
        return booking ? booking.status : 'available';
    }, [bookingData, selectedDate, getBookingKey]);

    const filteredCourts = useMemo(() => {
        if (selectedCategoryFilter === 'all') return courts;

        const selectedCategory = modalCategories.find(
            cat => String(cat.categoryId) === String(selectedCategoryFilter)
        );

        if (!selectedCategory) return [];

        return courts.filter(court => court.categoryName === selectedCategory.categoryName);
    }, [courts, selectedCategoryFilter, modalCategories]);

    const bookingStatusCounts = useMemo(() => {
        const totalSlots = courts.length * timeSlots.length;
        const bookingValues = Object.values(bookingData);

        return {
            available: totalSlots - bookingValues.length,
            paid: bookingValues.filter(b => b.status === 'paid').length,
            completed: bookingValues.filter(b => b.status === 'completed').length,
            cancelled: bookingValues.filter(b => b.status === 'cancelled').length,
        };
    }, [courts.length, timeSlots.length, bookingData]);

    // Effects
    useEffect(() => {
        loadFacilities();
    }, [loadFacilities]);

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
    }, [selectedFacility, loadCourts, loadTimeSlots, loadBookings, loadModalCategories]);

    useEffect(() => {
        if (selectedFacility) {
            loadBookings(selectedFacility);
        }
    }, [selectedDate, selectedFacility, loadBookings]);

    useEffect(() => {
        if (isCreateBookingModalVisible) {
            createBookingForm.setFieldsValue({ categoryId: undefined });
        }
        // eslint-disable-next-line
    }, [modalCategories, isCreateBookingModalVisible]);

    // Render helpers
    const renderTimeSlotGrid = useCallback(() => {
        if (loading.timeSlots) {
            return (
                <div className="loading-slots">
                    <Spin size="large" />
                    <span>Đang tải khung giờ...</span>
                </div>
            );
        }

        if (timeSlots.length === 0) {
            return (
                <div className="empty-slots">
                    <ClockCircleOutlined className="empty-icon" />
                    <span>Không có khung giờ nào</span>
                </div>
            );
        }

        return timeSlots.map(slot => (
            <div
                key={slot}
                className={`time-slot-card ${selectedTimeSlots.includes(slot) ? 'selected' : ''}`}
                onClick={() => handleTimeSlotToggle(slot)}
            >
                <div className="slot-time">
                    <ClockCircleOutlined className="slot-icon" />
                    <span>{slot}</span>
                </div>
                <div className="slot-indicator">
                    {selectedTimeSlots.includes(slot) && (
                        <CheckCircleOutlined className="check-icon" />
                    )}
                </div>
            </div>
        ));
    }, [timeSlots, selectedTimeSlots, loading.timeSlots, handleTimeSlotToggle]);

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
                            {selectedCategoryFilter === 'all'
                                ? (selectedFacility ? 'Không có sân nào' : 'Vui lòng chọn cơ sở')
                                : 'Không có sân nào thuộc loại này'}
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
                {timeSlots.map(slot => {
                    const status = getSlotStatus(court, slot);
                    return (
                        <div
                            key={`${court.courtId || court.id}-${slot}`}
                            className={`slot-cell ${status}`}
                            onClick={() => handleSlotClick(court, slot)}
                            title={`${court.courtName || court.name} - ${slot}`}
                        >
                            {getSlotDisplayText(status)}
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
        getSlotDisplayText
    ]);

    return (
        <div className="booking-management">
            <div className="main-container">
                {/* Header with connection status */}
                <div className="header">
                    <div className="header-content">
                        <HomeOutlined className="header-icon" />
                        <div className="header-text">
                            <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                                <h1 style={{ color: "white", margin: 0 }}>Quản Lý Đơn Đặt Sân</h1>
                            </div>
                            <div className="subtitle">Theo dõi và quản lý lịch đặt sân hiệu quả</div>
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
                                notFoundContent={loading.facilities ? <Spin size="small" /> : "Không có dữ liệu"}
                            >
                                {facilities.map(facility => (
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
                                Tạo Đơn
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
                        <span>Đã Đặt ({bookingStatusCounts.paid} slots)</span>
                    </div>
                    <div className="status-item">
                        <div className="status-dot completed"></div>
                        <span>Đã hoàn thành ({bookingStatusCounts.completed} slots)</span>
                    </div>
                    <div className="status-item">
                        <div className="status-dot cancelled"></div>
                        <span>Đã hủy ({bookingStatusCounts.cancelled} slots)</span>
                    </div>

                    <div className="category-filter" style={{ display: 'flex', alignItems: 'center', gap: 12, marginLeft: 32 }}>
                        <span style={{ fontWeight: 500 }}>Loại sân:</span>
                        <select
                            style={{
                                minWidth: 160,
                                padding: '6px 12px',
                                borderRadius: 6,
                                border: '1px solid #d9d9d9',
                                fontSize: 15,
                                background: '#fff'
                            }}
                            value={selectedCategoryFilter}
                            onChange={e => setSelectedCategoryFilter(e.target.value)}
                            disabled={loading.modalCategories}
                        >
                            <option value="all">Tất cả</option>
                            {modalCategories.map(category => (
                                <option key={category.categoryId} value={category.categoryId}>
                                    {category.categoryName}
                                </option>
                            ))}
                        </select>
                        {loading.modalCategories && <Spin size="small" style={{ marginLeft: 8 }} />}
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
                                        timeSlots.map(slot => (
                                            <div key={slot} className="header-cell time-slot">
                                                {slot}
                                            </div>
                                        ))
                                    )}
                                </div>
                            </div>

                            {/* Table Body */}
                            <div className="table-body">
                                {renderBookingTable()}
                            </div>
                        </div>
                    </div>
                </div>
            </div>

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
                                {getStatusDisplayText(selectedBooking.status, selectedBooking.originalStatus)}
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
                                            {selectedBooking.price.toLocaleString('vi-VN')} VNĐ
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>

                        {/* Customer Info Section */}
                        <div className="customer-info">
                            <h3>Thông tin khách hàng</h3>
                            <div className="customer-details">
                                {loading.customer ? (
                                    <div style={{ display: 'flex', alignItems: 'center', gap: '16px', padding: '20px' }}>
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

                        {/* Payment status display */}
                        <div className="payment-status">
                            <div className="payment-info">
                                {selectedBooking.paymentStatus === 'deposit' ? (
                                    <>
                                        <CheckCircleOutlined className="payment-icon deposit" />
                                        <span className="payment-text deposit">Đã thanh toán cọc</span>
                                    </>
                                ) : selectedBooking.paymentStatus === 'paid' ? (
                                    <>
                                        <CheckCircleOutlined className="payment-icon paid" />
                                        <span className="payment-text paid">Đã thanh toán</span>
                                    </>
                                ) : selectedBooking.paymentStatus === 'cancelled' ? (
                                    <>
                                        <CloseCircleOutlined className="payment-icon cancelled" />
                                        <span className="payment-text cancelled">Đã hủy</span>
                                    </>
                                ) : (
                                    <>
                                        <CloseCircleOutlined className="payment-icon pending" />
                                        <span className="payment-text pending">Chưa thanh toán</span>
                                    </>
                                )}
                            </div>
                            <div className="booking-time">
                                Đặt lúc: {selectedBooking.bookingTime}
                            </div>
                        </div>

                        {/* Modal Actions */}
                        <div className="modal-actions">
                            <Button onClick={closeModal}>Đóng</Button>
                            {selectedBooking.status === 'paid' ? (
                                <Button
                                    type="primary"
                                    className="action-button"
                                    onClick={handleCompleteBooking}
                                    loading={loading.completing}
                                    icon={<CheckCircleOutlined />}
                                >
                                    {loading.completing ? 'Đang hoàn thành...' : 'Hoàn Thành Đơn'}
                                </Button>
                            ) : selectedBooking.status === 'completed' ? (
                                <Button
                                    type="primary"
                                    className="action-button completed"
                                    disabled
                                    icon={<CheckCircleOutlined />}
                                >
                                    Đã Hoàn Thành
                                </Button>
                            ) : selectedBooking.status === 'cancelled' ? (
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
                    <div style={{
                        display: 'flex',
                        alignItems: 'center',
                        color: '#52c41a',
                        fontSize: '18px',
                        fontWeight: 'bold'
                    }}>
                        <CalendarOutlined style={{ marginRight: '12px', fontSize: '20px' }} />
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
                                // Mở modal booking detail chính
                                setIsNotificationDetailVisible(false);
                                await openModal(notificationBookingDetail.bookingId);
                                setNotificationBookingDetail(null);
                            }}
                        >
                            Xem chi tiết đầy đủ
                        </Button>
                    )
                ]}
                width={500}
                centered
                styles={{ body: { padding: '24px' } }}
            >
                {notificationBookingDetail && (
                    <div className="notification-booking-detail">
                        <Row gutter={[16, 16]}>
                            <Col span={24}>
                                <Card size="small" style={{ backgroundColor: '#f6ffed', border: '1px solid #b7eb8f' }}>
                                    <Statistic
                                        title="Trạng thái"
                                        value="Đã thanh toán"
                                        valueStyle={{ color: '#52c41a', fontSize: '16px' }}
                                        prefix={<CheckCircleOutlined />}
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
                                    <Text>{notificationBookingDetail.customerPhone || 'Chưa có'}</Text>
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

                            {notificationBookingDetail.totalAmount && (
                                <Col span={24}>
                                    <Card size="small" style={{ backgroundColor: '#fff7e6', border: '1px solid #ffd591' }}>
                                        <Statistic
                                            title="Tổng tiền"
                                            value={notificationBookingDetail.totalAmount}
                                            suffix="VND"
                                            valueStyle={{ color: '#fa8c16', fontSize: '18px', fontWeight: 'bold' }}
                                            formatter={(value) => value.toLocaleString()}
                                        />
                                    </Card>
                                </Col>
                            )}

                            <Col span={24}>
                                <div className="detail-item">
                                    <Text strong>Thời gian tạo:</Text>
                                    <br />
                                    <Text>{dayjs(notificationBookingDetail.timestamp).format('DD/MM/YYYY HH:mm:ss')}</Text>
                                </div>
                            </Col>
                        </Row>
                    </div>
                )}
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
                                    rules={[{ required: true, message: 'Vui lòng chọn loại sân' }]}
                                    className="form-item-half"
                                >
                                    <select
                                        style={{ width: '100%', padding: 8, borderRadius: 4 }}
                                        value={createBookingForm.getFieldValue('categoryId') || ''}
                                        onChange={e => createBookingForm.setFieldsValue({ categoryId: e.target.value })}
                                    >
                                        <option value="">Chọn loại sân</option>
                                        {modalCategories.map(category => (
                                            <option key={category.categoryId} value={category.categoryId}>
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
                                <div className="time-slots-grid">
                                    {renderTimeSlotGrid()}
                                </div>
                                {selectedTimeSlots.length > 0 && (
                                    <div className="selected-summary">
                                        <div className="summary-header">
                                            <CheckCircleOutlined className="summary-icon" />
                                            <span>Đã chọn {selectedTimeSlots.length} khung giờ</span>
                                        </div>
                                        <div className="summary-slots">
                                            {selectedTimeSlots.map(slot => (
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
                                >
                                    {loading.creating ? 'Đang tạo đơn...' : 'Tạo Đơn Đặt Sân'}
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