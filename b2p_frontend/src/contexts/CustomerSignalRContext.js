import React, { createContext, useContext, useEffect, useState, useCallback, useRef } from 'react';
import { message } from 'antd';
import signalRService from '../services/signalRService';

const CustomerSignalRContext = createContext();

export const CustomerSignalRProvider = ({ children }) => {
    const [isConnected, setIsConnected] = useState(false);
    const [connectionState, setConnectionState] = useState('Disconnected');
    const [joinedFacilities, setJoinedFacilities] = useState(new Set());

    // Handle connection change
    const handleConnectionChanged = useCallback((connected) => {
        setIsConnected(connected);
        setConnectionState(connected ? 'Connected' : 'Disconnected');
        console.log('🔗 Customer SignalR connection:', connected ? 'Connected' : 'Disconnected');
    }, []);

    // Initialize SignalR for customer
    useEffect(() => {
        const initializeCustomerSignalR = async () => {
            console.log('🚀 Initializing Customer SignalR...');

            // Setup event handlers
            signalRService.on('onConnectionChanged', handleConnectionChanged);

            // Start connection
            const connected = await signalRService.startConnection();

            if (connected) {
                setIsConnected(true);
                setConnectionState('Connected');
                console.log('✅ Customer SignalR initialized successfully');
            } else {
                setIsConnected(false);
                setConnectionState('Disconnected');
                console.log('❌ Customer SignalR failed to initialize');
            }
        };

        initializeCustomerSignalR();

        return () => {
            // Cleanup
            signalRService.off('onConnectionChanged');
            console.log('🧹 Customer SignalR cleanup completed');
        };
    }, [handleConnectionChanged]);

    // Join facility group for customer
    const joinFacilityForUpdates = useCallback(async (facilityId) => {
        if (!facilityId || joinedFacilities.has(facilityId)) return;

        try {
            if (signalRService.connected) {
                await signalRService.joinFacilityGroup(facilityId);
                setJoinedFacilities(prev => new Set([...prev, facilityId]));
                console.log(`👤 Customer joined facility group: ${facilityId}`);
            }
        } catch (error) {
            console.error('Error joining facility group for customer:', error);
        }
    }, [joinedFacilities]);

    // Leave facility group for customer
    const leaveFacilityUpdates = useCallback(async (facilityId) => {
        if (!facilityId || !joinedFacilities.has(facilityId)) return;

        try {
            if (signalRService.connected) {
                await signalRService.leaveFacilityGroup(facilityId);
                setJoinedFacilities(prev => {
                    const newSet = new Set(prev);
                    newSet.delete(facilityId);
                    return newSet;
                });
                console.log(`👤 Customer left facility group: ${facilityId}`);
            }
        } catch (error) {
            console.error('Error leaving facility group for customer:', error);
        }
    }, [joinedFacilities]);

    const contextValue = {
        isConnected,
        connectionState,
        joinFacilityForUpdates,
        leaveFacilityUpdates,
        joinedFacilities: Array.from(joinedFacilities)
    };

    return (
        <CustomerSignalRContext.Provider value={contextValue}>
            {children}
        </CustomerSignalRContext.Provider>
    );
};

export const useCustomerSignalR = () => {
    const context = useContext(CustomerSignalRContext);
    if (!context) {
        throw new Error('useCustomerSignalR must be used within CustomerSignalRProvider');
    }
    return context;
};

export default CustomerSignalRContext;