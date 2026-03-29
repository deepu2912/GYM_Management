import { useState } from "react";
import { useAuth } from "../context/AuthContext";

function ResetPasswordPage() {
  const { changePassword, getApiErrorMessage } = useAuth();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (event) => {
    event.preventDefault();
    setError("");
    setSuccess("");

    if (newPassword.length < 8) {
      setError("New password must be at least 8 characters.");
      return;
    }

    if (newPassword !== confirmPassword) {
      setError("New password and confirm password must match.");
      return;
    }

    if (newPassword === currentPassword) {
      setError("New password must be different from current password.");
      return;
    }

    setLoading(true);
    try {
      await changePassword(currentPassword, newPassword);
      setSuccess("Password updated successfully.");
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to change password. Please try again."));
    } finally {
      setLoading(false);
    }
  };

  return (
    <section>
      <h2 className="text-2xl font-bold text-slate-900">Reset Password</h2>
      <p className="mt-1 text-sm text-slate-600">Enter your old password, then set a new secure password.</p>

      <form className="mt-5 max-w-lg rounded-2xl border border-slate-200 bg-white p-5 shadow-sm" onSubmit={handleSubmit}>
        <div className="space-y-4">
          <div>
            <label className="mb-1 block text-sm font-semibold text-slate-700" htmlFor="currentPassword">
              Old Password
            </label>
            <input
              id="currentPassword"
              type="password"
              value={currentPassword}
              onChange={(event) => setCurrentPassword(event.target.value)}
              required
              className="w-full rounded-lg border border-slate-300 px-3 py-2 outline-none focus:border-orange-500"
            />
          </div>

          <div>
            <label className="mb-1 block text-sm font-semibold text-slate-700" htmlFor="newPassword">
              New Password
            </label>
            <input
              id="newPassword"
              type="password"
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
              required
              minLength={8}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 outline-none focus:border-orange-500"
            />
          </div>

          <div>
            <label className="mb-1 block text-sm font-semibold text-slate-700" htmlFor="confirmPassword">
              Confirm New Password
            </label>
            <input
              id="confirmPassword"
              type="password"
              value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)}
              required
              minLength={8}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 outline-none focus:border-orange-500"
            />
          </div>
        </div>

        {error && <p className="mt-3 text-sm font-medium text-red-600">{error}</p>}
        {success && <p className="mt-3 text-sm font-medium text-emerald-600">{success}</p>}

        <button
          type="submit"
          disabled={loading}
          className="mt-4 rounded-lg bg-orange-500 px-4 py-2 font-semibold text-white hover:bg-orange-600 disabled:opacity-60"
        >
          {loading ? "Updating..." : "Update Password"}
        </button>
      </form>
    </section>
  );
}

export default ResetPasswordPage;
