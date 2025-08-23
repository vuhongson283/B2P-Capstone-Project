import React, { createContext, useContext, useState, useEffect, useRef } from 'react';
import { notification as antdNotification } from 'antd';
import { MessageOutlined, CalendarOutlined } from '@ant-design/icons';
import { useSignalR } from './SignalRContext';

const GlobalCommentNotificationContext = createContext();

export const useGlobalCommentNotification = () => {
    const context = useContext(GlobalCommentNotificationContext);
    if (!context) {
        throw new Error('useGlobalCommentNotification must be used within GlobalCommentNotificationProvider');
    }
    return context;
};

export const GlobalCommentNotificationProvider = ({ children, currentUser }) => {
    const { connection } = useSignalR();
    const [notifications, setNotifications] = useState([]);
    const [unreadCount, setUnreadCount] = useState(0);
    const shownNotifications = useRef(new Set());

    // ✅ LOAD NOTIFICATIONS FROM LOCALSTORAGE ON MOUNT
    useEffect(() => {
        if (!currentUser?.userId) return;

        try {
            const storageKey = `notifications_user_${currentUser.userId}`;
            const savedNotifications = localStorage.getItem(storageKey);

            if (savedNotifications) {
                const parsed = JSON.parse(savedNotifications);

                // Filter notifications less than 7 days old
                const sevenDaysAgo = Date.now() - (7 * 24 * 60 * 60 * 1000);
                const validNotifications = parsed.filter(notif => notif.timestamp > sevenDaysAgo);

                setNotifications(validNotifications);

                // Calculate unread count
                const unreadCount = validNotifications.filter(notif => !notif.read).length;
                setUnreadCount(unreadCount);

                console.log(`📱 Loaded ${validNotifications.length} notifications from localStorage (${unreadCount} unread)`);

                // Clean up old notifications from localStorage
                if (validNotifications.length !== parsed.length) {
                    localStorage.setItem(storageKey, JSON.stringify(validNotifications));
                    console.log(`🧹 Cleaned up old notifications`);
                }
            }
        } catch (error) {
            console.error('❌ Error loading notifications from localStorage:', error);
        }
    }, [currentUser?.userId]);

    // ✅ SAVE NOTIFICATIONS TO LOCALSTORAGE WHEN UPDATED
    useEffect(() => {
        if (!currentUser?.userId || notifications.length === 0) return;

        try {
            const storageKey = `notifications_user_${currentUser.userId}`;
            localStorage.setItem(storageKey, JSON.stringify(notifications));
            console.log(`💾 Saved ${notifications.length} notifications to localStorage`);
        } catch (error) {
            console.error('❌ Error saving notifications to localStorage:', error);
        }
    }, [notifications, currentUser?.userId]);

    // ✅ INITIALIZE SIGNALR NOTIFICATIONS
    useEffect(() => {
        if (!currentUser?.userId) {
            console.log('⏳ No current user, skipping notification initialization');
            return;
        }

        console.log('🔄 Initializing comment notifications for user:', currentUser.userId);

        const initializeSignalR = async () => {
            try {
                if (!connection) {
                    console.log('⏳ SignalR connection not ready, waiting...');
                    return;
                }

                console.log('🎧 Setting up event listeners...');

                setupEventListeners();

                await joinUserGroup();

                console.log('✅ Comment notification system initialized');
            } catch (error) {
                console.error('❌ Error initializing SignalR for comment notifications:', error);
            }
        };

        const cleanup = () => {
            console.log('🧹 Cleaning up event listeners...');

            if (connection) {
                connection.off('CommentNotification', handleNewComment);
                connection.off('NewComment', handleNewComment);
                connection.off('CommentReply', handleNewComment);
            }

            console.log('🧹 Comment notification listeners removed');
        };

        const setupEventListeners = () => {
            if (!connection) return;

            connection.on('CommentNotification', handleNewComment);
            connection.on('NewComment', handleNewComment);
            connection.on('CommentReply', handleNewComment);

            console.log('🎧 Event listeners set up for comment notifications');
        };

        const joinUserGroup = async () => {
            try {
                if (connection && connection.state === 'Connected') {
                    await connection.invoke('JoinUserGroup', currentUser.userId.toString());
                    console.log(`👥 Joined user group: ${currentUser.userId}`);
                }
            } catch (error) {
                console.error('❌ Error joining user group:', error);
            }
        };

        initializeSignalR();

        // Cleanup on unmount or user change
        return cleanup;

    }, [connection, currentUser?.userId]);

    // ✅ HANDLE NEW COMMENT NOTIFICATION
    const handleNewComment = (commentData) => {
        console.log('🔔 New comment notification received:', commentData);
        console.log('🔔 Current user ID:', currentUser?.userId);
        console.log('🔔 Comment data analysis:', {
            commentUserId: commentData.userId,
            blogAuthorId: commentData.blogAuthorId,
            parentCommentUserId: commentData.parentCommentUserId,
            isReply: commentData.isReply
        });

        // Update comment count for blog
        if (window.blogCommentCountCallbacks && commentData.blogId) {
            const callbacks = window.blogCommentCountCallbacks.get(commentData.blogId);
            if (callbacks && callbacks.size > 0) {
                callbacks.forEach(callback => {
                    try {
                        callback(1);
                        console.log(`📊 Updated comment count for blog ${commentData.blogId}`);
                    } catch (error) {
                        console.error('Error updating comment count:', error);
                    }
                });
            }
        }

        // Skip own comments
        if (commentData.userId === currentUser?.userId) {
            console.log('⏭️ Skipping own comment notification');
            return;
        }

        // Determine if should notify
        let shouldNotify = false;
        let notificationType = '';

        const currentUserIdStr = String(currentUser?.userId);
        const blogAuthorIdStr = String(commentData.blogAuthorId);
        const parentCommentUserIdStr = String(commentData.parentCommentUserId || '');

        if (commentData.isReply && parentCommentUserIdStr === currentUserIdStr && parentCommentUserIdStr !== '') {
            shouldNotify = true;
            notificationType = 'reply_to_my_comment';
            console.log('✅ Notification type: Reply to my comment');
        } else if (!commentData.isReply && blogAuthorIdStr === currentUserIdStr) {
            shouldNotify = true;
            notificationType = 'comment_on_my_blog';
            console.log('✅ Notification type: Comment on my blog');
        } else {
            console.log('⏭️ Not relevant to current user, skipping notification');
            return;
        }

        if (!shouldNotify) {
            return;
        }

        // ✅ ADD TO PERSISTENT NOTIFICATIONS LIST FIRST
        const notificationId = `comment-${commentData.commentId || Date.now()}-${Date.now()}`;
        const newNotification = {
            id: notificationId,
            type: notificationType,
            data: commentData,
            timestamp: Date.now(),
            read: false
        };

        // Add to notifications list (this will trigger localStorage save)
        setNotifications(prev => [newNotification, ...prev.slice(0, 99)]);
        setUnreadCount(prev => prev + 1);

        console.log(`💾 Added notification to persistent storage:`, newNotification);

        // ✅ SHOW POPUP NOTIFICATION
        cleanupOldNotifications();

        // Check if already shown in this session
        if (shownNotifications.current.has(notificationId)) {
            console.log('⏭️ Comment notification already shown in this session');
            return;
        }

        // Mark as shown in this session
        shownNotifications.current.add(notificationId);

        // Create notification content
        const getNotificationContent = () => {
            const baseContent = (
                <div
                    style={{ cursor: 'pointer' }}
                    onClick={() => {
                        const url = `/blog#comment-${commentData.commentId}`;
                        if (window.location.pathname !== '/blog') {
                            window.location.href = url;
                        } else {
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
                        {notificationType === 'reply_to_my_comment'
                            ? 'Có người trả lời bình luận của bạn'
                            : 'Bình luận mới trên bài viết của bạn'
                        }
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

            return baseContent;
        };

        // Show popup notification
        antdNotification.info({
            key: notificationId,
            message: getNotificationContent(),
            duration: 15,
            placement: 'topRight',
            style: {
                width: '450px',
                borderLeft: notificationType === 'reply_to_my_comment'
                    ? '4px solid #52c41a'
                    : '4px solid #1890ff',
                marginTop: '10px',
                borderRadius: '8px',
                boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)'
            }
        });

        console.log(`✅ Notification shown for ${notificationType}:`, commentData);
    };

    // ✅ MARK NOTIFICATION AS READ
    const markAsRead = (notificationId) => {
        setNotifications(prev =>
            prev.map(notif =>
                notif.id === notificationId
                    ? { ...notif, read: true }
                    : notif
            )
        );

        setUnreadCount(prev => Math.max(0, prev - 1));
        console.log(`✅ Marked notification ${notificationId} as read`);
    };

    // ✅ MARK ALL AS READ
    const markAllAsRead = () => {
        setNotifications(prev =>
            prev.map(notif => ({ ...notif, read: true }))
        );
        setUnreadCount(0);
        console.log(`✅ Marked all notifications as read`);
    };

    // ✅ CLEAR ALL NOTIFICATIONS
    const clearAllNotifications = () => {
        setNotifications([]);
        setUnreadCount(0);

        if (currentUser?.userId) {
            const storageKey = `notifications_user_${currentUser.userId}`;
            localStorage.removeItem(storageKey);
        }

        console.log(`🗑️ Cleared all notifications`);
    };

    // ✅ CLEANUP OLD POPUP NOTIFICATIONS
    const cleanupOldNotifications = () => {
        const oneHourAgo = Date.now() - (60 * 60 * 1000);
        const oldNotificationIds = Array.from(shownNotifications.current).filter(id => {
            const timestamp = parseInt(id.split('-').pop());
            return timestamp < oneHourAgo;
        });

        oldNotificationIds.forEach(id => {
            shownNotifications.current.delete(id);
            antdNotification.destroy(id);
        });

        if (oldNotificationIds.length > 0) {
            console.log(`🧹 Cleaned up ${oldNotificationIds.length} old popup notifications`);
        }
    };

    // ✅ REGISTER COMMENT COUNT CALLBACK
    const registerCommentCountCallback = (blogId, callback) => {
        if (!window.blogCommentCountCallbacks) {
            window.blogCommentCountCallbacks = new Map();
        }

        if (!window.blogCommentCountCallbacks.has(blogId)) {
            window.blogCommentCountCallbacks.set(blogId, new Set());
        }

        window.blogCommentCountCallbacks.get(blogId).add(callback);
        console.log(`📝 Registered comment count callback for blog ${blogId}`);

        // Return cleanup function
        return () => {
            const callbacks = window.blogCommentCountCallbacks?.get(blogId);
            if (callbacks) {
                callbacks.delete(callback);
                if (callbacks.size === 0) {
                    window.blogCommentCountCallbacks.delete(blogId);
                }
                console.log(`🗑️ Unregistered comment count callback for blog ${blogId}`);
            }
        };
    };

    const contextValue = {
        notifications,
        unreadCount,
        markAsRead,
        markAllAsRead,
        clearAllNotifications,
        registerCommentCountCallback
    };

    return (
        <GlobalCommentNotificationContext.Provider value={contextValue}>
            {children}
        </GlobalCommentNotificationContext.Provider>
    );
};