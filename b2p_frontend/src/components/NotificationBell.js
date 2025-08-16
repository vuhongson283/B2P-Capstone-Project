import React, { useState } from 'react';
import {
    Badge,
    Dropdown,
    Button,
    List,
    Typography,
    Empty,
    Divider,
    Space,
    Avatar,
    Tooltip
} from 'antd';
import {
    BellOutlined,
    CalendarOutlined,
    CheckCircleOutlined,
    CloseCircleOutlined,
    InfoCircleOutlined,
    DeleteOutlined,
    CheckOutlined,
    WifiOutlined
} from '@ant-design/icons';
import { useGlobalNotification } from '../contexts/GlobalNotificationContext';
import dayjs from 'dayjs';
import './NotificationBell.scss';

const { Text } = Typography;

const NotificationBell = () => {
    const {
        notifications,
        unreadCount,
        isConnected,
        markAsRead,
        markAllAsRead,
        clearAllNotifications
    } = useGlobalNotification();

    const [dropdownVisible, setDropdownVisible] = useState(false);

    const getNotificationIcon = (type) => {
        switch (type) {
            case 'booking_created':
                return <CalendarOutlined style={{ color: '#52c41a' }} />;
            case 'booking_completed':
                return <CheckCircleOutlined style={{ color: '#52c41a' }} />;
            case 'booking_cancelled':
                return <CloseCircleOutlined style={{ color: '#ff4d4f' }} />;
            default:
                return <InfoCircleOutlined style={{ color: '#1890ff' }} />;
        }
    };

    const handleNotificationClick = (notification) => {
        if (!notification.read) {
            markAsRead(notification.id);
        }

        // Navigate to booking management
        if (window.location.pathname !== '/court-owner/booking-management') {
            window.location.href = '/court-owner/booking-management';
        }

        setDropdownVisible(false);
    };

    const notificationContent = (
        <div className="notification-dropdown">
            <div className="notification-header">
                <div className="header-title">
                    <BellOutlined />
                    <span>Thông báo</span>
                    {unreadCount > 0 && (
                        <Badge count={unreadCount} size="small" />
                    )}
                </div>
                <Space>
                    {notifications.length > 0 && (
                        <>
                            <Tooltip title="Đánh dấu tất cả đã đọc">
                                <Button
                                    type="text"
                                    size="small"
                                    icon={<CheckOutlined />}
                                    onClick={(e) => {
                                        e.stopPropagation();
                                        markAllAsRead();
                                    }}
                                />
                            </Tooltip>
                            <Tooltip title="Xóa tất cả">
                                <Button
                                    type="text"
                                    size="small"
                                    icon={<DeleteOutlined />}
                                    onClick={(e) => {
                                        e.stopPropagation();
                                        clearAllNotifications();
                                    }}
                                    danger
                                />
                            </Tooltip>
                        </>
                    )}
                </Space>
            </div>

            <Divider style={{ margin: '8px 0' }} />

            <div className="notification-list">
                {notifications.length === 0 ? (
                    <Empty
                        description="Không có thông báo nào"
                        image={Empty.PRESENTED_IMAGE_SIMPLE}
                        style={{ padding: '20px' }}
                    />
                ) : (
                    <List
                        size="small"
                        dataSource={notifications.slice(0, 10)}
                        renderItem={(notification) => (
                            <List.Item
                                key={notification.id}
                                className={`notification-item ${notification.read ? 'read' : 'unread'}`}
                                onClick={() => handleNotificationClick(notification)}
                            >
                                <List.Item.Meta
                                    avatar={
                                        <Avatar
                                            icon={getNotificationIcon(notification.type)}
                                            size="small"
                                        />
                                    }
                                    title={
                                        <div className="notification-title">
                                            <Text strong={!notification.read}>
                                                {notification.title}
                                            </Text>
                                            {!notification.read && (
                                                <div className="unread-dot" />
                                            )}
                                        </div>
                                    }
                                    description={
                                        <div className="notification-description">
                                            <Text type="secondary">
                                                {notification.message}
                                            </Text>
                                            <br />
                                            <Text type="secondary" style={{ fontSize: '11px' }}>
                                                {dayjs(notification.timestamp).format('DD/MM/YYYY HH:mm')}
                                            </Text>
                                        </div>
                                    }
                                />
                            </List.Item>
                        )}
                    />
                )}
            </div>

            {notifications.length > 10 && (
                <div className="notification-footer">
                    <Text type="secondary" style={{ fontSize: '12px' }}>
                        Hiển thị 10/{notifications.length} thông báo gần nhất
                    </Text>
                </div>
            )}
        </div>
    );

    return (
        <Dropdown
            overlay={notificationContent}
            trigger={['click']}
            placement="bottomRight"
            visible={dropdownVisible}
            onVisibleChange={setDropdownVisible}
            overlayClassName="notification-dropdown-overlay"
        >
            <Badge count={unreadCount} size="small" offset={[-2, 2]}>
                <Button
                    type="text"
                    icon={<BellOutlined />}
                    className="notification-bell-button"
                    style={{
                        color: isConnected ? '#fff' : '#ffccc7',
                        fontSize: '18px',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        width: '40px',
                        height: '40px'
                    }}
                />
            </Badge>
        </Dropdown>
    );
};

export default NotificationBell;