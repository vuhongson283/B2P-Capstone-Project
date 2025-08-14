import React from 'react';
import { useAuth } from '../../context/AuthContext';

const UserInfo = () => {
  const { user, userId, isLoggedIn, isLoading } = useAuth();

  console.log('🔍 UserInfo component - Auth state:', {
    isLoading,
    isLoggedIn,
    userId,
    user
  });

  if (isLoading) {
    return <div>⏳ Loading user info...</div>;
  }

  if (!isLoggedIn) {
    return (
      <div style={{ padding: '20px', border: '1px solid #ccc', margin: '10px' }}>
        <h3>❌ Not logged in</h3>
        <p>Please login to see user information</p>
      </div>
    );
  }

  return (
    <div style={{ padding: '20px', border: '1px solid green', margin: '10px' }}>
      <h3>✅ User Information (from Context)</h3>
      <p><strong>User ID:</strong> {userId}</p>
      <p><strong>Email:</strong> {user.email}</p>
      <p><strong>Full Name:</strong> {user.fullName}</p>
      <p><strong>Role ID:</strong> {user.roleId}</p>
      <p><strong>Role Name:</strong> {user.roleName}</p>
    </div>
  );
};

export default UserInfo;