import React, { useState, useEffect } from "react";
import "./BookingManagement.scss";
import { Select, DatePicker, Modal, Button, Tag, Avatar, Divider, Spin, message } from "antd";
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
} from "@ant-design/icons";
import dayjs from "dayjs";
import {
    getFacilitiesByCourtOwnerId,
    getAllCourts,
    getTimeSlotsByFacilityId,
    getBookingsByFacilityId,
    getAccountById // Import hàm mới
} from "../../services/apiService"; // Import API functions

const { Option } = Select;

const BookingManagement = () => {
    // State for facilities and courts data
    const [facilities, setFacilities] = useState([]);
    const [courts, setCourts] = useState([]);
    const [timeSlots, setTimeSlots] = useState([]); // Dynamic time slots
    const [selectedFacility, setSelectedFacility] = useState(null);
    const [selectedDate, setSelectedDate] = useState(dayjs());
    const [bookingData, setBookingData] = useState({}); // Real booking data
    const [bookings, setBookings] = useState([]); // Raw bookings từ API
    const [isModalVisible, setIsModalVisible] = useState(false);
    const [selectedBooking, setSelectedBooking] = useState(null);
    const [loading, setLoading] = useState(false);
    const [facilitiesLoading, setFacilitiesLoading] = useState(false);
    const [courtsLoading, setCourtsLoading] = useState(false);
    const [timeSlotsLoading, setTimeSlotsLoading] = useState(false);
    const [bookingsLoading, setBookingsLoading] = useState(false);
    const [customerLoading, setCustomerLoading] = useState(false); // Loading cho customer info

    // Fixed user ID
    const userId = 13;

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

    // ... (giữ nguyên các hàm loadFacilities, loadCourts, loadTimeSlots như cũ)

    // Load facilities by court owner ID
    const loadFacilities = async () => {
        try {
            setFacilitiesLoading(true);
            console.log('🔍 Loading facilities for userId:', userId);

            const response = await getFacilitiesByCourtOwnerId(
                userId,
                "", // facilityName
                null, // statusId
                1, // currentPage
                100 // itemsPerPage - get all facilities
            );

            console.log('📊 Facilities Response:', response.data);

            let facilitiesData = [];
            if (response.data && response.data.items) {
                facilitiesData = response.data.items;
            }

            if (facilitiesData && facilitiesData.length > 0) {
                setFacilities(facilitiesData);
                // Auto-select first facility
                const firstFacilityId = facilitiesData[0].facilityId;
                setSelectedFacility(firstFacilityId);
                console.log('✅ Auto-selected facility ID:', firstFacilityId);
            } else {
                message.warning('Không tìm thấy cơ sở nào cho user ID: ' + userId);
                setFacilities([]);
            }
        } catch (error) {
            console.error('❌ Error loading facilities:', error);
            message.error('Không thể tải danh sách cơ sở');
            setFacilities([]);
        } finally {
            setFacilitiesLoading(false);
        }
    };

    // Load courts by facility ID
    const loadCourts = async (facilityId) => {
        try {
            setCourtsLoading(true);
            console.log('🏟️ Loading courts for facilityId:', facilityId);

            const response = await getAllCourts({
                pageNumber: 1,
                pageSize: 100,
                facilityId: facilityId,
                search: "",
                status: null,
                categoryId: null
            });

            console.log('🏟️ Courts Response:', response.data);

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
                console.log('✅ Courts loaded:', courtsData.map(c => ({ id: c.courtId || c.id, name: c.courtName || c.name })));
            } else {
                setCourts([]);
                message.info('Không có sân nào trong cơ sở này');
            }
        } catch (error) {
            console.error('❌ Error loading courts:', error);
            message.error('Không thể tải danh sách sân');
            setCourts([]);
        } finally {
            setCourtsLoading(false);
        }
    };

    // Load time slots by facility ID
    const loadTimeSlots = async (facilityId) => {
        try {
            setTimeSlotsLoading(true);
            console.log('⏰ Loading time slots for facilityId:', facilityId);

            const response = await getTimeSlotsByFacilityId(facilityId);

            console.log('⏰ Time Slots Response:', response);
            console.log('⏰ Time Slots Data:', response.data);

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

            console.log('⏰ Processed Time Slots Data:', timeSlotsData);

            if (timeSlotsData && timeSlotsData.length > 0) {
                // Format time slots for display (assuming API returns startTime and endTime)
                const formattedTimeSlots = timeSlotsData.map(slot => {
                    // Adjust field names based on your API response structure
                    const startTime = slot.startTime || slot.start || slot.timeStart;
                    const endTime = slot.endTime || slot.end || slot.timeEnd;

                    // Format time (remove seconds if present)
                    const formatTime = (time) => {
                        if (!time) return '';
                        return time.substring(0, 5); // Get HH:MM part
                    };

                    return `${formatTime(startTime)} - ${formatTime(endTime)}`;
                });

                setTimeSlots(formattedTimeSlots);
                console.log('✅ Formatted time slots:', formattedTimeSlots);
            } else {
                setTimeSlots([]);
                message.info('Không có khung giờ nào cho cơ sở này');
            }
        } catch (error) {
            console.error('❌ Error loading time slots:', error);
            message.error('Không thể tải khung giờ');
            setTimeSlots([]);
        } finally {
            setTimeSlotsLoading(false);
        }
    };

    // Load bookings by facility ID
    const loadBookings = async (facilityId) => {
        try {
            setBookingsLoading(true);
            console.log('📅 Loading bookings for facilityId:', facilityId);
            console.log('📅 Selected date:', selectedDate.format('YYYY-MM-DD'));

            const response = await getBookingsByFacilityId(facilityId, 1, 1000);

            console.log('📅 RAW Bookings API Response:', response);

            let bookingsData = [];
            if (response.data && response.data.items) {
                bookingsData = response.data.items;
            }

            console.log('📅 RAW Bookings Data:', bookingsData);

            if (bookingsData && bookingsData.length > 0) {
                setBookings(bookingsData);
                processBookingData(bookingsData);
            } else {
                setBookings([]);
                setBookingData({});
            }
        } catch (error) {
            console.error('❌ Error loading bookings:', error);
            message.error('Không thể tải danh sách đặt sân');
            setBookings([]);
            setBookingData({});
        } finally {
            setBookingsLoading(false);
        }
    };

    // Process booking data into lookup format - CẬP NHẬT
    const processBookingData = (bookingsData) => {
        console.log('🔄 Processing booking data...');
        console.log('🔄 Total bookings to process:', bookingsData.length);

        const processedBookings = {};

        bookingsData.forEach((booking, index) => {
            try {
                console.log(`🔄 Processing booking ${index + 1}:`, booking);

                // Parse date from checkInDate
                let bookingDate;
                if (booking.checkInDate) {
                    bookingDate = dayjs(booking.checkInDate);
                    console.log(`📅 Found checkInDate:`, booking.checkInDate, '→', bookingDate.format('YYYY-MM-DD'));
                }

                if (!bookingDate || !bookingDate.isValid()) {
                    console.log('❌ No valid date found for booking:', booking);
                    return;
                }

                // Check if booking has slots array
                if (!booking.slots || !Array.isArray(booking.slots)) {
                    console.log('❌ No slots array found in booking:', booking);
                    return;
                }

                console.log(`📋 Found ${booking.slots.length} slots in booking:`, booking.slots);

                // Process each slot in the booking
                booking.slots.forEach((slot, slotIndex) => {
                    try {
                        console.log(`🔄 Processing slot ${slotIndex + 1}:`, slot);

                        const startTime = slot.startTime || slot.timeStart || slot.fromTime;
                        const endTime = slot.endTime || slot.timeEnd || slot.toTime;
                        const courtId = slot.courtId || slot.court_id || slot.id;
                        const statusId = slot.statusId || slot.status_id || booking.statusId || booking.status_id;

                        console.log(`⏰ Slot times: ${startTime} - ${endTime}`);
                        console.log(`🏟️ Court ID: ${courtId}`);
                        console.log(`📊 Status ID: ${statusId}`);

                        if (startTime && endTime && courtId) {
                            const formatTime = (time) => {
                                if (!time) return '';
                                return time.substring(0, 5);
                            };

                            const timeSlot = `${formatTime(startTime)} - ${formatTime(endTime)}`;
                            const bookingKey = `${courtId}_${bookingDate.format('YYYY-MM-DD')}_${timeSlot}`;

                            // Check status - look for different status indicators
                            let isActive = false;

                            if (statusId === 7 || statusId === '7') {
                                isActive = true;
                            } else if (booking.status === 'Active' || slot.status === 'Active') {
                                isActive = true;
                            } else if (booking.status === 'Paid' || slot.status === 'Paid' ||
                                booking.status === 'Confirmed' || slot.status === 'Confirmed') {
                                isActive = true;
                            }

                            if (isActive) {
                                console.log('✅ Adding active booking to processed bookings');

                                processedBookings[bookingKey] = {
                                    id: booking.bookingId || booking.id,
                                    userId: booking.userId, // Lưu userId để load customer info
                                    courtId: courtId,
                                    courtName: slot.courtName || slot.court_name || `Court ${courtId}`,
                                    timeSlot: timeSlot,
                                    date: bookingDate.format('DD/MM/YYYY'),
                                    price: booking.totalPrice || booking.price || 0,
                                    status: 'confirmed',
                                    paymentStatus: booking.status === 'Paid' ? 'paid' : 'pending',
                                    bookingTime: booking.checkInDate || 'N/A',
                                    statusId: statusId || 7,
                                    originalStatus: booking.status,
                                    // Placeholder customer info (sẽ được load khi click)
                                    customerName: 'Đang tải...',
                                    customerPhone: 'Đang tải...',
                                    customerEmail: 'Đang tải...'
                                };

                                console.log(`✅ Added booking: ${bookingKey}`, processedBookings[bookingKey]);
                            }
                        }
                    } catch (slotError) {
                        console.error(`❌ Error processing slot ${slotIndex + 1}:`, slot, slotError);
                    }
                });

            } catch (error) {
                console.error(`❌ Error processing booking ${index + 1}:`, booking, error);
            }
        });

        console.log('📊 Final processed bookings:', processedBookings);
        setBookingData(processedBookings);
    };

    // Load customer details - HÀM MỚI
    const loadCustomerDetails = async (userId) => {
        try {
            console.log('👤 Loading customer details for userId:', userId);
            const response = await getAccountById(userId);

            console.log('👤 Customer API Response:', response);

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

            console.log('👤 Customer Data:', customerData);

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
            console.error('❌ Error loading customer details:', error);
            return null;
        }
    };

    const getBookingKey = (courtId, date, timeSlot) => {
        const key = `${courtId}_${date.format('YYYY-MM-DD')}_${timeSlot}`;
        return key;
    };

    // Handle slot click - CẬP NHẬT ĐỂ LOAD CUSTOMER INFO
    const handleSlotClick = async (court, timeSlot) => {
        const bookingKey = getBookingKey(court.courtId || court.id, selectedDate, timeSlot);
        const booking = bookingData[bookingKey];

        console.log('🖱️ Slot clicked:', {
            court: court.courtName || court.name,
            timeSlot,
            date: selectedDate.format('YYYY-MM-DD'),
            bookingKey,
            hasBooking: !!booking
        });

        if (booking) {
            setCustomerLoading(true);
            setIsModalVisible(true);

            // Set initial booking data
            setSelectedBooking({
                ...booking,
                timeSlot,
                date: selectedDate.format('DD/MM/YYYY')
            });

            // Load customer details asynchronously
            if (booking.userId) {
                try {
                    const customerDetails = await loadCustomerDetails(booking.userId);

                    if (customerDetails) {
                        // Update booking with customer details
                        setSelectedBooking(prev => ({
                            ...prev,
                            ...customerDetails
                        }));
                    } else {
                        // Fallback if customer details not found
                        setSelectedBooking(prev => ({
                            ...prev,
                            customerName: 'Không tìm thấy thông tin',
                            customerPhone: 'Không tìm thấy thông tin',
                            customerEmail: 'Không tìm thấy thông tin'
                        }));
                    }
                } catch (error) {
                    console.error('Error loading customer details:', error);
                    setSelectedBooking(prev => ({
                        ...prev,
                        customerName: 'Lỗi tải thông tin',
                        customerPhone: 'Lỗi tải thông tin',
                        customerEmail: 'Lỗi tải thông tin'
                    }));
                }
            }

            setCustomerLoading(false);
        } else {
            console.log('🆕 No booking found, could open new booking form');
            message.info('Slot này đang trống. Có thể tạo đặt sân mới.');
        }
    };

    const getSlotStatus = (court, timeSlot) => {
        const bookingKey = getBookingKey(court.courtId || court.id, selectedDate, timeSlot);
        const booking = bookingData[bookingKey];
        return booking ? 'booked' : 'available';
    };

    const closeModal = () => {
        setIsModalVisible(false);
        setSelectedBooking(null);
        setCustomerLoading(false);
    };

    const handleFacilityChange = (value) => {
        console.log('🔄 Facility changed to:', value);
        setSelectedFacility(value);
    };

    const handleDateChange = (date) => {
        console.log('📅 Date changed to:', date.format('YYYY-MM-DD'));
        setSelectedDate(date);
    };

    return (
        <div className="booking-management">
            <div className="main-container">
                {/* Header với gradient đẹp */}
                <div className="header">
                    <div className="header-content">
                        <HomeOutlined className="header-icon" />
                        <div className="header-text">
                            <h1 style={{ color: "white" }}>Quản Lý Đơn Đặt Sân</h1>
                            <div className="subtitle">Theo dõi và quản lý lịch đặt sân hiệu quả</div>
                        </div>
                    </div>
                </div>

                {/* Filters với design mới */}
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
                    </div>
                </div>

                {/* Status indicators */}
                <div className="status-indicator">
                    <div className="status-item">
                        <div className="status-dot available"></div>
                        <span>Còn trống ({Object.keys(bookingData).length > 0 ?
                            courts.length * timeSlots.length - Object.keys(bookingData).length :
                            courts.length * timeSlots.length} slots)</span>
                    </div>
                    <div className="status-item">
                        <div className="status-dot booked"></div>
                        <span>Đã được đặt ({Object.keys(bookingData).length} slots)</span>
                    </div>
                </div>

                {/* Booking Table với design mới */}
                <div className="booking-table">
                    <div className="table-scroll-container">
                        <div className="table-content">
                            {/* Table Header */}
                            <div className="table-header">
                                <div className="header-cell time-header">
                                    <ClockCircleOutlined />
                                    <span>KHUNG GIỜ</span>
                                </div>
                                {timeSlotsLoading ? (
                                    <div className="header-cell loading-header">
                                        <Spin size="small" />
                                        <span>Đang tải khung giờ...</span>
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

                            {/* Table Body */}
                            <div className="table-body">
                                {courtsLoading || bookingsLoading ? (
                                    <div className="loading-container">
                                        <Spin size="large" />
                                        <span>
                                            {courtsLoading && 'Đang tải danh sách sân...'}
                                            {bookingsLoading && 'Đang tải thông tin đặt sân...'}
                                            {courtsLoading && bookingsLoading && 'Đang tải dữ liệu...'}
                                        </span>
                                    </div>
                                ) : courts.length === 0 ? (
                                    <div className="no-data-container">
                                        <span>{selectedFacility ? 'Không có sân nào trong cơ sở này' : 'Vui lòng chọn cơ sở'}</span>
                                    </div>
                                ) : timeSlots.length === 0 ? (
                                    <div className="no-data-container">
                                        <span>Không có khung giờ cho cơ sở này</span>
                                    </div>
                                ) : (
                                    courts.map((court) => (
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
                                                    >
                                                        {status === 'available' ? 'Còn trống' : 'Đã được đặt'}
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

            {/* Modal với gradient header - CẬP NHẬT CUSTOMER SECTION */}
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
                                {selectedBooking.status === 'confirmed' ? 'Đã xác nhận' : 'Chờ xác nhận'}
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

                        {/* CUSTOMER INFO SECTION - CẬP NHẬT */}
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

                        <div className="payment-status">
                            <div className="payment-info">
                                {selectedBooking.paymentStatus === 'paid' ? (
                                    <>
                                        <CheckCircleOutlined className="payment-icon paid" />
                                        <span className="payment-text paid">Đã thanh toán</span>
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

                        <div className="modal-actions">
                            <Button onClick={closeModal}>Đóng</Button>
                            <Button type="primary" className="action-button">
                                Cập nhật
                            </Button>
                        </div>
                    </div>
                )}
            </Modal>
        </div>
    );
};

export default BookingManagement;