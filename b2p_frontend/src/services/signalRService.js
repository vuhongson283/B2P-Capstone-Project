import * as signalR from "@microsoft/signalr";

class SignalRService {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectInterval = 5000;
        this.eventHandlers = {};
        this.isEnabled = false;
        this.initializeConnection();
    }

    initializeConnection() {
        const hubUrl = process.env.REACT_APP_SIGNALR_HUB_URL;

        if (!hubUrl || hubUrl === '') {
            console.log('SignalR is disabled - no URL configured');
            this.isEnabled = false;
            return;
        }

        this.isEnabled = true;
        console.log('Initializing SignalR connection to:', hubUrl);

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, {
                // Cấu hình cho localhost HTTPS
                skipNegotiation: false,
                transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
                accessTokenFactory: () => {
                    // Thêm token nếu API cần authentication
                    return localStorage.getItem('authToken') || localStorage.getItem('token') || '';
                }
            })
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: (retryContext) => {
                    if (retryContext.previousRetryCount < 3) {
                        return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
                    } else {
                        return null; // Stop reconnecting
                    }
                }
            })
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.setupEventHandlers();
    }

    setupEventHandlers() {
        if (!this.connection || !this.isEnabled) return;

        // Connection events
        this.connection.onclose((error) => {
            this.isConnected = false;
            console.log('SignalR connection closed:', error);
            this.eventHandlers.onConnectionChanged?.(false);

            // Chỉ reconnect nếu không phải lỗi SSL hoặc network
            if (error && !this.isNetworkError(error)) {
                this.handleReconnection();
            }
        });

        this.connection.onreconnecting((error) => {
            console.log('SignalR reconnecting:', error);
            this.isConnected = false;
            this.eventHandlers.onConnectionChanged?.(false);
        });

        this.connection.onreconnected(() => {
            console.log('SignalR reconnected successfully');
            this.isConnected = true;
            this.reconnectAttempts = 0;
            this.eventHandlers.onConnectionChanged?.(true);
        });

        // Booking events - theo tên từ backend .NET
        this.connection.on('BookingCreated', (notification) => {
            console.log('📅 New booking created:', notification);
            this.eventHandlers.onBookingCreated?.(notification);
        });

        this.connection.on('BookingUpdated', (notification) => {
            console.log('📝 Booking updated:', notification);
            this.eventHandlers.onBookingUpdated?.(notification);
        });

        this.connection.on('BookingCompleted', (notification) => {
            console.log('✅ Booking completed:', notification);
            this.eventHandlers.onBookingCompleted?.(notification);
        });

        this.connection.on('BookingCancelled', (notification) => {
            console.log('❌ Booking cancelled:', notification);
            this.eventHandlers.onBookingCancelled?.(notification);
        });

        // Generic booking update
        this.connection.on('ReceiveBookingUpdate', (notification) => {
            console.log('🔄 Received booking update:', notification);
            this.eventHandlers.onBookingUpdated?.(notification);
        });

        // Error handling
        this.connection.on('Error', (error) => {
            console.error('SignalR hub error:', error);
            this.eventHandlers.onError?.(error);
        });
    }

    isNetworkError(error) {
        const errorMessage = error?.message || error?.toString() || '';
        return (
            errorMessage.includes('ERR_NAME_NOT_RESOLVED') ||
            errorMessage.includes('ERR_CONNECTION_REFUSED') ||
            errorMessage.includes('ERR_CERT') ||
            errorMessage.includes('Failed to fetch') ||
            errorMessage.includes('SSL')
        );
    }

    async startConnection() {
        if (!this.isEnabled || !this.connection) {
            console.log('SignalR is disabled, skipping connection');
            return false;
        }

        try {
            if (this.connection.state === signalR.HubConnectionState.Disconnected) {
                console.log('Starting SignalR connection...');
                await this.connection.start();
                this.isConnected = true;
                this.reconnectAttempts = 0;
                console.log('✅ SignalR connected successfully!');
                this.eventHandlers.onConnectionChanged?.(true);
                return true;
            }
            return this.isConnected;
        } catch (error) {
            console.error('❌ Failed to start SignalR connection:', error);

            // Nếu là lỗi network, disable SignalR
            if (this.isNetworkError(error)) {
                console.log('Network error detected, disabling SignalR temporarily...');
                this.isEnabled = false;
                this.eventHandlers.onConnectionChanged?.(false);
                return false;
            }

            this.eventHandlers.onError?.(`Connection failed: ${error.message}`);
            this.handleReconnection();
            return false;
        }
    }

    async stopConnection() {
        if (this.connection && this.isEnabled) {
            try {
                await this.connection.stop();
                this.isConnected = false;
                console.log('SignalR connection stopped');
                this.eventHandlers.onConnectionChanged?.(false);
            } catch (error) {
                console.error('Error stopping SignalR connection:', error);
            }
        }
    }

    async handleReconnection() {
        if (!this.isEnabled || this.reconnectAttempts >= this.maxReconnectAttempts) {
            if (this.reconnectAttempts >= this.maxReconnectAttempts) {
                console.log('Max reconnection attempts reached');
                this.eventHandlers.onError?.('Không thể kết nối lại server. Vui lòng kiểm tra kết nối.');
            }
            return;
        }

        this.reconnectAttempts++;
        const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), 30000);
        console.log(`Attempting to reconnect in ${delay}ms... (${this.reconnectAttempts}/${this.maxReconnectAttempts})`);

        setTimeout(async () => {
            await this.startConnection();
        }, delay);
    }

    // Join facility group để nhận updates cho facility cụ thể
    async joinFacilityGroup(facilityId) {
        if (this.connection && this.isConnected && this.isEnabled) {
            try {
                await this.connection.invoke('JoinFacilityGroup', facilityId);
                console.log(`📍 Joined facility group: ${facilityId}`);
            } catch (error) {
                console.error('Error joining facility group:', error);
            }
        }
    }

    // Leave facility group
    async leaveFacilityGroup(facilityId) {
        if (this.connection && this.isConnected && this.isEnabled) {
            try {
                await this.connection.invoke('LeaveFacilityGroup', facilityId);
                console.log(`📤 Left facility group: ${facilityId}`);
            } catch (error) {
                console.error('Error leaving facility group:', error);
            }
        }
    }

    // Event handler registration
    on(event, handler) {
        this.eventHandlers[event] = handler;
    }

    off(event) {
        delete this.eventHandlers[event];
    }

    // Getters
    get connectionState() {
        return this.connection?.state || signalR.HubConnectionState.Disconnected;
    }

    get connected() {
        return this.isConnected && this.isEnabled;
    }

    get enabled() {
        return this.isEnabled;
    }

    // Send update to other clients
    async sendBookingUpdate(notification) {
        if (this.connection && this.isConnected && this.isEnabled) {
            try {
                await this.connection.invoke('SendBookingUpdate', notification);
                console.log('📤 Sent booking update:', notification);
            } catch (error) {
                console.error('Error sending booking update:', error);
            }
        } else {
            console.log('SignalR not connected, update not sent');
        }
    }

    // Get connection info for debugging
    getConnectionInfo() {
        return {
            isEnabled: this.isEnabled,
            isConnected: this.isConnected,
            connectionState: this.connectionState,
            reconnectAttempts: this.reconnectAttempts,
            hubUrl: process.env.REACT_APP_SIGNALR_HUB_URL
        };
    }
}

// Export singleton instance
export const signalRService = new SignalRService();
export default signalRService;