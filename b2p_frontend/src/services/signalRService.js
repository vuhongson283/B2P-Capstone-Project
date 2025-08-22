import * as signalR from "@microsoft/signalr";

// DEBUG ENVIRONMENT VARIABLES
console.log('🔧 DEBUG SignalR Environment:');
console.log('REACT_APP_SIGNALR_HUB_URL:', process.env.REACT_APP_SIGNALR_HUB_URL);
console.log('REACT_APP_API_BASE_URL:', process.env.REACT_APP_API_BASE_URL);
console.log('All REACT_APP env vars:', Object.keys(process.env).filter(key => key.startsWith('REACT_APP')));

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
        this.keepAliveInterval = null;
        this.initializeConnection();
    }

    initializeConnection() {
        // ✅ FORCE URL để debug - HARDCODE THAY VÌ ĐỌC TỪ ENV
        const hubUrl = 'https://localhost:5000/bookinghub';

        console.log('🔧 FORCED SignalR URL (ignoring env):', hubUrl);
        console.log('🔧 Original env URL was:', process.env.REACT_APP_SIGNALR_HUB_URL);

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

        // ✅ EXISTING: Booking events
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

        // ✅ NEW: Comment notification events
        this.connection.on('NewComment', (notification) => {
            console.log('💬 New comment notification:', notification);
            this.eventHandlers.onNewComment?.(notification);
        });

        this.connection.on('CommentReply', (notification) => {
            console.log('↩️ Comment reply notification:', notification);
            this.eventHandlers.onCommentReply?.(notification);
        });

        this.connection.on('CommentNotification', (notification) => {
            console.log('🔔 Comment notification received:', notification);
            this.eventHandlers.onCommentNotification?.(notification);
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

                this.startKeepAlive();
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
                this.stopKeepAlive();
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

    // ✅ NEW: User group management for comment notifications
    async joinUserGroup(userId) {
        if (!this.connection || !this.isConnected || !this.isEnabled) {
            console.log('Cannot join user group - SignalR not connected');
            return;
        }

        try {
            const groupKey = `user_${userId}`;
            if (this.joinedGroups.has(groupKey)) {
                console.log(`Already joined user group: ${userId}`);
                return;
            }

            await this.connection.invoke('JoinUserGroup', userId.toString());
            this.joinedGroups.add(groupKey);
            this.currentUserId = userId;
            console.log(`👤 Joined user group: ${userId}`);
        } catch (error) {
            console.error('❌ Error joining user group:', error);
        }
    }

    async leaveUserGroup(userId) {
        if (!this.connection || !this.isConnected || !this.isEnabled) {
            return;
        }

        try {
            const groupKey = `user_${userId}`;
            await this.connection.invoke('LeaveUserGroup', userId.toString());
            this.joinedGroups.delete(groupKey);
            console.log(`📤 Left user group: ${userId}`);

            if (this.currentUserId === userId) {
                this.currentUserId = null;
            }
        } catch (error) {
            console.error('❌ Error leaving user group:', error);
            // Remove from local set anyway
            this.joinedGroups.delete(`user_${userId}`);
        }
    }

    // ✅ NEW: Send comment notification
    async sendCommentNotification(notification) {
        if (!this.connection || !this.isConnected || !this.isEnabled) {
            console.log('Cannot send comment notification - SignalR not connected');
            return;
        }

        try {
            // Add timestamp if not present
            if (!notification.timestamp) {
                notification.timestamp = new Date().toISOString();
            }

            await this.connection.invoke('SendCommentNotification', notification);
            console.log('📤 Comment notification sent via SignalR:', notification);
        } catch (error) {
            console.error('❌ Error sending comment notification:', error);
        }
    }

    // ✅ EXISTING: Facility group management
    async joinUserFacilities(facilityIds) {
        if (!this.connection || !this.isConnected || !this.isEnabled || !Array.isArray(facilityIds)) {
            return;
        }

        try {
            for (const facilityId of facilityIds) {
                if (!this.joinedGroups.has(`facility_${facilityId}`)) {
                    await this.connection.invoke('JoinFacilityGroup', facilityId);
                    this.joinedGroups.add(`facility_${facilityId}`);
                    console.log(`📍 Joined facility group: ${facilityId}`);
                    await new Promise(resolve => setTimeout(resolve, 50));
                }
            }
            console.log(`✅ Joined ${facilityIds.length} facility groups`);
        } catch (error) {
            console.error('Error joining facility groups:', error);
        }
    }

    async leaveAllFacilityGroups() {
        if (!this.connection || !this.isConnected || !this.isEnabled) {
            return;
        }

        try {
            const facilityGroups = Array.from(this.joinedGroups).filter(group => group.startsWith('facility_'));

            for (const group of facilityGroups) {
                const facilityId = group.replace('facility_', '');
                try {
                    await this.connection.invoke('LeaveFacilityGroup', facilityId);
                    this.joinedGroups.delete(group);
                    console.log(`📤 Left facility group: ${facilityId}`);
                    await new Promise(resolve => setTimeout(resolve, 50));
                } catch (error) {
                    console.warn(`⚠️ Could not leave facility group ${facilityId}:`, error.message);
                    this.joinedGroups.delete(group);
                }
            }
        } catch (error) {
            console.error('Error leaving facility groups:', error);
            const facilityGroups = Array.from(this.joinedGroups).filter(group => group.startsWith('facility_'));
            facilityGroups.forEach(group => this.joinedGroups.delete(group));
        }
    }

    async joinFacilityGroup(facilityId) {
        if (this.connection && this.isConnected && this.isEnabled) {
            const groupKey = `facility_${facilityId}`;
            if (this.joinedGroups.has(groupKey)) {
                console.log(`Already joined facility group: ${facilityId}`);
                return;
            }

            try {
                await this.connection.invoke('JoinFacilityGroup', facilityId);
                this.joinedGroups.add(groupKey);
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
                } else if (group.startsWith('user_')) {
                    const userId = group.replace('user_', '');
                    await this.joinUserGroup(parseInt(userId));
                }
                await new Promise(resolve => setTimeout(resolve, 50));
            }
            console.log(`✅ Rejoined ${groupsToRejoin.length} groups after reconnection`);
        } catch (error) {
            console.error('Error rejoining groups:', error);
        }
    }

    // ✅ NEW: Start SignalR with user setup
    async start(userId = null) {
        try {
            const connected = await this.startConnection();

            if (connected && userId) {
                // Join user group for comment notifications
                await this.joinUserGroup(userId);
            }

            return connected;
        } catch (error) {
            console.error('Error starting SignalR with user setup:', error);
            return false;
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

    async ensureConnection() {
        if (!this.isEnabled) {
            return false;
        }

        if (!this.isConnected || this.connection.state === signalR.HubConnectionState.Disconnected) {
            console.log('🔄 Ensuring SignalR connection...');
            return await this.startConnection();
        }

        return true;
    }

    startKeepAlive() {
        if (this.keepAliveInterval) {
            clearInterval(this.keepAliveInterval);
        }

        this.keepAliveInterval = setInterval(async () => {
            if (this.isEnabled && this.connection) {
                if (this.connection.state === signalR.HubConnectionState.Disconnected) {
                    console.log('🔄 Keep-alive: Reconnecting...');
                    await this.startConnection();
                }
            }
        }, 10000);
    }

    stopKeepAlive() {
        if (this.keepAliveInterval) {
            clearInterval(this.keepAliveInterval);
            this.keepAliveInterval = null;
        }
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
            hubUrl: 'https://localhost:5000/bookinghub', // HARDCODED for debug
            joinedGroups: this.joinedGroupsInfo,
            currentUserId: this.currentUserId
        };
    }
}

export const signalRService = new SignalRService();
export default signalRService;