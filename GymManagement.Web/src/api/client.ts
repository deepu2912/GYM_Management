import axios from "axios";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "";
const AUTH_KEY = "gym_auth";

function getToken() {
  const authRaw = localStorage.getItem(AUTH_KEY);
  if (!authRaw) {
    return null;
  }

  try {
    const auth = JSON.parse(authRaw);
    return auth?.token ?? null;
  } catch {
    return null;
  }
}

export const api = axios.create({
  baseURL: API_BASE_URL,
});

api.interceptors.request.use((config) => {
  const token = getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  if (config.data instanceof FormData) {
    delete config.headers["Content-Type"];
  } else if (!config.headers["Content-Type"]) {
    config.headers["Content-Type"] = "application/json";
  }

  return config;
});

export function getApiErrorMessage(error, fallbackMessage) {
  const data = error?.response?.data;
  if (!data) {
    return fallbackMessage;
  }

  if (typeof data === "string") {
    return data;
  }

  if (data.message) {
    return data.message;
  }

  if (data.errors && typeof data.errors === "object") {
    const messages = Object.entries(data.errors).flatMap(([field, value]) => {
      if (!Array.isArray(value)) {
        return [];
      }
      return value.map((message) => `${field}: ${message}`);
    });

    if (messages.length > 0) {
      return messages.join(" | ");
    }
  }

  if (data.title) {
    return data.title;
  }

  return fallbackMessage;
}
