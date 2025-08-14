import React, { createContext, useContext, useEffect, useState, useRef } from 'react';
import { notification as antdNotification } from 'antd';
import { MessageOutlined, UserOutlined, CalendarOutlined } from '@ant-design/icons';
import signalRService from '../services/signalRService';

const GlobalCommentNotificationContext = createContext();

export const useGlobalCommentNotification = () => {
    const context = useContext(GlobalCommentNotificationContext);
    if (!context) {
        throw new Error('useGlobalCommentNotification must be used within GlobalCommentNotificationProvider');
    }
    return context;
};

export const GlobalCommentNotificationProvider = ({ children, currentUser }) => {
    const [notifications, setNotifications] = useState([]);
    const shownNotifications = useRef(new Set());
    const isInitialized = useRef(false);

    // Cleanup old notifications
    const cleanupOldNotifications = () => {
        const now = Date.now();
        const CLEANUP_INTERVAL = 300000; // 5 minutes

        shownNotifications.current.forEach(id => {
            const timestamp = parseInt(id.split('-').pop());
            if (now - timestamp > CLEANUP_INTERVAL) {
                shownNotifications.current.delete(id);
            }
        });
    };

    // Handle new comment notification
    const handleNewComment = (commentData) => {
        console.log('🔔 New comment notification received:', commentData);
        if (window.blogCommentCountCallbacks && commentData.blogId) {
            const callbacks = window.blogCommentCountCallbacks.get(commentData.blogId);
            if (callbacks && callbacks.size > 0) {
                callbacks.forEach(callback => {
                    try {
                        callback(1); // Increment by 1
                        console.log(`📊 Updated comment count for blog ${commentData.blogId}`);
                    } catch (error) {
                        console.error('Error updating comment count:', error);
                    }
                });
            }
        }

        // Skip if it's current user's own comment
        if (commentData.userId === currentUser?.userId) {
            console.log('⏭️ Skipping own comment notification');
            return;
        }

        // Skip if not commenting on current user's blog
        if (commentData.blogAuthorId !== currentUser?.userId) {
            console.log('⏭️ Not current user\'s blog, skipping');
            return;
        }

        cleanupOldNotifications();

        // Create unique notification ID
        const notificationId = `comment-${commentData.commentId || Date.now()}-${Date.now()}`;

        // Check if already shown
        if (shownNotifications.current.has(notificationId)) {
            console.log('⏭️ Comment notification already shown');
            return;
        }

        // Mark as shown
        shownNotifications.current.add(notificationId);

        // Create notification content
        const notificationContent = (
            <div
                style={{ cursor: 'pointer' }}
                onClick={() => {
                    // Navigate to blog post
                    const url = `/blog#comment-${commentData.commentId}`;
                    if (window.location.pathname !== '/blog') {
                        window.location.href = url;
                    } else {
                        // If already on blog page, scroll to comment
                        const element = document.getElementById(`comment-${commentData.commentId}`);
                        if (element) {
                            element.scrollIntoView({ behavior: 'smooth', block: 'center' });
                        }
                    }
                    antdNotification.destroy(notificationId);
                }}
            >
                <div style={{
                    display: 'flex',
                    alignItems: 'center',
                    marginBottom: '8px',
                    fontWeight: 'bold',
                    color: '#1890ff'
                }}>
                    <MessageOutlined style={{ marginRight: '8px', fontSize: '16px' }} />
                    {commentData.isReply ? 'Trả lời bình luận mới' : 'Bình luận mới'}
                </div>

                <div style={{ fontSize: '14px', color: '#666', lineHeight: '1.4' }}>
                    <div>
                        <strong>👤 {commentData.userName || 'Người dùng'}:</strong>
                        <span style={{ marginLeft: '4px' }}>
                            {commentData.content?.length > 80
                                ? commentData.content.substring(0, 80) + '...'
                                : commentData.content
                            }
                        </span>
                    </div>

                    {commentData.isReply && commentData.parentComment && (
                        <div style={{
                            marginTop: '8px',
                            padding: '8px',
                            background: '#f5f5f5',
                            borderRadius: '4px',
                            fontSize: '12px'
                        }}>
                            <strong>↳ Trả lời:</strong> "{commentData.parentComment}"
                        </div>
                    )}

                    <div style={{ marginTop: '8px' }}>
                        <strong>📝 Bài viết:</strong>
                        <span style={{ marginLeft: '4px' }}>
                            {commentData.blogTitle?.length > 50
                                ? commentData.blogTitle.substring(0, 50) + '...'
                                : commentData.blogTitle
                            }
                        </span>
                    </div>

                    <div style={{ marginTop: '4px', fontSize: '12px', color: '#999' }}>
                        <CalendarOutlined style={{ marginRight: '4px' }} />
                        {new Date().toLocaleString('vi-VN')}
                    </div>
                </div>

                <div style={{
                    marginTop: '8px',
                    fontSize: '12px',
                    color: '#1890ff',
                    fontStyle: 'italic'
                }}>
                    💡 Click để xem chi tiết
                </div>
            </div>
        );

        // Show notification
        antdNotification.info({
            key: notificationId,
            message: notificationContent,
            duration: 12, // 12 seconds
            placement: 'topRight',
            style: {
                width: '420px',
                borderLeft: '4px solid #1890ff',
                marginTop: '10px',
                borderRadius: '8px',
                boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)'
            }
        });

        // Add to notifications list
        setNotifications(prev => [{
            id: notificationId,
            type: 'comment',
            data: commentData,
            timestamp: Date.now(),
            read: false
        }, ...prev.slice(0, 99)]); // Keep last 100 notifications
    };

    // SignalR connection and event listeners
    // GlobalCommentNotificationContext.js
    useEffect(() => {
        if (!currentUser?.userId || isInitialized.current) return;

        const initializeSignalR = async () => {
            try {
                console.log('🔄 Initializing comment notifications for user:', currentUser.userId);

                let connected = signalRService.connected;
                if (!connected) {
                    connected = await signalRService.start(currentUser.userId);
                } else {
                    await signalRService.joinUserGroup(currentUser.userId);
                }

                if (connected) {
                    // ✅ ADD: Debug logs cho tất cả events
                    console.log('🎧 Setting up event listeners...');

                    signalRService.on('onNewComment', (data) => {
                        console.log('🔔 [DEBUG] onNewComment received:', data);
                        handleNewComment(data);
                    });

                    signalRService.on('onCommentReply', (data) => {
                        console.log('🔔 [DEBUG] onCommentReply received:', data);
                        handleNewComment(data);
                    });

                    signalRService.on('onCommentNotification', (data) => {
                        console.log('🔔 [DEBUG] onCommentNotification received:', data);
                        handleNewComment(data);
                    });

                    // ✅ ADD: Test all possible SignalR events
                    if (signalRService.connection) {
                        signalRService.connection.on('NewComment', (data) => {
                            console.log('🔔 [DEBUG] Direct NewComment event:', data);
                            handleNewComment(data);
                        });

                        signalRService.connection.on('CommentReply', (data) => {
                            console.log('🔔 [DEBUG] Direct CommentReply event:', data);
                            handleNewComment(data);
                        });

                        signalRService.connection.on('CommentNotification', (data) => {
                            console.log('🔔 [DEBUG] Direct CommentNotification event:', data);
                            handleNewComment(data);
                        });

                        // ✅ Listen to ALL events for debugging
                        signalRService.connection.onEvent = (eventName, ...args) => {
                            console.log(`🔔 [DEBUG] SignalR event received: ${eventName}`, args);
                        };
                    }

                    console.log('✅ Comment notification system initialized');
                    isInitialized.current = true;
                } else {
                    console.warn('⚠️ Could not establish SignalR connection for comment notifications');
                }
            } catch (error) {
                console.error('❌ Error initializing comment notifications:', error);
            }
        };

        initializeSignalR();

        return () => {
            if (signalRService.connection && currentUser?.userId) {
                console.log('🧹 Cleaning up event listeners...');
                signalRService.off('onNewComment');
                signalRService.off('onCommentReply');
                signalRService.off('onCommentNotification');

                // Cleanup direct listeners
                if (signalRService.connection) {
                    signalRService.connection.off('NewComment');
                    signalRService.connection.off('CommentReply');
                    signalRService.connection.off('CommentNotification');
                }

                signalRService.leaveUserGroup(currentUser.userId);
                console.log('🧹 Comment notification listeners removed');
            }
            isInitialized.current = false;
        };
    }, [currentUser?.userId]);

    // Context value
    const value = {
        notifications,
        markAsRead: (notificationId) => {
            setNotifications(prev =>
                prev.map(notif =>
                    notif.id === notificationId
                        ? { ...notif, read: true }
                        : notif
                )
            );
        },
        clearAll: () => {
            setNotifications([]);
            shownNotifications.current.clear();
        },
        unreadCount: notifications.filter(notif => !notif.read).length
    };

    return (
        <GlobalCommentNotificationContext.Provider value={value}>
            {children}
        </GlobalCommentNotificationContext.Provider>
    );
};
if (!window.blogCommentCountCallbacks) {
    window.blogCommentCountCallbacks = new Map();
}

window.registerCommentCountCallback = (blogId, callback) => {
    if (!window.blogCommentCountCallbacks.has(blogId)) {
        window.blogCommentCountCallbacks.set(blogId, new Set());
    }
    window.blogCommentCountCallbacks.get(blogId).add(callback);
    console.log(`📝 Registered comment count callback for blog ${blogId}`);
};

window.unregisterCommentCountCallback = (blogId, callback) => {
    if (window.blogCommentCountCallbacks.has(blogId)) {
        window.blogCommentCountCallbacks.get(blogId).delete(callback);
        if (window.blogCommentCountCallbacks.get(blogId).size === 0) {
            window.blogCommentCountCallbacks.delete(blogId);
        }
    }
    console.log(`🗑️ Unregistered comment count callback for blog ${blogId}`);
};