// contexts/SignalRContext.js
import { createContext, useContext, useEffect, useState } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

const SignalRContext = createContext();

export const SignalRProvider = ({ children }) => {
    const [connection, setConnection] = useState(null);
    const [isConnected, setIsConnected] = useState(false);
    const [connectionStatus, setConnectionStatus] = useState('Disconnected');

    useEffect(() => {
        console.log('🚀 SignalRProvider: Initializing connection...');
        
        const connectToSignalR = async () => {
            try {
                console.log('🔌 Creating SignalR connection to: https://localhost:7227/bookinghub');
                
                const newConnection = new HubConnectionBuilder()
                    .withUrl('https://ccce5ebbfdd9.ngrok-free.app/bookinghub')
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
                newConnection.on('BookingStatusChanged', (data) => {
                    console.log('🎯 [SignalRProvider] RAW BookingStatusChanged received:', data);
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
                console.log('🔌 Trying HTTP connection to: http://localhost:7227/bookinghub');
                
                const httpConnection = new HubConnectionBuilder()
                    .withUrl('http://localhost:7227/bookinghub')
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