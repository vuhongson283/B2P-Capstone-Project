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
import { getFacilitiesByCourtOwnerId, getAllCourts, getTimeSlotsByFacilityId } from "../../services/apiService"; // Import API functions

const { Option } = Select;

const BookingManagement = () => {
    // State for facilities and courts data
    const [facilities, setFacilities] = useState([]);
    const [courts, setCourts] = useState([]);
    const [timeSlots, setTimeSlots] = useState([]); // Dynamic time slots
    const [selectedFacility, setSelectedFacility] = useState(null);
    const [selectedDate, setSelectedDate] = useState(dayjs());
    const [bookingData, setBookingData] = useState({});
    const [isModalVisible, setIsModalVisible] = useState(false);
    const [selectedBooking, setSelectedBooking] = useState(null);
    const [loading, setLoading] = useState(false);
    const [facilitiesLoading, setFacilitiesLoading] = useState(false);
    const [courtsLoading, setCourtsLoading] = useState(false);
    const [timeSlotsLoading, setTimeSlotsLoading] = useState(false);

    // Fixed user ID
    const userId = 13;

    // Load facilities when component mounts
    useEffect(() => {
        loadFacilities();
    }, []);

    // Load courts and time slots when facility is selected
    useEffect(() => {
        if (selectedFacility) {
            loadCourts(selectedFacility);
            loadTimeSlots(selectedFacility);
        } else {
            setCourts([]);
            setTimeSlots([]);
        }
    }, [selectedFacility]);

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

    // Mock booking data
    const mockBookings = {
        "14_2025-07-30_08:00 - 10:00": {
            id: "BK001",
            courtId: 14,
            courtName: "Sân tennis số 1",
            timeSlot: "08:00 - 10:00",
            date: "30/07/2025",
            customerName: "Nguyễn Văn A",
            customerPhone: "0901234567",
            customerEmail: "nguyenvana@email.com",
            price: 200000,
            status: "confirmed",
            paymentStatus: "paid",
            bookingTime: "28/07/2025 14:30:00"
        },
        "27_2025-07-30_15:00 - 17:00": {
            id: "BK002",
            courtId: 27,
            courtName: "Sân Bóng Mini CMT8",
            timeSlot: "15:00 - 17:00",
            date: "30/07/2025",
            customerName: "Trần Thị B",
            customerPhone: "0907654321",
            customerEmail: "tranthib@email.com",
            price: 250000,
            status: "confirmed",
            paymentStatus: "pending",
            bookingTime: "29/07/2025 09:15:00"
        }
    };

    useEffect(() => {
        setBookingData(mockBookings);
    }, []);

    const getBookingKey = (courtId, date, timeSlot) => {
        return `${courtId}_${date.format('YYYY-MM-DD')}_${timeSlot}`;
    };

    const handleSlotClick = (court, timeSlot) => {
        const bookingKey = getBookingKey(court.courtId || court.id, selectedDate, timeSlot);
        const booking = bookingData[bookingKey];

        if (booking) {
            setSelectedBooking({
                ...booking,
                timeSlot,
                date: selectedDate.format('DD/MM/YYYY')
            });
            setIsModalVisible(true);
        }
    };

    const getSlotStatus = (court, timeSlot) => {
        const bookingKey = getBookingKey(court.courtId || court.id, selectedDate, timeSlot);
        return bookingData[bookingKey] ? 'booked' : 'available';
    };

    const closeModal = () => {
        setIsModalVisible(false);
        setSelectedBooking(null);
    };

    const handleFacilityChange = (value) => {
        console.log('🔄 Facility changed to:', value);
        setSelectedFacility(value);
    };

    const handleDateChange = (date) => {
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
                            <h1>Quản Lý Đơn Đặt Sân</h1>
                            <div className="subtitle">Theo dõi và quản lý lịch đặt sân hiệu quả</div>
                        </div>
                    </div>
                    <div className="header-actions">
                        <Button className="add-button" icon={<PlusOutlined />}>
                            Thêm đặt sân
                        </Button>
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
                        <span>Còn trống</span>
                    </div>
                    <div className="status-item">
                        <div className="status-dot booked"></div>
                        <span>Đã được đặt</span>
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
                                {courtsLoading ? (
                                    <div className="loading-container">
                                        <Spin size="large" />
                                        <span>Đang tải danh sách sân...</span>
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

            {/* Modal với gradient header */}
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

                        <div className="customer-info">
                            <h3>Thông tin khách hàng</h3>
                            <div className="customer-details">
                                <Avatar size={64} icon={<UserOutlined />} className="avatar" />
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