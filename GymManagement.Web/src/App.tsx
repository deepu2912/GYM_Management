import { Navigate, Route, Routes } from "react-router-dom";
import AppShell from "./components/AppShell";
import ProtectedRoute from "./components/ProtectedRoute";
import { useAuth } from "./context/AuthContext";
import BusinessDetailsPage from "./pages/BusinessDetailsPage";
import DashboardPage from "./pages/DashboardPage";
import ForcePasswordChangePage from "./pages/ForcePasswordChangePage";
import LoginPage from "./pages/LoginPage";
import MemberMembershipsPage from "./pages/MemberMembershipsPage";
import MemberProfilePage from "./pages/MemberProfilePage";
import MembersPage from "./pages/MembersPage";
import PlansPage from "./pages/PlansPage";
import ProfilePage from "./pages/ProfilePage";
import ResetPasswordPage from "./pages/ResetPasswordPage";
import RegisterPage from "./pages/RegisterPage";
import ReportsAttendancePage from "./pages/ReportsAttendancePage";
import ReportsFinancialPage from "./pages/ReportsFinancialPage";
import ReportsPaymentCollectionsPage from "./pages/ReportsPaymentCollectionsPage";
import ReportsPaymentDuesPage from "./pages/ReportsPaymentDuesPage";
import SuperAdminGymSubscriptionPage from "./pages/SuperAdminGymSubscriptionPage";
import SuperAdminGymsPage from "./pages/SuperAdminGymsPage";
import SuperAdminGymProfilePage from "./pages/SuperAdminGymProfilePage";
import SuperAdminInvoiceSettingsPage from "./pages/SuperAdminInvoiceSettingsPage";
import SuperAdminSubscriptionPlansPage from "./pages/SuperAdminSubscriptionPlansPage";

function App() {
  const { token, user } = useAuth();
  const isSuperAdmin = user?.role === "SuperAdmin";

  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route
        path="/change-password"
        element={
          <ProtectedRoute>
            <ForcePasswordChangePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <AppShell />
          </ProtectedRoute>
        }
      >
        <Route
          index
          element={
            isSuperAdmin ? <Navigate to="/super-admin/gyms" replace /> : <DashboardPage />
          }
        />
        <Route path="members" element={isSuperAdmin ? <Navigate to="/super-admin/gyms" replace /> : <MembersPage />} />
        <Route
          path="members/:memberId"
          element={isSuperAdmin ? <Navigate to="/super-admin/gyms" replace /> : <MemberProfilePage />}
        />
        <Route path="plans" element={isSuperAdmin ? <Navigate to="/super-admin/gyms" replace /> : <PlansPage />} />
        <Route
          path="member-memberships"
          element={isSuperAdmin ? <Navigate to="/super-admin/gyms" replace /> : <MemberMembershipsPage />}
        />
        <Route
          path="reports/financial"
          element={isSuperAdmin ? <Navigate to="/super-admin/gyms" replace /> : <ReportsFinancialPage />}
        />
        <Route
          path="reports/attendance"
          element={isSuperAdmin ? <Navigate to="/super-admin/gyms" replace /> : <ReportsAttendancePage />}
        />
        <Route
          path="reports/payment-dues"
          element={isSuperAdmin ? <Navigate to="/super-admin/gyms" replace /> : <ReportsPaymentDuesPage />}
        />
        <Route
          path="reports/payment-collections"
          element={isSuperAdmin ? <Navigate to="/super-admin/gyms" replace /> : <ReportsPaymentCollectionsPage />}
        />
        <Route
          path="business-details"
          element={isSuperAdmin ? <Navigate to="/super-admin/gyms" replace /> : <BusinessDetailsPage />}
        />
        <Route
          path="super-admin/gyms"
          element={isSuperAdmin ? <SuperAdminGymsPage /> : <Navigate to="/" replace />}
        />
        <Route
          path="super-admin/gyms/:gymId"
          element={isSuperAdmin ? <SuperAdminGymProfilePage /> : <Navigate to="/" replace />}
        />
        <Route
          path="super-admin/gyms/:gymId/subscription"
          element={isSuperAdmin ? <SuperAdminGymSubscriptionPage /> : <Navigate to="/" replace />}
        />
        <Route
          path="super-admin/invoice-settings"
          element={isSuperAdmin ? <SuperAdminInvoiceSettingsPage /> : <Navigate to="/" replace />}
        />
        <Route
          path="super-admin/subscription-plans"
          element={isSuperAdmin ? <SuperAdminSubscriptionPlansPage /> : <Navigate to="/" replace />}
        />
        <Route path="profile" element={<ProfilePage />} />
        <Route path="reset-password" element={<ResetPasswordPage />} />
      </Route>
      <Route path="*" element={<Navigate to={token ? "/" : "/login"} replace />} />
    </Routes>
  );
}

export default App;
