import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

function ProtectedRoute({ children }) {
  const { isAuthenticated, user } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  const mustChangePassword = Boolean(user?.mustChangePassword);
  if (mustChangePassword && location.pathname !== "/change-password") {
    return <Navigate to="/change-password" replace />;
  }

  if (!mustChangePassword && location.pathname === "/change-password") {
    return <Navigate to="/" replace />;
  }

  return children;
}

export default ProtectedRoute;
