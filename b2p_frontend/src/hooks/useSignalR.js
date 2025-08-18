import { useEffect, useRef, useCallback } from 'react';
import { message, notification } from 'antd';
import { useLocation } from 'react-router-dom';
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

    const location = useLocation();
    const previousFacilityId = useRef(null);
    const localHandlersRef = useRef({});

    // ✅ Kiểm tra xem có phải trang court owner không
    const isCourtOwnerPage = location.pathname.startsWith('/court-owner');

    // Show notification helper
    const showBookingNotification = useCallback((notif) => {
        // ✅ Chỉ hiển thị thông báo ở trang court owner
        if (!showNotifications || !isCourtOwnerPage) {
            console.log('⏭️ Skipping notification - not on court owner page');
            return;
        }

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
                    message: '🆕 Đơn đặt sân mới (Local)',
                });
                break;
            case 'updated':
                notification.info({
                    ...config,
                    description: `Đơn đặt sân đã được cập nhật: ${notif.courtName} - ${notif.timeSlot}`,
                    message: '📝 Cập nhật đặt sân (Local)',
                });
                break;
            case 'completed':
                notification.success({
                    ...config,
                    description: `Đơn đặt sân đã hoàn thành: ${notif.courtName} - ${notif.timeSlot}`,
                    message: '✅ Hoàn thành đặt sân (Local)',
                });
                break;
            case 'cancelled':
                notification.warning({
                    ...config,
                    description: `Đơn đặt sân đã bị hủy: ${notif.courtName} - ${notif.timeSlot}`,
                    message: '❌ Hủy đặt sân (Local)',
                });
                break;
            default:
                break;
        }
    }, [showNotifications, isCourtOwnerPage]);

    useEffect(() => {
        const ensureConnection = async () => {
            await signalRService.ensureConnection();
        };

        ensureConnection();
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

    // ✅ Setup LOCAL callbacks với điều kiện kiểm tra trang
    useEffect(() => {
        // ✅ Chỉ setup callbacks nếu ở trang court owner
        if (!isCourtOwnerPage) {
            console.log('⏭️ Not on court owner page, skipping SignalR local setup');
            return;
        }

        // Only register if we have specific callbacks
        if (!onBookingCreated && !onBookingUpdated && !onBookingCompleted && !onBookingCancelled && !onConnectionChanged) {
            console.log('🔄 LOCAL: No callbacks provided, skipping');
            return;
        }

        console.log('📝 LOCAL: Setting up local callbacks...');

        // ✅ Create wrapper functions that call both global and local
        const createCombinedHandler = (eventName, localCallback) => {
            return (notification) => {
                // ✅ ALWAYS call local callback for component-specific logic
                if (localCallback) {
                    console.log(`🔔 LOCAL: ${eventName} received!`, notification);
                    localCallback(notification);
                }
            };
        };

        // ✅ Store local handlers
        if (onBookingCreated) {
            localHandlersRef.current.onBookingCreated = createCombinedHandler('Booking created', (notification) => {
                showBookingNotification(notification);
                onBookingCreated(notification);
            });
        }

        if (onBookingUpdated) {
            localHandlersRef.current.onBookingUpdated = createCombinedHandler('Booking updated', (notification) => {
                showBookingNotification(notification);
                onBookingUpdated(notification);
            });
        }

        if (onBookingCompleted) {
            localHandlersRef.current.onBookingCompleted = createCombinedHandler('Booking completed', (notification) => {
                showBookingNotification(notification);
                onBookingCompleted(notification);
            });
        }

        if (onBookingCancelled) {
            localHandlersRef.current.onBookingCancelled = createCombinedHandler('Booking cancelled', (notification) => {
                showBookingNotification(notification);
                onBookingCancelled(notification);
            });
        }

        if (onConnectionChanged) {
            localHandlersRef.current.onConnectionChanged = (isConnected) => {
                console.log('🔗 LOCAL: Connection changed:', isConnected);
                if (isConnected) {
                    message.success('Kết nối real-time thành công', 2);
                } else {
                    message.warning('Mất kết nối real-time', 2);
                }
                onConnectionChanged(isConnected);
            };
        }

        // ✅ HOOK INTO EXISTING SIGNALR EVENTS instead of overriding
        const originalOnMessage = signalRService.connection?.on;
        if (originalOnMessage && localHandlersRef.current.onBookingCreated) {
            // Add additional listener for BookingCreated
            signalRService.connection.on('BookingCreated', localHandlersRef.current.onBookingCreated);
        }

        if (originalOnMessage && localHandlersRef.current.onBookingUpdated) {
            signalRService.connection.on('BookingUpdated', localHandlersRef.current.onBookingUpdated);
        }

        if (originalOnMessage && localHandlersRef.current.onBookingCompleted) {
            signalRService.connection.on('BookingCompleted', localHandlersRef.current.onBookingCompleted);
        }

        if (originalOnMessage && localHandlersRef.current.onBookingCancelled) {
            signalRService.connection.on('BookingCancelled', localHandlersRef.current.onBookingCancelled);
        }

        // ✅ For connection changes, add to SignalR service handlers
        if (localHandlersRef.current.onConnectionChanged) {
            const originalConnectionHandler = signalRService.eventHandlers.onConnectionChanged;
            signalRService.eventHandlers.onConnectionChanged = (isConnected) => {
                // Call original first
                if (originalConnectionHandler) {
                    originalConnectionHandler(isConnected);
                }
                // Then call local
                localHandlersRef.current.onConnectionChanged(isConnected);
            };
        }

        console.log('✅ LOCAL: Local callbacks setup complete');

        return () => {
            console.log('🧹 LOCAL: Cleaning up local callbacks...');

            // ✅ Remove local SignalR connection event listeners
            if (signalRService.connection && localHandlersRef.current.onBookingCreated) {
                signalRService.connection.off('BookingCreated', localHandlersRef.current.onBookingCreated);
            }
            if (signalRService.connection && localHandlersRef.current.onBookingUpdated) {
                signalRService.connection.off('BookingUpdated', localHandlersRef.current.onBookingUpdated);
            }
            if (signalRService.connection && localHandlersRef.current.onBookingCompleted) {
                signalRService.connection.off('BookingCompleted', localHandlersRef.current.onBookingCompleted);
            }
            if (signalRService.connection && localHandlersRef.current.onBookingCancelled) {
                signalRService.connection.off('BookingCancelled', localHandlersRef.current.onBookingCancelled);
            }

            // ✅ Don't restore connection handler - let global keep working
            localHandlersRef.current = {};
            console.log('✅ LOCAL: Local cleanup complete (global preserved)');
        };
    }, [
        onBookingCreated,
        onBookingUpdated,
        onBookingCompleted,
        onBookingCancelled,
        onConnectionChanged,
        showBookingNotification,
        isCourtOwnerPage
    ]);

    return {
        isConnected: signalRService.connected,
        connectionState: signalRService.connectionState,
        sendBookingUpdate: signalRService.sendBookingUpdate.bind(signalRService),
    };
};

export default useSignalR;