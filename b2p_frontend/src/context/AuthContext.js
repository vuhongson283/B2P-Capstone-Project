import React, { createContext, useContext, useState, useEffect } from 'react';

const AuthContext = createContext();

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [token, setToken] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  // 🔄 Load user from localStorage on app start
  useEffect(() => {
    console.log('📱 AuthContext: Loading user from localStorage...');
    
    try {
      const savedUser = localStorage.getItem('user');
      const savedToken = localStorage.getItem('accessToken');
      
      if (savedUser && savedToken) {
        const parsedUser = JSON.parse(savedUser);
        setUser(parsedUser);
        setToken(savedToken);
        console.log('✅ AuthContext: User loaded from localStorage:', parsedUser);
      } else {
        console.log('ℹ️ AuthContext: No saved user found');
      }
    } catch (error) {
      console.error('❌ AuthContext: Error loading user from localStorage:', error);
      // Clear corrupted data
      localStorage.removeItem('user');
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
    } finally {
      setIsLoading(false);
    }
  }, []);

  // 🔑 Login function
  const login = (userData, accessToken, refreshToken) => {
    console.log('🔑 AuthContext: Logging in user:', userData);
    
    setUser(userData);
    setToken(accessToken);
    localStorage.setItem('user', JSON.stringify(userData));
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
  };

  // 🚪 Logout function
  const logout = () => {
    console.log('🚪 AuthContext: Logging out user');
    
    setUser(null);
    setToken(null);
    localStorage.removeItem('user');
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
  };

  // 🎯 Helper values
  const isLoggedIn = !!(user && token);
  const userId = user?.userId || null;

  console.log('🔍 AuthContext current state:', {
    hasUser: !!user,
    hasToken: !!token,
    isLoggedIn,
    userId,
    isLoading
  });

  const contextValue = {
    // State
    user,
    token,
    isLoading,
    
    // Computed values
    isLoggedIn,
    userId,
    
    // Actions
    login,
    logout
  };

  return (
    <AuthContext.Provider value={contextValue}>
      {children}
    </AuthContext.Provider>
  );
};

// 🎣 Custom hook
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};