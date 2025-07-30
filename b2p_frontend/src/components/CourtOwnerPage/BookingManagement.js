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
} from "@ant-design/icons";
import dayjs from "dayjs";
import { getFacilitiesByCourtOwnerId, getAllCourts } from "../../services/apiService"; // Import API functions

const { Option } = Select;

const BookingManagement = () => {
    // State for facilities and courts data
    const [facilities, setFacilities] = useState([]);
    const [courts, setCourts] = useState([]);
    const [selectedFacility, setSelectedFacility] = useState(null);
    const [selectedDate, setSelectedDate] = useState(dayjs());
    const [bookingData, setBookingData] = useState({});
    const [isModalVisible, setIsModalVisible] = useState(false);
    const [selectedBooking, setSelectedBooking] = useState(null);
    const [loading, setLoading] = useState(false);
    const [facilitiesLoading, setFacilitiesLoading] = useState(false);
    const [courtsLoading, setCourtsLoading] = useState(false);

    // Fixed user ID
    const userId = 13;

    const timeSlots = [
        "06:00 - 08:00",
        "08:00 - 10:00",
        "10:00 - 12:00",
        "13:00 - 15:00",
        "15:00 - 17:00",
        "17:00 - 19:00",
        "19:00 - 21:00",
        "21:00 - 23:00"
    ];

    // Load facilities when component mounts
    useEffect(() => {
        loadFacilities();
    }, []);

    // Load courts when facility is selected
    useEffect(() => {
        if (selectedFacility) {
            loadCourts(selectedFacility);
        } else {
            setCourts([]);
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

            console.log('📊 Full API Response:', response);
            console.log('📊 Response Data:', response.data);

            // The correct structure is response.data.items
            let facilitiesData = [];

            if (response.data && response.data.items) {
                facilitiesData = response.data.items;
            }

            console.log('🏢 Facilities Data:', facilitiesData);
            console.log('🔢 Number of facilities:', facilitiesData.length);

            if (facilitiesData && facilitiesData.length > 0) {
                setFacilities(facilitiesData);
                console.log('✅ Setting facilities:', facilitiesData);

                // Auto-select first facility
                const firstFacilityId = facilitiesData[0].facilityId;
                setSelectedFacility(firstFacilityId);
                console.log('🎯 Auto-selected facility ID:', firstFacilityId);
            } else {
                console.log('❌ No facilities found');
                message.warning('Không tìm thấy cơ sở nào cho user ID: ' + userId);
                setFacilities([]);
            }
        } catch (error) {
            console.error('❌ Error loading facilities:', error);
            console.error('❌ Error details:', error.response);
            console.error('❌ Error message:', error.message);

            if (error.response) {
                message.error(`Lỗi API: ${error.response.status} - ${error.response.data?.message || 'Unknown error'}`);
            } else if (error.request) {
                message.error('Không thể kết nối đến server');
            } else {
                message.error('Lỗi: ' + error.message);
            }
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
                pageSize: 100, // Get all courts
                facilityId: facilityId,
                search: "",
                status: null,
                categoryId: null
            });

            console.log('🏟️ Courts Response:', response);
            console.log('🏟️ Courts Data:', response.data);

            let courtsData = [];

            if (response.data) {
                if (response.data.data) {
                    courtsData = response.data.data;
                } else if (response.data.items) {
                    courtsData = response.data.items;
                } else if (response.data.result) {
                    courtsData = response.data.result;
                } else if (Array.isArray(response.data)) {
                    courtsData = response.data;
                }
            }

            console.log('🏟️ Processed Courts Data:', courtsData);

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
                {/* Header */}
                <div className="header">
                    <HomeOutlined className="header-icon" />
                    <span>Quản Lý Đơn Đặt Sân</span>
                </div>

                {/* Filters */}
                <div className="filters-section">
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

                {/* Booking Table với scroll container */}
                <div className="booking-table">
                    <div className="table-scroll-container">
                        <div className="table-content">
                            {/* Table Header */}
                            <div className="table-header">
                                <div className="header-cell time-header">
                                    <ClockCircleOutlined />
                                    <span>KHUNG GIỜ</span>
                                </div>
                                {timeSlots.map(slot => (
                                    <div key={slot} className="header-cell time-slot">
                                        {slot}
                                    </div>
                                ))}
                            </div>

                            {/* Table Body */}
                            <div className="table-body">
                                {courtsLoading ? (
                                    <div className="loading-container" style={{
                                        display: 'flex',
                                        justifyContent: 'center',
                                        alignItems: 'center',
                                        padding: '50px',
                                        gridColumn: '1 / -1'
                                    }}>
                                        <Spin size="large" />
                                        <span style={{ marginLeft: '10px' }}>Đang tải danh sách sân...</span>
                                    </div>
                                ) : courts.length === 0 ? (
                                    <div className="no-data-container" style={{
                                        display: 'flex',
                                        justifyContent: 'center',
                                        alignItems: 'center',
                                        padding: '50px',
                                        gridColumn: '1 / -1',
                                        color: '#999'
                                    }}>
                                        {selectedFacility ? 'Không có sân nào trong cơ sở này' : 'Vui lòng chọn cơ sở'}
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

            {/* Modal giữ nguyên */}
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