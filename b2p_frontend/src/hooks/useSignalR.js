import { useEffect, useRef, useCallback } from 'react';
import { message, notification } from 'antd';
import signalRService from '../services/signalRService';

export const useSignalR = (options = {}) => {
    const {
        facilityId,
        onBookingCreated,
        onBookingUpdated,
        onBookingCompleted,
        onBookingCancelled,
        onConnectionChanged,
        showNotifications = true
    } = options;

    const previousFacilityId = useRef(null);
    const isInitialized = useRef(false);

    // Show notification helper
    const showBookingNotification = useCallback((notif) => {
        if (!showNotifications) return;

        const config = {
            message: 'Cập nhật đặt sân',
            duration: 4,
            placement: 'topRight',
        };

        switch (notif.action) {
            case 'created':
                notification.success({
                    ...config,
                    description: `Đơn đặt sân mới: ${notif.courtName} - ${notif.timeSlot}`,
                    message: '🆕 Đơn đặt sân mới',
                });
                break;
            case 'updated':
                notification.info({
                    ...config,
                    description: `Đơn đặt sân đã được cập nhật: ${notif.courtName} - ${notif.timeSlot}`,
                    message: '📝 Cập nhật đặt sân',
                });
                break;
            case 'completed':
                notification.success({
                    ...config,
                    description: `Đơn đặt sân đã hoàn thành: ${notif.courtName} - ${notif.timeSlot}`,
                    message: '✅ Hoàn thành đặt sân',
                });
                break;
            case 'cancelled':
                notification.warning({
                    ...config,
                    description: `Đơn đặt sân đã bị hủy: ${notif.courtName} - ${notif.timeSlot}`,
                    message: '❌ Hủy đặt sân',
                });
                break;
            default:
                break;
        }
    }, [showNotifications]);

    // Initialize SignalR connection
    useEffect(() => {
        if (isInitialized.current) return;

        const initializeSignalR = async () => {
            try {
                const connected = await signalRService.startConnection();
                if (!connected) {
                    message.warning('Không thể kết nối real-time. Một số tính năng có thể bị hạn chế.');
                }
            } catch (error) {
                console.error('Failed to initialize SignalR:', error);
                message.error('Lỗi kết nối real-time');
            }
        };

        initializeSignalR();
        isInitialized.current = true;

        return () => {
            signalRService.stopConnection();
        };
    }, []);

    // Handle facility group changes
    useEffect(() => {
        const handleFacilityChange = async () => {
            // Leave previous facility group
            if (previousFacilityId.current !== null) {
                await signalRService.leaveFacilityGroup(previousFacilityId.current);
            }

            // Join new facility group
            if (facilityId !== null && facilityId !== undefined) {
                await signalRService.joinFacilityGroup(facilityId);
                previousFacilityId.current = facilityId;
            }
        };

        if (signalRService.connected) {
            handleFacilityChange();
        } else {
            // Wait for connection to be established
            const checkConnection = setInterval(() => {
                if (signalRService.connected) {
                    handleFacilityChange();
                    clearInterval(checkConnection);
                }
            }, 1000);

            return () => clearInterval(checkConnection);
        }
    }, [facilityId]);

    // Setup event handlers
    useEffect(() => {
        const handleBookingCreated = (notification) => {
            showBookingNotification(notification);
            onBookingCreated?.(notification);
        };

        const handleBookingUpdated = (notification) => {
            showBookingNotification(notification);
            onBookingUpdated?.(notification);
        };

        const handleBookingCompleted = (notification) => {
            showBookingNotification(notification);
            onBookingCompleted?.(notification);
        };

        const handleBookingCancelled = (notification) => {
            showBookingNotification(notification);
            onBookingCancelled?.(notification);
        };

        const handleConnectionChanged = (isConnected) => {
            if (isConnected) {
                message.success('Kết nối real-time thành công', 2);
            } else {
                message.warning('Mất kết nối real-time', 2);
            }
            onConnectionChanged?.(isConnected);
        };

        const handleError = (error) => {
            message.error(`Lỗi real-time: ${error}`, 3);
        };

        // Register event handlers
        signalRService.on('onBookingCreated', handleBookingCreated);
        signalRService.on('onBookingUpdated', handleBookingUpdated);
        signalRService.on('onBookingCompleted', handleBookingCompleted);
        signalRService.on('onBookingCancelled', handleBookingCancelled);
        signalRService.on('onConnectionChanged', handleConnectionChanged);
        signalRService.on('onError', handleError);

        return () => {
            // Cleanup event handlers
            signalRService.off('onBookingCreated');
            signalRService.off('onBookingUpdated');
            signalRService.off('onBookingCompleted');
            signalRService.off('onBookingCancelled');
            signalRService.off('onConnectionChanged');
            signalRService.off('onError');
        };
    }, [
        onBookingCreated,
        onBookingUpdated,
        onBookingCompleted,
        onBookingCancelled,
        onConnectionChanged,
        showBookingNotification
    ]);

    return {
        isConnected: signalRService.connected,
        connectionState: signalRService.connectionState,
        sendBookingUpdate: signalRService.sendBookingUpdate.bind(signalRService),
    };
};

export default useSignalR;