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
        this.joinedGroups = new Set();
        this.currentUserId = null;
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
                skipNegotiation: false,
                transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
                accessTokenFactory: () => {
                    return localStorage.getItem('authToken') || localStorage.getItem('token') || '';
                }
            })
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: (retryContext) => {
                    if (retryContext.previousRetryCount < 3) {
                        return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
                    } else {
                        return null;
                    }
                }
            })
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.setupEventHandlers();
    }

    setupEventHandlers() {
        if (!this.connection || !this.isEnabled) return;

        this.connection.onclose((error) => {
            this.isConnected = false;
            this.joinedGroups.clear();
            console.log('SignalR connection closed:', error);
            this.eventHandlers.onConnectionChanged?.(false);

            if (error && !this.isNetworkError(error)) {
                this.handleReconnection();
            }
        });

        this.connection.onreconnecting((error) => {
            console.log('SignalR reconnecting:', error);
            this.isConnected = false;
            this.joinedGroups.clear();
            this.eventHandlers.onConnectionChanged?.(false);
        });

        this.connection.onreconnected(async () => {
            console.log('SignalR reconnected successfully');
            this.isConnected = true;
            this.reconnectAttempts = 0;
            await this.rejoinAllGroups();
            this.eventHandlers.onConnectionChanged?.(true);
        });

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

        this.connection.on('ReceiveBookingUpdate', (notification) => {
            console.log('🔄 Received booking update:', notification);
            this.eventHandlers.onBookingUpdated?.(notification);
        });

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
                this.joinedGroups.clear();
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

    async joinUserFacilities(facilityIds) {
        if (!this.connection || !this.isConnected || !this.isEnabled || !Array.isArray(facilityIds)) {
            return;
        }

        try {
            for (const facilityId of facilityIds) {
                await this.connection.invoke('JoinFacilityGroup', facilityId);
                this.joinedGroups.add(`facility_${facilityId}`);
                console.log(`📍 Joined facility group: ${facilityId}`);
            }
            console.log(`✅ Joined ${facilityIds.length} facility groups`);
        } catch (error) {
            console.error('Error joining facility groups:', error);
        }
    }

    async joinFacilityGroup(facilityId) {
        if (this.connection && this.isConnected && this.isEnabled) {
            try {
                await this.connection.invoke('JoinFacilityGroup', facilityId);
                this.joinedGroups.add(`facility_${facilityId}`);
                console.log(`📍 Joined facility group: ${facilityId}`);
            } catch (error) {
                console.error('Error joining facility group:', error);
            }
        }
    }

    async leaveFacilityGroup(facilityId) {
        if (this.connection && this.isConnected && this.isEnabled) {
            try {
                await this.connection.invoke('LeaveFacilityGroup', facilityId);
                this.joinedGroups.delete(`facility_${facilityId}`);
                console.log(`📤 Left facility group: ${facilityId}`);
            } catch (error) {
                console.error('Error leaving facility group:', error);
            }
        }
    }

    async rejoinAllGroups() {
        if (!this.connection || !this.isConnected || !this.isEnabled) {
            return;
        }

        const groupsToRejoin = Array.from(this.joinedGroups);
        this.joinedGroups.clear();

        try {
            for (const group of groupsToRejoin) {
                if (group.startsWith('facility_')) {
                    const facilityId = group.replace('facility_', '');
                    await this.joinFacilityGroup(facilityId);
                }
            }
            console.log(`✅ Rejoined ${groupsToRejoin.length} groups after reconnection`);
        } catch (error) {
            console.error('Error rejoining groups:', error);
        }
    }

    on(event, handler) {
        this.eventHandlers[event] = handler;
    }

    off(event) {
        delete this.eventHandlers[event];
    }

    get connectionState() {
        return this.connection?.state || signalR.HubConnectionState.Disconnected;
    }

    get connected() {
        return this.isConnected && this.isEnabled;
    }

    get enabled() {
        return this.isEnabled;
    }

    get joinedGroupsInfo() {
        return Array.from(this.joinedGroups);
    }

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

    getConnectionInfo() {
        return {
            isEnabled: this.isEnabled,
            isConnected: this.isConnected,
            connectionState: this.connectionState,
            reconnectAttempts: this.reconnectAttempts,
            hubUrl: process.env.REACT_APP_SIGNALR_HUB_URL,
            joinedGroups: this.joinedGroupsInfo,
            currentUserId: this.currentUserId
        };
    }
}

export const signalRService = new SignalRService();
export default signalRService;