import React, { useState, useEffect } from "react";
import "./BookingManagement.scss";
import { Select, DatePicker, Modal, Button, Tag, Avatar, Spin, message, Form, notification } from "antd";
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

const { Option } = Select;

const BookingManagement = () => {
    // State for facilities and courts data
    const [facilities, setFacilities] = useState([]);
    const [courts, setCourts] = useState([]);
    const [timeSlots, setTimeSlots] = useState([]);
    const [rawTimeSlots, setRawTimeSlots] = useState([]); // Store original timeSlot data with IDs
    const [selectedFacility, setSelectedFacility] = useState(null);
    const [selectedDate, setSelectedDate] = useState(dayjs());
    const [bookingData, setBookingData] = useState({});
    const [bookings, setBookings] = useState([]);
    const [isModalVisible, setIsModalVisible] = useState(false);
    const [selectedBooking, setSelectedBooking] = useState(null);
    const [loading, setLoading] = useState(false);
    const [facilitiesLoading, setFacilitiesLoading] = useState(false);
    const [courtsLoading, setCourtsLoading] = useState(false);
    const [timeSlotsLoading, setTimeSlotsLoading] = useState(false);
    const [bookingsLoading, setBookingsLoading] = useState(false);
    const [customerLoading, setCustomerLoading] = useState(false);
    const [completingBooking, setCompletingBooking] = useState(false);

    // State for create booking modal
    const [isCreateBookingModalVisible, setIsCreateBookingModalVisible] = useState(false);
    const [modalCategories, setModalCategories] = useState([]);
    const [modalCategoriesLoading, setModalCategoriesLoading] = useState(false);
    const [selectedTimeSlots, setSelectedTimeSlots] = useState([]);
    const [creatingBooking, setCreatingBooking] = useState(false);
    const [createBookingForm] = Form.useForm();

    // State for category filter outside table
    const [selectedCategoryFilter, setSelectedCategoryFilter] = useState('all');

    // Fixed user ID
    const userId = 13;

    // HELPER FUNCTIONS FOR STATUS MAPPING
    const getBookingStatusFromString = (status) => {
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
    };

    const getStatusDisplayText = (status, originalStatus) => {
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
    };

    const getPaymentStatusFromId = (statusId) => {
        switch (statusId) {
            case 7:
            case '7':
            case 10:
            case '10':
                return 'paid';
            default:
                return 'pending';
        }
    };

    // Get timeSlot ID from timeSlot string
    const getTimeSlotId = (timeSlotString) => {
        const foundSlot = rawTimeSlots.find(slot => {
            const startTime = slot.startTime || slot.start || slot.timeStart;
            const endTime = slot.endTime || slot.end || slot.timeEnd;
            if (!startTime || !endTime) return false;
            const formatTime = (time) => (time ? time.substring(0, 5) : '');
            const formattedSlot = `${formatTime(startTime)} - ${formatTime(endTime)}`;
            return formattedSlot === timeSlotString;
        });
        if (!foundSlot) return 1; // fallback
        return foundSlot.timeSlotId || foundSlot.id;
    };

    // Load categories from facility details API
    const loadModalCategories = async (facilityId) => {
        try {
            setModalCategoriesLoading(true);
            const response = await getFacilityDetailsById(facilityId);
            if (response && response.success && response.data) {
                const facilityData = response.data;
                if (facilityData.categories && Array.isArray(facilityData.categories) && facilityData.categories.length > 0) {
                    setModalCategories(facilityData.categories);
                } else {
                    setModalCategories([]);
                }
            } else {
                setModalCategories([]);
            }
        } catch (error) {
            setModalCategories([]);
        } finally {
            setModalCategoriesLoading(false);
        }
    };

    // Handle time slot selection
    const handleTimeSlotToggle = (timeSlot) => {
        setSelectedTimeSlots(prev => {
            if (prev.includes(timeSlot)) {
                return prev.filter(slot => slot !== timeSlot);
            } else {
                return [...prev, timeSlot];
            }
        });
    };

    // Check if time slot is selected
    const isTimeSlotSelected = (timeSlot) => {
        return selectedTimeSlots.includes(timeSlot);
    };

    // Load facilities when component mounts
    useEffect(() => {
        loadFacilities();
    }, []);

    // Load courts, time slots, and bookings when facility is selected
    useEffect(() => {
        if (selectedFacility) {
            loadCourts(selectedFacility);
            loadTimeSlots(selectedFacility);
            loadBookings(selectedFacility);
        } else {
            setCourts([]);
            setTimeSlots([]);
            setRawTimeSlots([]);
            setBookings([]);
            setBookingData({});
        }
    }, [selectedFacility]);

    // Reload bookings when date changes
    useEffect(() => {
        if (selectedFacility) {
            loadBookings(selectedFacility);
        }
    }, [selectedDate]);

    // Khi đổi cơ sở thì reset filter loại sân về all
    useEffect(() => {
        if (selectedFacility) {
            loadModalCategories(selectedFacility);
        } else {
            setModalCategories([]);
        }
    }, [selectedFacility]);

    // Load facilities by court owner ID
    const loadFacilities = async () => {
        try {
            setFacilitiesLoading(true);
            const response = await getFacilitiesByCourtOwnerId(
                userId,
                "",
                null,
                1,
                100
            );
            let facilitiesData = [];
            if (response.data && response.data.items) {
                facilitiesData = response.data.items;
            }
            if (facilitiesData && facilitiesData.length > 0) {
                setFacilities(facilitiesData);
                const firstFacilityId = facilitiesData[0].facilityId;
                setSelectedFacility(firstFacilityId);
            } else {
                setFacilities([]);
            }
        } catch (error) {
            setFacilities([]);
        } finally {
            setFacilitiesLoading(false);
        }
    };

    // Load courts by facility ID
    const loadCourts = async (facilityId) => {
        try {
            setCourtsLoading(true);
            const response = await getAllCourts({
                pageNumber: 1,
                pageSize: 100,
                facilityId: facilityId,
                search: "",
                status: null,
                categoryId: null
            });
            let courtsData = [];
            if (response.data) {
                if (response.data.data) {
                    courtsData = response.data.data;
                } else if (response.data.items) {
                    courtsData = response.data.items;
                } else if (Array.isArray(response.data)) {
                    courtsData = response.data;
                }
            }
            if (courtsData && courtsData.length > 0) {
                setCourts(courtsData);
            } else {
                setCourts([]);
            }
        } catch (error) {
            setCourts([]);
        } finally {
            setCourtsLoading(false);
        }
    };

    // Load time slots by facility ID
    const loadTimeSlots = async (facilityId) => {
        try {
            setTimeSlotsLoading(true);
            const response = await getTimeSlotsByFacilityId(facilityId);
            let timeSlotsData = [];
            if (response.data) {
                if (response.data.data) {
                    timeSlotsData = response.data.data;
                } else if (response.data.items) {
                    timeSlotsData = response.data.items;
                } else if (Array.isArray(response.data)) {
                    timeSlotsData = response.data;
                } else if (response.data.result) {
                    timeSlotsData = response.data.result;
                }
            }
            if (timeSlotsData && timeSlotsData.length > 0) {
                setRawTimeSlots(timeSlotsData);
                const formattedTimeSlots = timeSlotsData.map(slot => {
                    const startTime = slot.startTime || slot.start || slot.timeStart;
                    const endTime = slot.endTime || slot.end || slot.timeEnd;
                    const formatTime = (time) => (time ? time.substring(0, 5) : '');
                    return `${formatTime(startTime)} - ${formatTime(endTime)}`;
                });
                setTimeSlots(formattedTimeSlots);
            } else {
                setTimeSlots([]);
                setRawTimeSlots([]);
            }
        } catch (error) {
            setTimeSlots([]);
            setRawTimeSlots([]);
        } finally {
            setTimeSlotsLoading(false);
        }
    };

    // Load bookings by facility ID
    const loadBookings = async (facilityId) => {
        try {
            setBookingsLoading(true);
            const response = await getBookingsByFacilityId(facilityId, 1, 1000);
            let bookingsData = [];
            if (response.data && response.data.items) {
                bookingsData = response.data.items;
            }
            if (bookingsData && bookingsData.length > 0) {
                setBookings(bookingsData);
                processBookingData(bookingsData);
            } else {
                setBookings([]);
                setBookingData({});
            }
        } catch (error) {
            setBookings([]);
            setBookingData({});
        } finally {
            setBookingsLoading(false);
        }
    };

    // Process booking data
    const processBookingData = (bookingsData) => {
        const processedBookings = {};
        bookingsData.forEach((booking) => {
            try {
                let bookingDate;
                if (booking.checkInDate) {
                    bookingDate = dayjs(booking.checkInDate);
                }
                if (!bookingDate || !bookingDate.isValid()) return;
                const isDateMatch = bookingDate.format('YYYY-MM-DD') === selectedDate.format('YYYY-MM-DD');
                if (!isDateMatch) return;
                if (!booking.slots || !Array.isArray(booking.slots)) return;
                booking.slots.forEach((slot) => {
                    try {
                        const startTime = slot.startTime || slot.timeStart || slot.fromTime;
                        const endTime = slot.endTime || slot.timeEnd || slot.toTime;
                        const courtId = slot.courtId || slot.court_id || slot.id;
                        if (startTime && endTime && courtId) {
                            const formatTime = (time) => (time ? time.substring(0, 5) : '');
                            const timeSlot = `${formatTime(startTime)} - ${formatTime(endTime)}`;
                            const bookingKey = `${courtId}_${bookingDate.format('YYYY-MM-DD')}_${timeSlot}`;
                            let shouldDisplay = false;
                            let finalStatus = 'confirmed';
                            let mappedStatusId = null;
                            let paymentStatus = 'pending';
                            const status = booking.status?.toLowerCase();
                            if (status === 'paid') {
                                shouldDisplay = true;
                                finalStatus = 'paid';
                                mappedStatusId = 7;
                                paymentStatus = 'deposit';
                            } else if (status === 'completed') {
                                shouldDisplay = true;
                                finalStatus = 'completed';
                                mappedStatusId = 10;
                                paymentStatus = 'paid';
                            } else if (status === 'cancelled' || status === 'canceled') {
                                shouldDisplay = true;
                                finalStatus = 'cancelled';
                                mappedStatusId = 9;
                                paymentStatus = 'cancelled';
                            } else if (status === 'active') {
                                shouldDisplay = true;
                                finalStatus = 'confirmed';
                                mappedStatusId = 1;
                                paymentStatus = 'pending';
                            }
                            if (shouldDisplay) {
                                let bookingTime = 'N/A';
                                let rawBookingTime = null;
                                if (booking.createDate) {
                                    rawBookingTime = booking.createDate;
                                } else if (booking.createdAt) {
                                    rawBookingTime = booking.createdAt;
                                } else {
                                    rawBookingTime = booking.checkInDate;
                                }
                                if (rawBookingTime && rawBookingTime !== 'N/A' && rawBookingTime !== '0001-01-01T00:00:00') {
                                    try {
                                        const parsedTime = dayjs(rawBookingTime);
                                        if (parsedTime.isValid()) {
                                            bookingTime = parsedTime.format('DD/MM/YYYY HH:mm:ss');
                                        } else {
                                            bookingTime = rawBookingTime;
                                        }
                                    } catch {
                                        bookingTime = rawBookingTime;
                                    }
                                } else {
                                    bookingTime = 'Không có thông tin';
                                }
                                const processedBooking = {
                                    id: booking.bookingId || booking.id,
                                    userId: booking.userId,
                                    courtId: courtId,
                                    courtName: slot.courtName || slot.court_name || `Court ${courtId}`,
                                    timeSlot: timeSlot,
                                    date: bookingDate.format('DD/MM/YYYY'),
                                    price: booking.totalPrice || booking.price || 0,
                                    status: finalStatus,
                                    paymentStatus: paymentStatus,
                                    bookingTime: bookingTime,
                                    checkInDate: booking.checkInDate,
                                    statusId: mappedStatusId,
                                    originalStatus: booking.status,
                                    rawCreateDate: rawBookingTime,
                                    customerName: 'Đang tải...',
                                    customerPhone: 'Đang tải...',
                                    customerEmail: 'Đang tải...'
                                };
                                processedBookings[bookingKey] = processedBooking;
                            }
                        }
                    } catch { }
                });
            } catch { }
        });
        setBookingData(processedBookings);
    };

    // Load customer details
    const loadCustomerDetails = async (userId) => {
        try {
            const response = await getAccountById(userId);
            let customerData = null;
            if (response.data) {
                if (response.data.data) {
                    customerData = response.data.data;
                } else if (response.data.user) {
                    customerData = response.data.user;
                } else {
                    customerData = response.data;
                }
            }
            if (customerData) {
                return {
                    customerName: customerData.fullName || customerData.name || customerData.userName || 'N/A',
                    customerPhone: customerData.phoneNumber || customerData.phone || 'N/A',
                    customerEmail: customerData.email || 'N/A',
                    customerAvatar: customerData.avatar || customerData.profilePicture || null
                };
            }
            return null;
        } catch {
            return null;
        }
    };

    // Complete booking function
    const handleCompleteBooking = async () => {
        if (!selectedBooking || !selectedBooking.id) {
            message.error('Không tìm thấy thông tin đơn đặt sân');
            return;
        }
        try {
            setCompletingBooking(true);
            const response = await completeBooking(selectedBooking.id);
            const isSuccess =
                response.status === 200 ||
                response.status === 201 ||
                (response.data && response.data.success === true) ||
                (response.data && response.data.status === 200) ||
                (response.data && !response.data.error);
            if (isSuccess) {
                message.success('Hoàn thành đơn đặt sân thành công!');
                setSelectedBooking(prev => ({
                    ...prev,
                    status: 'completed',
                    statusId: 10,
                    paymentStatus: 'paid',
                    originalStatus: 'Completed'
                }));
                const bookingKey = `${selectedBooking.courtId}_${selectedDate.format('YYYY-MM-DD')}_${selectedBooking.timeSlot}`;
                setBookingData(prev => ({
                    ...prev,
                    [bookingKey]: {
                        ...prev[bookingKey],
                        status: 'completed',
                        statusId: 10,
                        paymentStatus: 'paid',
                        originalStatus: 'Completed'
                    }
                }));
                setTimeout(() => {
                    closeModal();
                }, 1000);
                if (selectedFacility) {
                    setTimeout(() => {
                        loadBookings(selectedFacility);
                    }, 1500);
                }
            } else {
                const errorMessage =
                    response.data?.message ||
                    response.data?.error ||
                    'Không thể hoàn thành đơn đặt sân';
                message.error(errorMessage);
            }
        } catch (error) {
            if (error.response) {
                const errorMessage = error.response.data?.message || 'Lỗi từ server';
                message.error(`Không thể hoàn thành đơn: ${errorMessage}`);
            } else if (error.request) {
                message.error('Lỗi kết nối mạng. Vui lòng thử lại.');
            } else {
                message.error('Có lỗi xảy ra. Vui lòng thử lại.');
            }
        } finally {
            setCompletingBooking(false);
        }
    };

    // Open create booking modal
    const openCreateBookingModal = async () => {
        setSelectedTimeSlots([]);
        setIsCreateBookingModalVisible(true);
        if (selectedFacility) {
            await loadModalCategories(selectedFacility);
            createBookingForm.setFieldsValue({ categoryId: undefined });
        } else {
            message.warning('Vui lòng chọn cơ sở trước');
        }
    };
    useEffect(() => {
        if (isCreateBookingModalVisible) {
            createBookingForm.setFieldsValue({ categoryId: undefined });
        }
        // eslint-disable-next-line
    }, [modalCategories]);
    // Close create booking modal
    const closeCreateBookingModal = () => {
        setIsCreateBookingModalVisible(false);
        createBookingForm.resetFields();
        setSelectedTimeSlots([]);
        setCreatingBooking(false);
    };

    // Handle create booking form submission
    const handleCreateBooking = async (values) => {
        try {
            setCreatingBooking(true);
            if (selectedTimeSlots.length === 0) {
                message.error('Vui lòng chọn ít nhất một khung giờ');
                return;
            }
            const timeSlotIds = selectedTimeSlots.map(timeSlot => getTimeSlotId(timeSlot));
            const checkInDate = selectedDate.format('YYYY-MM-DDTHH:mm:ss.SSS') + 'Z';
            const bookingRequestData = {
                userId: 16,
                email: "admin@courtowner.com",
                phone: "0000000000",
                checkInDate: checkInDate,
                timeSlotIds: timeSlotIds,
                facilityId: selectedFacility,
                categoryId: values.categoryId
            };
            const createResponse = await createBookingForCO(bookingRequestData);
            const isCreateSuccess =
                createResponse.status === 200 ||
                createResponse.status === 201 ||
                (createResponse.data && createResponse.data.success === true) ||
                (createResponse.data && createResponse.data.status === 200) ||
                (createResponse.data && !createResponse.data.error);
            if (isCreateSuccess) {
                message.success('Tạo đơn đặt sân thành công!');
                setTimeout(() => {
                    closeCreateBookingModal();
                }, 1000);
                if (selectedFacility) {
                    setTimeout(() => {
                        loadBookings(selectedFacility);
                    }, 1500);
                }
            } else {
                const errorMessage =
                    createResponse.data?.message ||
                    createResponse.data?.error ||
                    'Không thể tạo đơn đặt sân';
                message.error(errorMessage);
            }
        } catch (error) {
            if (error.response) {
                const errorMessage = error.response.data?.message || 'Lỗi từ server';
                message.error(`Không thể tạo đơn: ${errorMessage}`);
            } else if (error.request) {
                message.error('Lỗi kết nối mạng. Vui lòng thử lại.');
            } else {
                message.error('Có lỗi xảy ra. Vui lòng thử lại.');
            }
        } finally {
            setCreatingBooking(false);
        }
    };

    const getBookingKey = (courtId, date, timeSlot) => {
        return `${courtId}_${date.format('YYYY-MM-DD')}_${timeSlot}`;
    };

    // Handle slot click - CHỈ HIỂN THỊ MODAL CHO SLOT ĐÃ ĐƯỢC ĐẶT
    const handleSlotClick = async (court, timeSlot) => {
        const bookingKey = getBookingKey(court.courtId || court.id, selectedDate, timeSlot);
        const booking = bookingData[bookingKey];
        if (booking) {
            setCustomerLoading(true);
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
                        setSelectedBooking(prev => ({
                            ...prev,
                            ...customerDetails
                        }));
                    } else {
                        setSelectedBooking(prev => ({
                            ...prev,
                            customerName: 'Không tìm thấy thông tin',
                            customerPhone: 'Không tìm thấy thông tin',
                            customerEmail: 'Không tìm thấy thông tin'
                        }));
                    }
                } catch {
                    setSelectedBooking(prev => ({
                        ...prev,
                        customerName: 'Lỗi tải thông tin',
                        customerPhone: 'Lỗi tải thông tin',
                        customerEmail: 'Lỗi tải thông tin'
                    }));
                }
            }
            setCustomerLoading(false);
        }
    };

    // Get slot status
    const getSlotStatus = (court, timeSlot) => {
        const bookingKey = getBookingKey(court.courtId || court.id, selectedDate, timeSlot);
        const booking = bookingData[bookingKey];
        if (!booking) return 'available';
        return booking.status;
    };

    // Get slot display text
    const getSlotDisplayText = (status) => {
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
    };

    const closeModal = () => {
        setIsModalVisible(false);
        setSelectedBooking(null);
        setCustomerLoading(false);
        setCompletingBooking(false);
    };

    const handleFacilityChange = (value) => {
        setSelectedFacility(value);
    };

    const handleDateChange = (date) => {
        setSelectedDate(date);
    };

    // Lọc courts theo loại sân đã chọn
    const filteredCourts = React.useMemo(() => {
        if (selectedCategoryFilter === 'all') return courts;
        // Tìm categoryName từ modalCategories dựa vào categoryId được chọn
        const selectedCategory = modalCategories.find(
            cat => String(cat.categoryId) === String(selectedCategoryFilter)
        );
        if (!selectedCategory) return [];
        return courts.filter(
            court => court.categoryName === selectedCategory.categoryName
        );
    }, [courts, selectedCategoryFilter, modalCategories]);

    return (
        <div className="booking-management">
            <div className="main-container">
                {/* Header */}
                <div className="header">
                    <div className="header-content">
                        <HomeOutlined className="header-icon" />
                        <div className="header-text">
                            <h1 style={{ color: "white" }}>Quản Lý Đơn Đặt Sân</h1>
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
                                loading={facilitiesLoading}
                                placeholder="Chọn cơ sở"
                                notFoundContent={facilitiesLoading ? <Spin size="small" /> : "Không có dữ liệu"}
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

                        {/* Create Booking Button */}
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
                        <span>Còn trống ({courts.length * timeSlots.length - Object.keys(bookingData).length} slots)</span>
                    </div>
                    <div className="status-item">
                        <div className="status-dot paid"></div>
                        <span>Đã Đặt ({Object.values(bookingData).filter(b => b.status === 'paid').length} slots)</span>
                    </div>
                    <div className="status-item">
                        <div className="status-dot completed"></div>
                        <span>Đã hoàn thành ({Object.values(bookingData).filter(b => b.status === 'completed').length} slots)</span>
                    </div>
                    <div className="status-item">
                        <div className="status-dot cancelled"></div>
                        <span>Đã hủy ({Object.values(bookingData).filter(b => b.status === 'cancelled').length} slots)</span>
                    </div>
                    {/* Category Filter */}
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
                            disabled={modalCategoriesLoading}
                        >
                            <option value="all">Tất cả</option>
                            {modalCategories.map(category => (
                                <option key={category.categoryId} value={category.categoryId}>
                                    {category.categoryName}
                                </option>
                            ))}
                        </select>
                        {modalCategoriesLoading && <Spin size="small" style={{ marginLeft: 8 }} />}
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
                                    {timeSlotsLoading ? (
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
                                {courtsLoading || bookingsLoading ? (
                                    <div className="loading-container">
                                        <div>
                                            <Spin size="large" />
                                            <span>Đang tải dữ liệu...</span>
                                        </div>
                                    </div>
                                ) : filteredCourts.length === 0 ? (
                                    <div className="no-data-container">
                                        <div>
                                            <span>
                                                {selectedCategoryFilter === 'all'
                                                    ? (selectedFacility ? 'Không có sân nào' : 'Vui lòng chọn cơ sở')
                                                    : 'Không có sân nào thuộc loại này'}
                                            </span>
                                        </div>
                                    </div>
                                ) : timeSlots.length === 0 ? (
                                    <div className="no-data-container">
                                        <div>
                                            <span>Không có khung giờ</span>
                                        </div>
                                    </div>
                                ) : (
                                    filteredCourts.map((court) => (
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
                                    ))
                                )}
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            {/* Modal for filled slots only */}
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
                                {customerLoading ? (
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
                                    loading={completingBooking}
                                    icon={<CheckCircleOutlined />}
                                >
                                    {completingBooking ? 'Đang hoàn thành...' : 'Hoàn Thành Đơn'}
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

            {/* Create Booking Modal - IMPROVED DESIGN */}
            <Modal
                title={null}
                open={isCreateBookingModalVisible}
                onCancel={closeCreateBookingModal}
                footer={null}
                width={700}
                className="create-booking-modal-modern"
                centered
                bodyStyle={{ padding: 0 }}
            >
                <div className="modern-modal-wrapper">
                    {/* Beautiful Modal Header */}
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

                    {/* Modal Body */}
                    <div className="modern-modal-body">
                        <Form
                            form={createBookingForm}
                            layout="vertical"
                            onFinish={handleCreateBooking}
                            className="modern-booking-form"
                        >
                            {/* Category Selection Section */}
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

                            {/* Time Slots Selection Section */}
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
                                    {timeSlotsLoading ? (
                                        <div className="loading-slots">
                                            <Spin size="large" />
                                            <span>Đang tải khung giờ...</span>
                                        </div>
                                    ) : timeSlots.length === 0 ? (
                                        <div className="empty-slots">
                                            <ClockCircleOutlined className="empty-icon" />
                                            <span>Không có khung giờ nào</span>
                                        </div>
                                    ) : (
                                        timeSlots.map(slot => (
                                            <div
                                                key={slot}
                                                className={`time-slot-card ${isTimeSlotSelected(slot) ? 'selected' : ''}`}
                                                onClick={() => handleTimeSlotToggle(slot)}
                                            >
                                                <div className="slot-time">
                                                    <ClockCircleOutlined className="slot-icon" />
                                                    <span>{slot}</span>
                                                </div>
                                                <div className="slot-indicator">
                                                    {isTimeSlotSelected(slot) && (
                                                        <CheckCircleOutlined className="check-icon" />
                                                    )}
                                                </div>
                                            </div>
                                        ))
                                    )}
                                </div>
                                {/* Selected Slots Summary */}
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
                            {/* Form Actions */}
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
                                    loading={creatingBooking}
                                    icon={<PlusOutlined />}
                                    className="submit-button"
                                    disabled={selectedTimeSlots.length === 0}
                                >
                                    {creatingBooking ? 'Đang tạo đơn...' : 'Tạo Đơn Đặt Sân'}
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