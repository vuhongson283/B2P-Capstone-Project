import React, { createContext, useContext, useEffect, useState, useCallback, useRef } from 'react';
import { notification as antdNotification } from 'antd';
import {
    CalendarOutlined,
    CheckCircleOutlined,
    CloseCircleOutlined,
    InfoCircleOutlined
} from '@ant-design/icons';
import signalRService from '../services/signalRService';
import dayjs from 'dayjs';

const GlobalNotificationContext = createContext(null);

export const useGlobalNotification = () => {
    const context = useContext(GlobalNotificationContext);
    if (!context) {
        throw new Error('useGlobalNotification must be used within GlobalNotificationProvider');
    }
    return context;
};

export const GlobalNotificationProvider = ({ children, userId, facilityIds = [] }) => {
    const [notifications, setNotifications] = useState([]);
    const [unreadCount, setUnreadCount] = useState(0);
    const [isConnected, setIsConnected] = useState(false);
    const shownNotifications = useRef(new Set());
    const lastProcessedTime = useRef(0);
    const isInitialized = useRef(false);
    const globalHandlersSet = useRef(false); // ✅ NEW: Track if global handlers are set

    // Cleanup old notification IDs
    const cleanupOldNotifications = useCallback(() => {
        const now = Date.now();
        const CLEANUP_INTERVAL = 30000;

        if (now - lastProcessedTime.current > CLEANUP_INTERVAL) {
            shownNotifications.current.clear();
            lastProcessedTime.current = now;
        }
    }, []);

    // ✅ PERSISTENT Global booking created handler
    const handleGlobalBookingCreated = useCallback((notification) => {
        console.log('🔔 GLOBAL: Booking created received!', notification);

        cleanupOldNotifications();

        const notificationId = `global-booking-created-${notification.bookingId}-${notification.facilityId}-${Date.now()}`;

        if (shownNotifications.current.has(notificationId)) {
            console.log('⏭️ Global notification already shown, skipping:', notificationId);
            return;
        }
        shownNotifications.current.add(notificationId);

        const dateText = notification.date ||
            (notification.checkInDate && dayjs(notification.checkInDate).isValid()
                ? dayjs(notification.checkInDate).format('DD/MM/YYYY')
                : '');

        const timeText = notification.timeSlot ||
            (notification.checkInTime && notification.checkOutTime
                ? `${notification.checkInTime} - ${notification.checkOutTime}`
                : notification.checkInTime || '');

        const notificationContent = (
            <div style={{ cursor: 'pointer' }}>
                <div style={{
                    display: 'flex',
                    alignItems: 'center',
                    marginBottom: '8px',
                    fontWeight: 'bold',
                    color: '#52c41a'
                }}>
                    <CalendarOutlined style={{ marginRight: '8px', fontSize: '16px' }} />
                    Đơn đặt sân mới
                </div>
                <div style={{ fontSize: '14px', color: '#666', lineHeight: '1.4' }}>
                    <div><strong>Sân:</strong> {notification.courtName || 'Sân thể thao'}</div>
                    <div><strong>Khách hàng:</strong> {notification.customerName || (notification.customerEmail?.split('@')[0]) || 'Admin'}</div>
                    <div><strong>Thời gian:</strong> {dateText}{timeText ? ` • ${timeText}` : ''}</div>
                    {notification.totalAmount && (
                        <div><strong>Tổng tiền:</strong> {Number(notification.totalAmount).toLocaleString('vi-VN')} VND</div>
                    )}
                </div>
                <div style={{ marginTop: '8px', fontSize: '12px', color: '#52c41a', fontStyle: 'italic' }}>
                    💡 Click để xem chi tiết
                </div>
            </div>
        );

        antdNotification.success({
            key: notificationId,
            message: notificationContent,
            duration: 8,
            placement: 'topRight',
            style: {
                width: '420px',
                borderLeft: '4px solid #52c41a',
                marginTop: '10px'
            },
            onClick: () => {
                // ✅ FIXED: Mở modal chi tiết
                console.log('🔔 Global notification clicked, opening detail modal');

                // Store notification data globally
                window.globalNotificationDetail = notification;

                // Trigger custom event
                window.dispatchEvent(new CustomEvent('openNotificationDetail', {
                    detail: notification
                }));

                // Close notification
                antdNotification.destroy(notificationId);
            }
        });

        const newNotification = {
            id: notificationId,
            type: 'booking_created',
            title: 'Đơn đặt sân mới',
            message: `${notification.courtName || 'Sân thể thao'} - ${dateText} ${timeText}`,
            data: notification,
            timestamp: new Date(),
            read: false
        };

        setNotifications(prev => [newNotification, ...prev.slice(0, 49)]);
        setUnreadCount(prev => prev + 1);

        console.log('✅ GLOBAL notification added to list');

    }, []);

    // ✅ PERSISTENT Global booking updated handler
    const handleGlobalBookingUpdated = useCallback((notification) => {
        console.log('🔔 GLOBAL: Booking updated received!', notification);

        cleanupOldNotifications();

        const notificationId = `global-booking-updated-${notification.bookingId}-${notification.action}-${Date.now()}`;

        if (shownNotifications.current.has(notificationId)) {
            console.log('⏭️ Global update notification already shown, skipping:', notificationId);
            return;
        }
        shownNotifications.current.add(notificationId);

        let icon = <InfoCircleOutlined />;
        let color = '#1890ff';
        let title = 'Cập nhật đặt sân';

        if (notification.action === 'completed') {
            icon = <CheckCircleOutlined />;
            color = '#52c41a';
            title = 'Đơn đặt sân hoàn thành';
        } else if (notification.action === 'cancelled') {
            icon = <CloseCircleOutlined />;
            color = '#ff4d4f';
            title = 'Đơn đặt sân đã hủy';
        }

        const notificationContent = (
            <div style={{ cursor: 'pointer' }}>
                <div style={{
                    display: 'flex',
                    alignItems: 'center',
                    marginBottom: '8px',
                    fontWeight: 'bold',
                    color: color
                }}>
                    {icon}
                    <span style={{ marginLeft: '8px' }}>{title} (Global)</span>
                </div>
                <div style={{ fontSize: '14px', color: '#666', lineHeight: '1.4' }}>
                    <div><strong>Mã booking:</strong> #{notification.bookingId}</div>
                    <div><strong>Sân:</strong> {notification.courtName || 'Sân thể thao'}</div>
                    <div><strong>Thời gian:</strong> {notification.date} • {notification.timeSlot}</div>
                </div>
            </div>
        );

        antdNotification.info({
            key: notificationId,
            message: notificationContent,
            duration: 5,
            placement: 'topRight',
            style: {
                width: '380px',
                borderLeft: `4px solid ${color}`,
                marginTop: '10px'
            },
            onClick: () => {
                handleNotificationClick(notification);
            }
        });

        const newNotification = {
            id: notificationId,
            type: 'booking_updated',
            title: title,
            message: `${notification.courtName || 'Sân thể thao'} - ${notification.date} ${notification.timeSlot}`,
            data: notification,
            timestamp: new Date(),
            read: false
        };

        setNotifications(prev => [newNotification, ...prev.slice(0, 49)]);
        setUnreadCount(prev => prev + 1);

        console.log('✅ GLOBAL update notification added to list');

    }, []);

    const handleConnectionChanged = useCallback((connected) => {
        console.log(`🔗 GLOBAL connection status changed: ${connected ? 'Connected' : 'Disconnected'}`);
        setIsConnected(connected);
    }, []);

    const handleNotificationClick = useCallback((notification) => {
        console.log('🔔 Global notification clicked:', notification);

        if (window.location.pathname !== '/court-owner/booking-management') {
            window.location.href = '/court-owner/booking-management';
        }
    }, []);

    const markAsRead = useCallback((notificationId) => {
        setNotifications(prev =>
            prev.map(notif =>
                notif.id === notificationId
                    ? { ...notif, read: true }
                    : notif
            )
        );
        setUnreadCount(prev => Math.max(0, prev - 1));
    }, []);

    const markAllAsRead = useCallback(() => {
        setNotifications(prev =>
            prev.map(notif => ({ ...notif, read: true }))
        );
        setUnreadCount(0);
    }, []);

    const clearAllNotifications = useCallback(() => {
        setNotifications([]);
        setUnreadCount(0);
    }, []);

    // ✅ FIXED: Set global handlers ONCE and never cleanup
    useEffect(() => {
        if (globalHandlersSet.current) {
            console.log('🔄 GLOBAL: Handlers already set, skipping');
            return;
        }

        console.log('🚀 GLOBAL: Setting up PERMANENT global handlers...');

        // ✅ Set handlers directly on signalRService
        signalRService.on('onBookingCreated', handleGlobalBookingCreated);
        signalRService.on('onBookingUpdated', handleGlobalBookingUpdated);
        signalRService.on('onBookingCompleted', handleGlobalBookingUpdated);
        signalRService.on('onBookingCancelled', handleGlobalBookingUpdated);
        signalRService.on('onConnectionChanged', handleConnectionChanged);

        globalHandlersSet.current = true;
        console.log('✅ GLOBAL: Permanent handlers set');

        // ✅ NO CLEANUP - Let handlers persist across page changes

    }, [handleGlobalBookingCreated, handleGlobalBookingUpdated, handleConnectionChanged]);

    // ✅ Initialize connection ONCE
    useEffect(() => {
        if (isInitialized.current) return;
        isInitialized.current = true;

        console.log('🚀 GLOBAL: Initializing SignalR connection...');

        const initializeSignalR = async () => {
            try {
                // Start connection
                const connected = await signalRService.startConnection();
                console.log(`🔗 GLOBAL SignalR connection status: ${connected}`);
                setIsConnected(connected);

                if (connected && facilityIds.length > 0) {
                    setTimeout(async () => {
                        await signalRService.joinUserFacilities(facilityIds);
                        console.log('🔗 GLOBAL: Joined facilities:', facilityIds);
                    }, 500);
                }

            } catch (error) {
                console.error('❌ Failed to initialize GLOBAL SignalR:', error);
            }
        };

        initializeSignalR();
    }, []);

    // ✅ Update facility groups when facilityIds change
    useEffect(() => {
        if (isConnected && facilityIds.length > 0 && isInitialized.current) {
            console.log('🔄 GLOBAL: Updating facility groups...', facilityIds);
            const timeoutId = setTimeout(() => {
                signalRService.joinUserFacilities(facilityIds);
                console.log('🔄 Updated facility groups:', facilityIds);
            }, 300);

            return () => clearTimeout(timeoutId);
        }
    }, [facilityIds, isConnected]);

    // ✅ Monitor connection state changes periodically
    useEffect(() => {
        const interval = setInterval(() => {
            const currentConnected = signalRService.connected;
            if (currentConnected !== isConnected) {
                console.log(`🔄 GLOBAL: Connection state sync: ${currentConnected}`);
                setIsConnected(currentConnected);
            }
        }, 2000);

        return () => clearInterval(interval);
    }, [isConnected]);

    const contextValue = {
        notifications,
        unreadCount,
        isConnected,
        markAsRead,
        markAllAsRead,
        clearAllNotifications,
        getConnectionInfo: () => signalRService.getConnectionInfo()
    };

    return (
        <GlobalNotificationContext.Provider value={contextValue}>
            {children}
        </GlobalNotificationContext.Provider>
    );
};