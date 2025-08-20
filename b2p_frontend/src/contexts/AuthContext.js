import React, { createContext, useContext, useState, useEffect } from 'react';
import { Navigate, useLocation } from 'react-router-dom';

const AuthContext = createContext();

// 🎭 ROLE CONSTANTS
export const ROLES = {
  ADMIN: 'Admin',
  PLAYER: 'Player',
  COURTOWNER: 'CourtOwner',
};

export const ROLE_IDS = {
  ADMIN: 1,
  PLAYER: 2,
  COURTOWNER: 3,
};

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

  // 🎭 ROLE CHECKING FUNCTIONS
  const getUserRole = () => {
    return user?.roleName || null;
  };

  const getUserRoleId = () => {
    return user?.roleId || null;
  };

  const hasRole = (requiredRoles) => {
    if (!user || !user.roleName) {
      console.log('🚫 hasRole: No user or role found');
      return false;
    }
    
    // Convert to array if string
    const roles = Array.isArray(requiredRoles) ? requiredRoles : [requiredRoles];
    const hasAccess = roles.includes(user.roleName);
    
    console.log(`🔍 hasRole: User role "${user.roleName}", Required: [${roles.join(', ')}], Access: ${hasAccess}`);
    return hasAccess;
  };

  const hasRoleId = (requiredRoleIds) => {
    if (!user || user.roleId === null || user.roleId === undefined) {
      console.log('🚫 hasRoleId: No user or roleId found');
      return false;
    }
    
    // Convert to array if number
    const roleIds = Array.isArray(requiredRoleIds) ? requiredRoleIds : [requiredRoleIds];
    const hasAccess = roleIds.includes(user.roleId);
    
    console.log(`🔍 hasRoleId: User roleId "${user.roleId}", Required: [${roleIds.join(', ')}], Access: ${hasAccess}`);
    return hasAccess;
  };

  // 🎯 SPECIFIC ROLE CHECKING FUNCTIONS
  const isAdmin = () => {
    const adminCheck = user?.roleName === ROLES.ADMIN || user?.roleId === ROLE_IDS.ADMIN;
    console.log(`👑 isAdmin: ${adminCheck} (Role: ${user?.roleName}, RoleId: ${user?.roleId})`);
    return adminCheck;
  };

  const isPlayer = () => {
    const playerCheck = user?.roleName === ROLES.PLAYER || user?.roleId === ROLE_IDS.PLAYER;
    console.log(`🏃‍♂️ isPlayer: ${playerCheck} (Role: ${user?.roleName}, RoleId: ${user?.roleId})`);
    return playerCheck;
  };

  const isCourtOwner = () => {
    const courtOwnerCheck = user?.roleName === ROLES.COURTOWNER || user?.roleId === ROLE_IDS.COURTOWNER;
    console.log(`🏢 isCourtOwner: ${courtOwnerCheck} (Role: ${user?.roleName}, RoleId: ${user?.roleId})`);
    return courtOwnerCheck;
  };

  // 🎯 Helper values
  const isLoggedIn = !!(user && token);
  const userId = user?.userId || null;

  console.log('🔍 AuthContext current state:', {
    hasUser: !!user,
    hasToken: !!token,
    isLoggedIn,
    userId,
    userRole: getUserRole(),
    userRoleId: getUserRoleId(),
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
    
    // Role functions
    getUserRole,
    getUserRoleId,
    hasRole,
    hasRoleId,
    isAdmin,
    isPlayer,
    isCourtOwner,
    
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

// 🛡️ PROTECTED ROUTE COMPONENT
export const ProtectedRoute = ({ 
  children, 
  requiredRoles = [], 
  requiredRoleIds = [],
  fallbackPath = '/unauthorized',
  adminOnly = false,
  playerOnly = false,
  courtOwnerOnly = false,
  playerAndGuestOnly = false, // 🆕 NEW PROP
  allowedRoles = [], // Alternative name for requiredRoles
  // 🆕 NEW REDIRECT PROPS for playerAndGuestOnly
  adminRedirect,
  courtOwnerRedirect
}) => {
  const { user, isLoading, hasRole, hasRoleId, isAdmin, isPlayer, isCourtOwner } = useAuth();
  const location = useLocation();

  // Use allowedRoles if requiredRoles is empty (for flexibility)
  const rolesToCheck = requiredRoles.length > 0 ? requiredRoles : allowedRoles;

  // Show loading spinner while checking auth
  if (isLoading) {
    return (
      <div className="flex justify-center items-center h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
        <span className="ml-3 text-gray-600">Đang kiểm tra quyền truy cập...</span>
      </div>
    );
  }

  // 🆕 HANDLE playerAndGuestOnly - Allow Guest & Player, redirect Admin & CourtOwner
  if (playerAndGuestOnly) {
    // If user is Admin and adminRedirect is provided, redirect
    if (user && isAdmin() && adminRedirect) {
      console.log('🔄 ProtectedRoute: Admin detected, redirecting to:', adminRedirect);
      return <Navigate to={adminRedirect} replace />;
    }
    
    // If user is CourtOwner and courtOwnerRedirect is provided, redirect
    if (user && isCourtOwner() && courtOwnerRedirect) {
      console.log('🔄 ProtectedRoute: CourtOwner detected, redirecting to:', courtOwnerRedirect);
      return <Navigate to={courtOwnerRedirect} replace />;
    }
    
    // Allow Guest (not logged in) and Player
    console.log('✅ ProtectedRoute: playerAndGuestOnly - Access granted for Guest or Player');
    return children;
  }

  // Redirect to login if not authenticated (for other protection types)
  if (!user) {
    console.log('🚫 ProtectedRoute: User not authenticated, redirecting to login');
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  // Check admin only
  if (adminOnly && !isAdmin()) {
    console.log('🚫 ProtectedRoute: Admin access required but user is not admin');
    return <Navigate to={fallbackPath} replace />;
  }

  // Check player only
  if (playerOnly && !isPlayer()) {
    console.log('🚫 ProtectedRoute: Player access required but user is not player');
    return <Navigate to={fallbackPath} replace />;
  }

  // Check court owner only
  if (courtOwnerOnly && !isCourtOwner()) {
    console.log('🚫 ProtectedRoute: CourtOwner access required but user is not court owner');
    return <Navigate to={fallbackPath} replace />;
  }

  // Check required roles by name
  if (rolesToCheck.length > 0 && !hasRole(rolesToCheck)) {
    console.log(`🚫 ProtectedRoute: Required roles ${rolesToCheck.join(', ')} not met. User role: ${user.roleName}`);
    return <Navigate to={fallbackPath} replace />;
  }

  // Check required roles by ID
  if (requiredRoleIds.length > 0 && !hasRoleId(requiredRoleIds)) {
    console.log(`🚫 ProtectedRoute: Required role IDs ${requiredRoleIds.join(', ')} not met. User role ID: ${user.roleId}`);
    return <Navigate to={fallbackPath} replace />;
  }

  console.log('✅ ProtectedRoute: Access granted');
  return children;
};

// 🎨 ROLE-BASED UI COMPONENT (Hide/Show elements based on role)
export const RoleBasedComponent = ({ 
  children, 
  allowedRoles = [], 
  allowedRoleIds = [],
  requiredRoles = [], // Alternative name
  requiredRoleIds = [], // Alternative name
  adminOnly = false,
  playerOnly = false,
  courtOwnerOnly = false,
  fallback = null,
  hideWhenNoAccess = true
}) => {
  const { hasRole, hasRoleId, isAdmin, isPlayer, isCourtOwner } = useAuth();

  // Use requiredRoles if allowedRoles is empty (for flexibility)
  const rolesToCheck = allowedRoles.length > 0 ? allowedRoles : requiredRoles;
  const roleIdsToCheck = allowedRoleIds.length > 0 ? allowedRoleIds : requiredRoleIds;

  // Check admin only
  if (adminOnly && !isAdmin()) {
    return hideWhenNoAccess ? fallback : null;
  }

  // Check player only
  if (playerOnly && !isPlayer()) {
    return hideWhenNoAccess ? fallback : null;
  }

  // Check court owner only
  if (courtOwnerOnly && !isCourtOwner()) {
    return hideWhenNoAccess ? fallback : null;
  }

  // Check allowed roles by name
  if (rolesToCheck.length > 0 && !hasRole(rolesToCheck)) {
    return hideWhenNoAccess ? fallback : null;
  }

  // Check allowed roles by ID
  if (roleIdsToCheck.length > 0 && !hasRoleId(roleIdsToCheck)) {
    return hideWhenNoAccess ? fallback : null;
  }

  return children;
};

// 🚪 PUBLIC ROUTE (Redirect if already logged in)
export const PublicRoute = ({ children, redirectTo = '/dashboard' }) => {
  const { isLoggedIn, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="flex justify-center items-center h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
        <span className="ml-3 text-gray-600">Đang tải...</span>
      </div>
    );
  }

  if (isLoggedIn) {
    console.log('🔄 PublicRoute: User already logged in, redirecting to:', redirectTo);
    return <Navigate to={redirectTo} replace />;
  }

  return children;
};

// 🚧 ROLE-BASED REDIRECT (Redirect based on user role)
export const RoleBasedRedirect = ({ 
  adminRedirect = '/admin/dashboard', 
  playerRedirect = '/player/dashboard', 
  courtOwnerRedirect = '/courtowner/dashboard',
  defaultRedirect = '/dashboard'
}) => {
  const { user, isLoading, isAdmin, isPlayer, isCourtOwner } = useAuth();

  if (isLoading) {
    return (
      <div className="flex justify-center items-center h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
        <span className="ml-3 text-gray-600">Đang chuyển hướng...</span>
      </div>
    );
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  // Redirect based on role
  if (isAdmin()) {
    console.log('🔄 Redirecting Admin to:', adminRedirect);
    return <Navigate to={adminRedirect} replace />;
  }
  
  if (isPlayer()) {
    console.log('🔄 Redirecting Player to:', playerRedirect);
    return <Navigate to={playerRedirect} replace />;
  }
  
  if (isCourtOwner()) {
    console.log('🔄 Redirecting CourtOwner to:', courtOwnerRedirect);
    return <Navigate to={courtOwnerRedirect} replace />;
  }

  // Default redirect if role is not recognized
  console.log('🔄 Unknown role, redirecting to:', defaultRedirect);
  return <Navigate to={defaultRedirect} replace />;
};