// contexts/SignalRContext.js
import { createContext, useContext, useEffect, useState } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import signalRService from '../services/signalRService'; // ✅ ADD THIS
import dayjs from 'dayjs';

const SignalRContext = createContext();

export const SignalRProvider = ({ children }) => {
    const [connection, setConnection] = useState(null);
    const [isConnected, setIsConnected] = useState(false);
    const [connectionStatus, setConnectionStatus] = useState('Disconnected');

    useEffect(() => {
        console.log('🚀 SignalRProvider: Initializing connection...');

        const connectToSignalR = async () => {
            try {
                console.log('🔌 Creating SignalR connection to: https://api.book2play.site/bookinghub');

                const newConnection = new HubConnectionBuilder()
                    .withUrl('https://api.book2play.site/bookinghub')
                    .configureLogging(LogLevel.Information)
                    .withAutomaticReconnect({
                        nextRetryDelayInMilliseconds: retryContext => {
                            console.log(`🔄 Retry attempt ${retryContext.previousRetryCount + 1}`);
                            // Retry after 0, 2, 10, 30 seconds, then stop
                            if (retryContext.previousRetryCount < 4) {
                                return Math.pow(2, retryContext.previousRetryCount) * 1000;
                            } else {
                                return null; // Stop retrying
                            }
                        }
                    })
                    .build();

                // Connection event handlers
                newConnection.onreconnecting(error => {
                    console.log('🔄 SignalR Reconnecting...', error);
                    setIsConnected(false);
                    setConnectionStatus('Reconnecting');
                });

                newConnection.onreconnected(connectionId => {
                    console.log('✅ SignalR Reconnected with ID:', connectionId);
                    setIsConnected(true);
                    setConnectionStatus('Connected');
                });

                newConnection.onclose(error => {
                    console.log('❌ SignalR Connection Closed:', error);
                    setIsConnected(false);
                    setConnectionStatus('Disconnected');
                });

                // Start connection
                console.log('🔌 Starting SignalR connection...');
                setConnectionStatus('Connecting');
                await newConnection.start();

                console.log('✅ SignalR Connected successfully!');
                console.log('✅ Connection ID:', newConnection.connectionId);
                console.log('✅ Connection State:', newConnection.state);

                setConnection(newConnection);
                setIsConnected(true);
                setConnectionStatus('Connected');

                // Test the connection by listening to a general event
                // Replace the entire BookingStatusChanged handler with this:
                newConnection.on('BookingStatusChanged', async (data) => {
                    console.log('🎯 [SignalRProvider] RAW BookingStatusChanged received:', data);

                    try {
                        const bookingId = data.bookingId || data.BookingId;
                        const cusName = data.customerName || data.CustomerName;




                        // ✅ REMOVE API CALL - USE SIGNALR DATA DIRECTLY
                        console.log('🔍 [DEBUG] Processing SignalR data directly:', data);
                        console.log('🔍 [DEBUG] Data keys:', Object.keys(data));
                        console.log('🔍 [DEBUG] Status-related fields:', {
                            status: data.status,
                            newStatus: data.newStatus,
                            statusId: data.statusId,
                            newStatusId: data.newStatusId,
                            action: data.action
                        });

                        // ✅ CHECK IF THIS IS A CANCELLATION FIRST
                        const isCancellation =
                            data.status === 'cancelled' ||
                            data.newStatus === 'cancelled' ||
                            data.status === 'Cancelled' ||
                            data.newStatus === 'Cancelled' ||
                            data.statusId === 9 ||
                            data.newStatusId === 9 ||
                            data.action === 'cancelled' ||
                            data.action === 'cancel';

                        if (isCancellation) {
                            console.log('🚨 [SignalRProvider] CANCELLATION detected:', data);

                            const cancellationNotification = {
                                bookingId: bookingId,
                                courtName: data.courtName || data.CourtName || `Booking #${bookingId}`,
                                customerName: data.customerName || data.CustomerName || 'Khách hàng',
                                date: data.date || data.Date || dayjs().format('DD/MM/YYYY'),
                                timeSlot: data.timeSlot || data.TimeSlot || 'Unknown time',
                                totalAmount: data.totalAmount || data.TotalAmount || 0,
                                status: 'cancelled',
                                statusId: 9,
                                action: 'cancelled',
                                reason: data.reason || data.Reason || 'Hủy đơn'
                            };

                            console.log('🎯 [DEBUG] Cancellation notification:', cancellationNotification);

                            // ✅ DEBOUNCE FOR CANCELLATION
                            const eventKey = `cancel-${bookingId}`;

                            if (window.lastCancelEvents && window.lastCancelEvents[eventKey]) {
                                const timeSinceLastEvent = Date.now() - window.lastCancelEvents[eventKey];
                                if (timeSinceLastEvent < 3000) {
                                    console.log('⏭️ Ignoring duplicate cancellation event within 3 seconds');
                                    return;
                                }
                            }

                            if (!window.lastCancelEvents) window.lastCancelEvents = {};
                            window.lastCancelEvents[eventKey] = Date.now();

                            setTimeout(() => {
                                if (window.lastCancelEvents && window.lastCancelEvents[eventKey]) {
                                    delete window.lastCancelEvents[eventKey];
                                }
                            }, 10000);

                            // Trigger cancellation notification
                            if (signalRService.eventHandlers.onBookingCancelled) {
                                signalRService.eventHandlers.onBookingCancelled(cancellationNotification);
                            }

                            return; // Don't process as payment
                        }

                        // ✅ OTHERWISE, PROCESS AS PAYMENT (using SignalR data only)
                        console.log('💰 [SignalRProvider] Processing as PAYMENT:', data);

                        const paymentNotification = {
                            bookingId: bookingId,
                            courtName: data.courtName || data.CourtName || `Booking #${bookingId}`,
                            customerName: cusName,
                            date: data.date || data.Date || dayjs().format('DD/MM/YYYY'),
                            timeSlot: data.timeSlot || data.TimeSlot,
                            totalAmount: data.totalAmount || data.TotalAmount,
                            status: 'paid',
                            statusId: 7,
                            action: 'paid'
                        };

                        console.log('🎯 [DEBUG] Payment notification:', paymentNotification);

                        // ✅ DEBOUNCE FOR PAYMENT
                        const paymentEventKey = `payment-${bookingId}`;

                        if (window.lastPaymentEvents && window.lastPaymentEvents[paymentEventKey]) {
                            const timeSinceLastEvent = Date.now() - window.lastPaymentEvents[paymentEventKey];
                            if (timeSinceLastEvent < 3000) {
                                console.log('⏭️ Ignoring duplicate payment event within 3 seconds');
                                return;
                            }
                        }

                        if (!window.lastPaymentEvents) window.lastPaymentEvents = {};
                        window.lastPaymentEvents[paymentEventKey] = Date.now();

                        setTimeout(() => {
                            if (window.lastPaymentEvents && window.lastPaymentEvents[paymentEventKey]) {
                                delete window.lastPaymentEvents[paymentEventKey];
                            }
                        }, 10000);

                        // Trigger payment notification
                        if (signalRService.eventHandlers.onBookingPaid) {
                            signalRService.eventHandlers.onBookingPaid(paymentNotification);
                        }

                    } catch (error) {
                        console.error('❌ Error handling BookingStatusChanged:', error);
                        console.error('❌ Error message:', error.message);
                        console.error('❌ Error stack:', error.stack);

                        // ✅ FALLBACK: Still trigger notification with basic info
                        const bookingId = data.bookingId || data.BookingId;
                        const fallbackNotification = {
                            bookingId: bookingId,
                            courtName: `Booking #${bookingId}`,
                            customerName: 'Khách hàng',
                            date: dayjs().format('DD/MM/YYYY'),
                            timeSlot: 'Unknown time',
                            totalAmount: 0,
                            status: 'paid',
                            statusId: 7,
                            action: 'paid'
                        };

                        // Still trigger notification even if processing fails
                        if (signalRService.eventHandlers.onBookingPaid) {
                            signalRService.eventHandlers.onBookingPaid(fallbackNotification);
                        }
                    }
                });
            } catch (error) {
                console.error('❌ SignalR Connection Error:', error);
                console.error('❌ Error details:', error.message);
                console.error('❌ Error stack:', error.stack);
                setConnectionStatus('Failed');

                // Fallback to HTTP if HTTPS fails
                if (error.message.includes('SSL') || error.message.includes('certificate') ||
                    error.message.includes('HTTPS') || error.message.includes('ERR_CERT')) {
                    console.log('💡 HTTPS failed, trying HTTP fallback...');
                    tryHttpConnection();
                } else {
                    console.log('💡 Trying HTTP fallback anyway...');
                    tryHttpConnection();
                }
            }
        };

        const tryHttpConnection = async () => {
            try {
                console.log('🔌 Trying HTTP connection to: http://api.book2play.site/bookinghub');

                const httpConnection = new HubConnectionBuilder()
                    .withUrl('http://api.book2play.site/bookinghub')
                    .configureLogging(LogLevel.Information)
                    .withAutomaticReconnect()
                    .build();

                await httpConnection.start();
                console.log('✅ SignalR Connected via HTTP');
                console.log('✅ HTTP Connection ID:', httpConnection.connectionId);

                setConnection(httpConnection);
                setIsConnected(true);
                setConnectionStatus('Connected');

                // Test the HTTP connection
                httpConnection.on('BookingStatusChanged', (data) => {
                    console.log('🎯 [SignalRProvider HTTP] RAW BookingStatusChanged received:', data);
                });

            } catch (httpError) {
                console.error('❌ HTTP connection also failed:', httpError);
                console.error('❌ HTTP Error details:', httpError.message);
                setConnectionStatus('Failed');
            }
        };

        connectToSignalR();

        // Cleanup
        return () => {
            if (connection) {
                console.log('🔌 [SignalRProvider] Cleaning up SignalR connection...');
                connection.stop();
            }
        };
    }, []); // Remove connection dependency to avoid infinite loop

    // Helper method to subscribe to events with cleanup
    const useSignalREvent = (eventName, handler, dependencies = []) => {
        useEffect(() => {
            if (!connection || !isConnected) {
                console.log(`❌ Cannot subscribe to ${eventName}: no connection or not connected`);
                return;
            }

            console.log(`📡 [useSignalREvent] Subscribing to event: ${eventName}`);
            console.log(`📡 [useSignalREvent] Connection state: ${connection.state}`);
            console.log(`📡 [useSignalREvent] Is connected: ${isConnected}`);

            connection.on(eventName, handler);

            return () => {
                console.log(`📡 [useSignalREvent] Unsubscribing from event: ${eventName}`);
                if (connection) {
                    connection.off(eventName, handler);
                }
            };
        }, [connection, isConnected, eventName, ...dependencies]);
    };

    const contextValue = {
        connection,
        isConnected,
        connectionStatus,
        useSignalREvent // Helper for components
    };

    console.log('🔍 [SignalRProvider] Current state:', {
        hasConnection: !!connection,
        isConnected,
        connectionStatus,
        connectionId: connection?.connectionId,
        connectionState: connection?.state
    });

    return (
        <SignalRContext.Provider value={contextValue}>
            {children}
        </SignalRContext.Provider>
    );
};

// Custom hook to use SignalR
export const useSignalR = () => {
    const context = useContext(SignalRContext);
    if (!context) {
        throw new Error('useSignalR must be used within SignalRProvider');
    }

    console.log('🎯 [useSignalR] Hook called with context:', {
        hasConnection: !!context.connection,
        isConnected: context.isConnected,
        connectionStatus: context.connectionStatus
    });

    return context;
};

// Custom hook for booking-specific events
export const useBookingSignalR = (bookingId) => {
    console.log('🎯 [useBookingSignalR] Hook called with bookingId:', bookingId);

    const { connection, isConnected, useSignalREvent, connectionStatus } = useSignalR();
    const [bookingUpdates, setBookingUpdates] = useState(null);

    console.log('🎯 [useBookingSignalR] Current state:', {
        bookingId,
        hasConnection: !!connection,
        isConnected,
        connectionStatus
    });

    // Listen for booking status changes
    useSignalREvent(
        'BookingStatusChanged',
        (data) => {
            console.log('📨 [useBookingSignalR] Received BookingStatusChanged:', data);
            console.log('📨 [useBookingSignalR] Current bookingId:', bookingId);
            console.log('📨 [useBookingSignalR] Received BookingId:', data.BookingId);
            console.log('📨 [useBookingSignalR] BookingId type check:', typeof data.BookingId, typeof bookingId);

            // Only update if it's for the current booking
            if (data.BookingId && bookingId && data.BookingId.toString() === bookingId.toString()) {
                console.log(`✅ [useBookingSignalR] Booking ${bookingId} status updated:`, data);
                setBookingUpdates(data);
            } else {
                console.log(`❌ [useBookingSignalR] BookingId mismatch or missing data:`, {
                    dataBookingId: data.BookingId,
                    currentBookingId: bookingId,
                    match: data.BookingId && bookingId && data.BookingId.toString() === bookingId.toString()
                });
            }
        },
        [bookingId]
    );

    return {
        connection,
        isConnected,
        connectionStatus,
        bookingUpdates,
        clearUpdates: () => setBookingUpdates(null)
    };
};