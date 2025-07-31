import React, { useState, useEffect } from 'react';
import { Card, Row, Col, Statistic, Table, Typography } from 'antd';
import { ArrowUpOutlined, ArrowDownOutlined } from '@ant-design/icons';
import './AdminDashboard.scss';

const { Title, Text } = Typography;

const CourtOwnerDashboard = () => {
  const [loading, setLoading] = useState(true);
  const [stats, setStats] = useState(null);
  const [topFacilities, setTopFacilities] = useState([]);

  // Mock data - Thay bằng API call thực tế
  useEffect(() => {
    const fetchData = async () => {
      try {
        // Giả lập API call
        const mockResponse = {
          success: true,
          message: "Lấy dữ liệu báo cáo thành công!",
          status: 200,
          data: {
            year: 2025,
            month: 6,
            startDate: "2025-06-01T00:00:00",
            endDate: "2025-06-30T00:00:00",
            monthlyStats: {
              totalBooking: 10,
              totalRevenue: 2040000,
              averageRevenuePerBooking: 255000,
              completedBookings: 8,
              cancelledBookings: 2,
              totalFacilities: 10,
              totalCourts: 11,
              activeUsers: 10
            },
            topFacilities: [
              {
                facilityId: 7,
                facilityName: "Cơ sở Cầu Giấy",
                totalBooking: 3,
                totalRevenue: 600000
              },
              // ... các dữ liệu khác
            ],
            popularCourtCategories: []
          }
        };

        setStats(mockResponse.data.monthlyStats);
        setTopFacilities(mockResponse.data.topFacilities);
        setLoading(false);
      } catch (error) {
        console.error('Error fetching dashboard data:', error);
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  // Hàm format tiền tệ
  const formatCurrency = (value) => {
    return new Intl.NumberFormat('vi-VN', { 
      style: 'currency', 
      currency: 'VND' 
    }).format(value);
  };

  // Columns cho bảng top facilities
  const facilityColumns = [
    {
      title: 'Tên Cơ Sở',
      dataIndex: 'facilityName',
      key: 'facilityName',
    },
    {
      title: 'Số Đơn',
      dataIndex: 'totalBooking',
      key: 'totalBooking',
      align: 'center',
    },
    {
      title: 'Doanh Thu',
      dataIndex: 'totalRevenue',
      key: 'totalRevenue',
      render: (value) => formatCurrency(value),
      align: 'right',
    },
  ];

  return (
    <div className="court-owner-dashboard">
      <Title level={3} className="dashboard-title">
        Thống Kê Tháng 6/2025
      </Title>

      {/* Thống kê tổng quan */}
      <Row gutter={[16, 16]} className="stats-row">
        <Col xs={24} sm={12} md={8} lg={6}>
          <Card loading={loading}>
            <Statistic
              title="Tổng Đơn Đặt"
              value={stats?.totalBooking || 0}
              prefix={<ArrowUpOutlined />}
              valueStyle={{ color: '#3f8600' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={8} lg={6}>
          <Card loading={loading}>
            <Statistic
              title="Tổng Doanh Thu"
              value={stats?.totalRevenue ? stats.totalRevenue / 1000000 : 0}
              precision={2}
              suffix="triệu"
              valueStyle={{ color: '#3f8600' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={8} lg={6}>
          <Card loading={loading}>
            <Statistic
              title="Đơn Hoàn Thành"
              value={stats?.completedBookings || 0}
              valueStyle={{ color: '#3f8600' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={8} lg={6}>
          <Card loading={loading}>
            <Statistic
              title="Đơn Đã Hủy"
              value={stats?.cancelledBookings || 0}
              valueStyle={{ color: '#cf1322' }}
              prefix={<ArrowDownOutlined />}
            />
          </Card>
        </Col>
      </Row>

      {/* Thống kê chi tiết */}
      <Row gutter={[16, 16]} className="detailed-stats-row">
        <Col xs={24} md={12}>
          <Card 
            title="Cơ Sở Hoạt Động Tốt Nhất" 
            loading={loading}
            className="top-facilities-card"
          >
            <Table
              columns={facilityColumns}
              dataSource={topFacilities}
              rowKey="facilityId"
              pagination={false}
              size="small"
            />
          </Card>
        </Col>
        <Col xs={24} md={12}>
          <Card 
            title="Thống Kê Khác" 
            loading={loading}
            className="additional-stats-card"
          >
            <div className="additional-stats">
              <div className="stat-item">
                <Text strong>Số Cơ Sở:</Text>
                <Text>{stats?.totalFacilities || 0}</Text>
              </div>
              <div className="stat-item">
                <Text strong>Tổng Số Sân:</Text>
                <Text>{stats?.totalCourts || 0}</Text>
              </div>
              <div className="stat-item">
                <Text strong>Khách Hàng Hoạt Động:</Text>
                <Text>{stats?.activeUsers || 0}</Text>
              </div>
              <div className="stat-item">
                <Text strong>Doanh Thu Trung Bình/Đơn:</Text>
                <Text>{stats?.averageRevenuePerBooking ? formatCurrency(stats.averageRevenuePerBooking) : 0}</Text>
              </div>
            </div>
          </Card>
        </Col>
      </Row>
    </div>
  );
};

export default CourtOwnerDashboard;