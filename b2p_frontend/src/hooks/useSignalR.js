import { useEffect, useRef, useCallback } from 'react';
import { message, notification } from 'antd';
import signalRService from '../services/signalRService';

export const useSignalR = (options = {}) => {
    const {
        facilityId,
        facilityIds = [],
        userId = null, // ✅ Keep for future use but don't use for now
        onBookingCreated,
        onBookingUpdated,
        onBookingCompleted,
        onBookingCancelled,
        onConnectionChanged,
        showNotifications = true
    } = options;

    const previousFacilityIds = useRef([]);
    const isInitialized = useRef(false);
    const cleanupRef = useRef(null); // ✅ Track cleanup function

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

    // ✅ FIXED: Initialize SignalR connection only once globally
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

        // ✅ Only cleanup on page unload, not component unmount
        const handlePageUnload = () => {
            signalRService.stopConnection();
        };

        window.addEventListener('beforeunload', handlePageUnload);

        cleanupRef.current = () => {
            window.removeEventListener('beforeunload', handlePageUnload);
        };

        return cleanupRef.current;
    }, []);

    // ✅ UPDATED: Handle facility changes with better debouncing
    useEffect(() => {
        const handleFacilityChanges = async () => {
            const currentFacilityIds = facilityIds.length > 0 ? facilityIds : (facilityId ? [facilityId] : []);
            const prevFacilityIds = previousFacilityIds.current;

            // ✅ Skip if no change
            if (JSON.stringify(currentFacilityIds.sort()) === JSON.stringify(prevFacilityIds.sort())) {
                return;
            }

            console.log(`🔄 Facility IDs changed: [${prevFacilityIds.join(', ')}] -> [${currentFacilityIds.join(', ')}]`);

            // Leave old facility groups that are no longer needed
            const toLeave = prevFacilityIds.filter(id => !currentFacilityIds.includes(id));
            for (const id of toLeave) {
                await signalRService.leaveFacilityGroup(id);
                console.log(`📤 Left facility group: ${id}`);
            }

            // Join new facility groups
            const toJoin = currentFacilityIds.filter(id => !prevFacilityIds.includes(id));

            if (toJoin.length > 0) {
                await signalRService.joinUserFacilities(toJoin);
            }

            // Update previous state
            previousFacilityIds.current = [...currentFacilityIds];

            console.log(`🔗 Active facility groups: [${currentFacilityIds.join(', ')}]`);
        };

        // ✅ Add delay to ensure connection is stable
        const timeoutId = setTimeout(() => {
            if (signalRService.connected) {
                handleFacilityChanges();
            } else {
                // Wait for connection to be established
                const checkConnection = setInterval(() => {
                    if (signalRService.connected) {
                        handleFacilityChanges();
                        clearInterval(checkConnection);
                    }
                }, 1000);

                // Cleanup interval after 10 seconds
                setTimeout(() => clearInterval(checkConnection), 10000);
            }
        }, 200);

        return () => clearTimeout(timeoutId);
    }, [facilityId, facilityIds]);

    // ✅ REMOVED: User group handling since backend doesn't support it
    // useEffect(() => {
    //     // Remove user group logic
    // }, [userId]);

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
            // ✅ Only cleanup event handlers, not the connection
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
        getConnectionInfo: () => signalRService.getConnectionInfo(),
    };
};

export default useSignalR;