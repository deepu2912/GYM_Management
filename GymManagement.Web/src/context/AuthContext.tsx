import { createContext, useContext, useState } from "react";
import { api, getApiErrorMessage } from "../api/client";

const AuthContext = createContext(null);
const AUTH_KEY = "gym_auth";

function loadInitialAuth() {
  const authRaw = localStorage.getItem(AUTH_KEY);
  if (!authRaw) {
    return { token: null, user: null };
  }

  try {
    const auth = JSON.parse(authRaw);
    return {
      token: auth.token ?? null,
      user: auth.user ?? null,
    };
  } catch {
    localStorage.removeItem(AUTH_KEY);
    return { token: null, user: null };
  }
}

export function AuthProvider({ children }) {
  const [{ token, user }, setAuthState] = useState(loadInitialAuth);

  const saveAuth = (nextToken, nextUser) => {
    setAuthState({ token: nextToken, user: nextUser });
    localStorage.setItem(
      AUTH_KEY,
      JSON.stringify({
        token: nextToken,
        user: nextUser,
      })
    );
  };

  const login = async (email, password) => {
    const response = await api.post("/api/auth/login", { email, password });
    const payload = response.data;
    const nextUser = {
      name: payload.name,
      email: payload.email,
      role: payload.role,
      mustChangePassword: Boolean(payload.mustChangePassword),
      profilePhotoDataUri: payload.profilePhotoDataUri ?? null,
    };

    saveAuth(payload.token, nextUser);
    return nextUser;
  };

  const registerMember = async (formData) => {
    const response = await api.post("/api/auth/register-member", formData);
    const payload = response.data;
    const nextUser = {
      name: payload.name,
      email: payload.email,
      role: payload.role,
      mustChangePassword: Boolean(payload.mustChangePassword),
      profilePhotoDataUri: payload.profilePhotoDataUri ?? null,
    };

    saveAuth(payload.token, nextUser);
    return nextUser;
  };

  const changePassword = async (currentPassword, newPassword) => {
    const response = await api.post("/api/auth/change-password", { currentPassword, newPassword });
    const payload = response.data;
    const nextUser = {
      name: payload.name,
      email: payload.email,
      role: payload.role,
      mustChangePassword: Boolean(payload.mustChangePassword),
      profilePhotoDataUri: payload.profilePhotoDataUri ?? null,
    };

    saveAuth(payload.token, nextUser);
    return nextUser;
  };

  const logout = () => {
    setAuthState({ token: null, user: null });
    localStorage.removeItem(AUTH_KEY);
  };

  const setProfilePhoto = (profilePhotoDataUri) => {
    if (!user) {
      return;
    }

    const nextUser = {
      ...user,
      profilePhotoDataUri: profilePhotoDataUri ?? null,
    };
    saveAuth(token, nextUser);
  };

  const value = {
    token,
    user,
    isAuthenticated: Boolean(token),
    login,
    changePassword,
    registerMember,
    logout,
    setProfilePhoto,
    getApiErrorMessage,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within AuthProvider.");
  }
  return context;
}
