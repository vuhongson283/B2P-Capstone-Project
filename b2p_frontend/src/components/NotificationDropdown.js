import React from 'react';
import { Dropdown, Badge, Button, List, Typography, Empty, Divider } from 'antd';
import { BellOutlined, MessageOutlined, CalendarOutlined, DeleteOutlined, CheckOutlined } from '@ant-design/icons';
import { useGlobalCommentNotification } from '../contexts/GlobalCommentNotificationContext';

const { Text } = Typography;

const NotificationDropdown = () => {
    const {
        notifications,
        unreadCount,
        markAsRead,
        markAllAsRead,
        clearAllNotifications
    } = useGlobalCommentNotification();

    const formatTime = (timestamp) => {
        const now = Date.now();
        const diff = now - timestamp;

        const minute = 60 * 1000;
        const hour = 60 * minute;
        const day = 24 * hour;

        if (diff < minute) {
            return 'Vừa xong';
        } else if (diff < hour) {
            return `${Math.floor(diff / minute)} phút trước`;
        } else if (diff < day) {
            return `${Math.floor(diff / hour)} giờ trước`;
        } else {
            return `${Math.floor(diff / day)} ngày trước`;
        }
    };

    const handleNotificationClick = (notification) => {
        if (!notification.read) {
            markAsRead(notification.id);
        }

        // Navigate to comment
        const url = `/blog#comment-${notification.data.commentId}`;
        if (window.location.pathname !== '/blog') {
            window.location.href = url;
        } else {
            const element = document.getElementById(`comment-${notification.data.commentId}`);
            if (element) {
                element.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        }
    };

    const notificationMenu = (
        <div style={{ width: '400px', maxHeight: '500px', overflow: 'auto', backgroundColor: 'white', borderRadius: '8px', boxShadow: '0 4px 12px rgba(0,0,0,0.15)' }}>
            <div style={{ padding: '12px 16px', borderBottom: '1px solid #f0f0f0' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Text strong>💬 Thông báo bình luận</Text>
                    {unreadCount > 0 && (
                        <Button size="small" type="link" onClick={markAllAsRead}>
                            <CheckOutlined /> Đánh dấu tất cả đã đọc
                        </Button>
                    )}
                </div>
                {notifications.length > 0 && (
                    <Button
                        size="small"
                        type="text"
                        danger
                        onClick={clearAllNotifications}
                        style={{ marginTop: '4px' }}
                    >
                        <DeleteOutlined /> Xóa tất cả
                    </Button>
                )}
            </div>

            {notifications.length === 0 ? (
                <div style={{ padding: '40px 16px', textAlign: 'center' }}>
                    <Empty
                        description="Chưa có thông báo bình luận nào"
                        image={Empty.PRESENTED_IMAGE_SIMPLE}
                    />
                </div>
            ) : (
                <List
                    dataSource={notifications}
                    renderItem={(notification) => (
                        <List.Item
                            style={{
                                padding: '12px 16px',
                                backgroundColor: notification.read ? '#fff' : '#f6ffed',
                                cursor: 'pointer',
                                borderLeft: notification.read ? 'none' : '4px solid #52c41a',
                                borderBottom: '1px solid #f5f5f5'
                            }}
                            onClick={() => handleNotificationClick(notification)}
                        >
                            <div style={{ width: '100%' }}>
                                <div style={{ display: 'flex', alignItems: 'center', marginBottom: '4px' }}>
                                    <MessageOutlined style={{ color: '#1890ff', marginRight: '8px' }} />
                                    <Text strong style={{ color: '#1890ff', fontSize: '13px' }}>
                                        {notification.type === 'reply_to_my_comment'
                                            ? 'Trả lời bình luận'
                                            : 'Bình luận mới'
                                        }
                                    </Text>
                                    {!notification.read && (
                                        <div style={{
                                            width: '8px',
                                            height: '8px',
                                            backgroundColor: '#52c41a',
                                            borderRadius: '50%',
                                            marginLeft: '8px'
                                        }} />
                                    )}
                                </div>

                                <div style={{ fontSize: '14px', color: '#333', marginBottom: '4px' }}>
                                    <Text strong>{notification.data.userName}: </Text>
                                    <Text>
                                        {notification.data.content?.length > 60
                                            ? notification.data.content.substring(0, 60) + '...'
                                            : notification.data.content
                                        }
                                    </Text>
                                </div>

                                <div style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>
                                    <Text>📝 {notification.data.blogTitle}</Text>
                                </div>

                                <div style={{ fontSize: '11px', color: '#999', display: 'flex', alignItems: 'center' }}>
                                    <CalendarOutlined style={{ marginRight: '4px' }} />
                                    {formatTime(notification.timestamp)}
                                </div>
                            </div>
                        </List.Item>
                    )}
                />
            )}
        </div>
    );

    return (
        <Dropdown
            overlay={notificationMenu}
            trigger={['click']}
            placement="bottomRight"
        >
            <div style={{ position: 'relative' }}>
                <Badge count={unreadCount} size="small" offset={[-5, 5]}>
                    <Button
                        type="text"
                        icon={<BellOutlined style={{ color: 'white', fontSize: '18px' }} />}
                        style={{ border: 'none', background: 'transparent' }}
                    />
                </Badge>
            </div>
        </Dropdown>
    );
};

export default NotificationDropdown;