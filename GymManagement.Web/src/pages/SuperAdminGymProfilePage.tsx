import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api, getApiErrorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";

function SuperAdminGymProfilePage() {
  const { user } = useAuth();
  const { gymId } = useParams();
  const [gym, setGym] = useState<any | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const isSuperAdmin = user?.role === "SuperAdmin";

  useEffect(() => {
    if (!isSuperAdmin || !gymId) {
      setLoading(false);
      return;
    }

    const loadGym = async () => {
      setLoading(true);
      setError("");
      try {
        const response = await api.get(`/api/gyms/${gymId}`);
        setGym(response.data ?? null);
      } catch (err) {
        setError(getApiErrorMessage(err, "Unable to load gym profile."));
      } finally {
        setLoading(false);
      }
    };

    loadGym();
  }, [isSuperAdmin, gymId]);

  if (!isSuperAdmin) {
    return <p className="text-slate-600">Only Super Admin can access this page.</p>;
  }

  return (
    <section className="space-y-5">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h2 className="text-2xl font-bold text-slate-900">Gym Profile</h2>
          <p className="mt-1 text-sm text-slate-600">Detailed profile and onboarding details for the selected gym.</p>
        </div>
        <div className="flex gap-2">
          <Link
            to={`/super-admin/gyms/${gymId}/subscription`}
            className="rounded-lg border border-indigo-300 bg-white px-4 py-2 text-sm font-semibold text-indigo-700 hover:bg-indigo-50"
          >
            Subscription
          </Link>
          <Link
            to="/super-admin/gyms"
            className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100"
          >
            Back to Gyms
          </Link>
        </div>
      </div>

      {error && <p className="rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">{error}</p>}
      {loading && <p className="text-slate-600">Loading gym profile...</p>}

      {!loading && !error && gym && (
        <div className="grid grid-cols-1 gap-4 xl:grid-cols-2">
          <div className="rounded-xl border border-slate-200 bg-white p-4">
            <h3 className="text-base font-bold text-slate-900">Gym Details</h3>
            <div className="mt-3 grid grid-cols-1 gap-3 md:grid-cols-2">
              <Info label="Gym Name" value={gym.gymName} />
              <Info label="Status" value={gym.isActive ? "Active" : "Inactive"} />
              <Info label="Email" value={gym.email} />
              <Info label="Phone" value={gym.phone} />
              <Info label="State" value={gym.state} />
              <Info label="City" value={gym.city} />
              <Info label="Pincode" value={gym.pincode} />
              <Info label="Address" value={gym.addressLine} full />
              <Info label="GST Number" value={gym.gstNumber} />
              <Info label="HSN/SAC" value={gym.hsnSacCode} />
              <Info label="GST Rate %" value={gym.gstRatePercent?.toString?.() ?? "-"} />
              <Info label="GST Applicable" value={gym.isGstApplicable ? "Yes" : "No"} />
            </div>
          </div>

          <div className="rounded-xl border border-slate-200 bg-white p-4">
            <h3 className="text-base font-bold text-slate-900">Admin & Bank Details</h3>
            <div className="mt-3 grid grid-cols-1 gap-3 md:grid-cols-2">
              <Info label="Admin Name" value={gym.admin?.name ?? "-"} />
              <Info label="Admin Email" value={gym.admin?.email ?? "-"} />
              <Info label="Bank Name" value={gym.bankName} />
              <Info label="Account Name" value={gym.accountName} />
              <Info label="Account Number" value={gym.accountNumber} />
              <Info label="IFSC Code" value={gym.ifscCode} />
              <Info label="UPI ID" value={gym.upiId || "-"} />
              <Info label="Created On" value={gym.createdOn ? new Date(gym.createdOn).toLocaleString() : "-"} />
            </div>
          </div>
        </div>
      )}
    </section>
  );
}

function Info({ label, value, full = false }) {
  return (
    <div className={full ? "md:col-span-2" : ""}>
      <p className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">{label}</p>
      <p className="mt-1 text-sm font-medium text-slate-900">{value || "-"}</p>
    </div>
  );
}

export default SuperAdminGymProfilePage;
