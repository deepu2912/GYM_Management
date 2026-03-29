import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api, getApiErrorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";

type GymSubscriptionStatus = {
  plan: string;
  validTill: string | null;
  lifetimePlanActivated: boolean;
  isExpired: boolean;
  daysRemaining: number;
  currentPlanAmount: number;
};

type GymSummary = {
  id: string;
  gymName: string;
  city: string;
  email: string;
  phone: string;
};

type SubscriptionPlanOption = {
  id: string;
  code: string;
  name: string;
  price: number;
  durationMonths: number;
  isLifetime: boolean;
  isMaintenance: boolean;
  isActive: boolean;
  sortOrder: number;
  description: string;
};

function SuperAdminGymSubscriptionPage() {
  const { user } = useAuth();
  const { gymId } = useParams();
  const [gym, setGym] = useState<GymSummary | null>(null);
  const [subscription, setSubscription] = useState<GymSubscriptionStatus | null>(null);
  const [plans, setPlans] = useState<SubscriptionPlanOption[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [selectedPlanCode, setSelectedPlanCode] = useState("");
  const [paymentMode, setPaymentMode] = useState("UPI");
  const [transactionReference, setTransactionReference] = useState("");
  const [notes, setNotes] = useState("");

  const isSuperAdmin = user?.role === "SuperAdmin";

  const loadData = async () => {
    if (!gymId) {
      return;
    }
    setLoading(true);
    setError("");
    try {
      const [gymRes, subRes, plansRes] = await Promise.all([
        api.get(`/api/gyms/${gymId}`),
        api.get(`/api/gyms/${gymId}/subscription`),
        api.get("/api/subscription-plans/active"),
      ]);
      setGym((gymRes.data ?? null) as GymSummary | null);
      setSubscription((subRes.data ?? null) as GymSubscriptionStatus | null);
      setPlans((plansRes.data ?? []) as SubscriptionPlanOption[]);
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to load gym subscription details."));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isSuperAdmin && gymId) {
      loadData();
    } else {
      setLoading(false);
    }
  }, [isSuperAdmin, gymId]);

  const getPlanAmount = (planCode: string) => {
    if (!planCode) {
      return 0;
    }
    return plans.find((plan) => plan.code === planCode)?.price ?? 0;
  };

  const payAndActivate = async () => {
    if (!gymId) {
      return;
    }
    if (!selectedPlanCode) {
      setError("Please select a plan first.");
      return;
    }
    setSaving(true);
    setError("");
    setSuccess("");
    try {
      const response = await api.post(`/api/gyms/${gymId}/subscription/pay`, {
        planCode: selectedPlanCode,
        paymentMode,
        transactionReference: transactionReference || null,
        notes: notes || null,
        paidOn: new Date().toISOString(),
        amount: getPlanAmount(selectedPlanCode),
      });
      await loadData();
      const invoiceNumber = response.data?.invoiceNumber;
      const mailStatus = response.data?.invoiceEmailSent
        ? "Invoice emailed to gym admin."
        : response.data?.invoiceEmailError
          ? `Invoice email failed: ${response.data.invoiceEmailError}`
          : "Invoice email status unavailable.";
      setSuccess(`Payment recorded (${invoiceNumber}). ${mailStatus}`);
      setSelectedPlanCode("");
      setTransactionReference("");
      setNotes("");
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to process subscription payment."));
    } finally {
      setSaving(false);
    }
  };

  if (!isSuperAdmin) {
    return <p className="text-slate-600">Only Super Admin can access this page.</p>;
  }

  return (
    <section className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-2xl font-bold text-slate-900">Gym Subscription</h2>
          <p className="mt-1 text-sm text-slate-600">Manage plan, validity and lifetime maintenance for this gym.</p>
        </div>
        <div className="flex gap-2">
          <Link
            to={`/super-admin/gyms/${gymId}`}
            className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100"
          >
            Gym Profile
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
      {success && <p className="rounded-lg bg-emerald-50 px-3 py-2 text-sm font-medium text-emerald-700">{success}</p>}
      {loading && <p className="text-slate-600">Loading subscription...</p>}

      {!loading && gym && subscription && (
        <>
          <div className="rounded-xl border border-slate-200 bg-white p-4">
            <h3 className="text-lg font-bold text-slate-900">{gym.gymName}</h3>
            <p className="mt-1 text-sm text-slate-600">
              {gym.city} | {gym.email} | {gym.phone}
            </p>
            <div className="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-3">
              <Stat label="Current Plan" value={subscription.plan || "None"} />
              <Stat
                label="Valid Till"
                value={subscription.validTill ? new Date(subscription.validTill).toLocaleDateString() : "-"}
              />
              <Stat
                label="Status"
                value={subscription.isExpired ? "Expired" : `${Math.max(subscription.daysRemaining, 0)} day(s) remaining`}
                danger={subscription.isExpired}
              />
            </div>
          </div>

          <div className="rounded-xl border border-slate-200 bg-white p-4">
            <h4 className="text-sm font-bold text-slate-800">Select Plan</h4>
            <p className="mt-1 text-xs text-slate-600">Plans are loaded from super-admin subscription plan setup.</p>
            <div className="mt-3 grid grid-cols-1 gap-3 md:grid-cols-3">
              {!subscription.lifetimePlanActivated && (
                plans
                  .filter((plan) => !plan.isMaintenance)
                  .map((plan) => (
                    <PlanCard
                      key={plan.id}
                      title={plan.name}
                      price={`₹${plan.price.toLocaleString()}`}
                      subtitle={plan.description?.trim() ? plan.description : `${plan.durationMonths} month(s) validity`}
                      onChoose={() => setSelectedPlanCode(plan.code)}
                      selected={selectedPlanCode === plan.code}
                      disabled={saving}
                    />
                  ))
              )}
              {subscription.lifetimePlanActivated && (
                <>
                  {plans
                    .filter((plan) => plan.isLifetime)
                    .map((plan) => (
                      <PlanCard key={plan.id} title={plan.name} price="Active" subtitle="Already active" onChoose={() => {}} selected disabled />
                    ))}
                  {plans
                    .filter((plan) => plan.isMaintenance)
                    .map((plan) => (
                      <PlanCard
                        key={plan.id}
                        title={plan.name}
                        price={`₹${plan.price.toLocaleString()}`}
                        subtitle={plan.description || "Maintenance renewal"}
                        onChoose={() => setSelectedPlanCode(plan.code)}
                        selected={selectedPlanCode === plan.code}
                        disabled={saving}
                      />
                    ))}
                </>
              )}
            </div>
            <div className="mt-4 rounded-xl border border-slate-200 bg-slate-50 p-3">
              <h4 className="text-sm font-bold text-slate-900">Payment Details</h4>
              <div className="mt-3 grid grid-cols-1 gap-3 md:grid-cols-3">
                <label>
                  <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Payment Mode</span>
                  <select
                    value={paymentMode}
                    onChange={(event) => setPaymentMode(event.target.value)}
                    className="h-10 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm outline-none focus:border-orange-500"
                  >
                    <option value="UPI">UPI</option>
                    <option value="Card">Card</option>
                    <option value="BankTransfer">Bank Transfer</option>
                    <option value="Cash">Cash</option>
                  </select>
                </label>
                <label className="md:col-span-2">
                  <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Transaction Ref (Optional)</span>
                  <input
                    value={transactionReference}
                    onChange={(event) => setTransactionReference(event.target.value)}
                    className="h-10 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm outline-none focus:border-orange-500"
                  />
                </label>
                <label className="md:col-span-3">
                  <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Notes (Optional)</span>
                  <input
                    value={notes}
                    onChange={(event) => setNotes(event.target.value)}
                    className="h-10 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm outline-none focus:border-orange-500"
                  />
                </label>
              </div>
              <div className="mt-3 flex items-center justify-between">
                <p className="text-sm font-semibold text-slate-700">
                  Amount: <span className="text-orange-600">₹{getPlanAmount(selectedPlanCode).toLocaleString()}</span>
                </p>
                <button
                  type="button"
                  onClick={payAndActivate}
                  disabled={saving || !selectedPlanCode}
                  className="rounded-md bg-orange-500 px-4 py-2 text-xs font-semibold text-white hover:bg-orange-600 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {saving ? "Processing..." : "Pay & Activate"}
                </button>
              </div>
            </div>
          </div>
        </>
      )}
    </section>
  );
}

function Stat({ label, value, danger = false }: { label: string; value: string; danger?: boolean }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-slate-50 p-3">
      <p className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">{label}</p>
      <p className={`mt-1 text-sm font-semibold ${danger ? "text-red-700" : "text-slate-900"}`}>{value}</p>
    </div>
  );
}

function PlanCard({
  title,
  price,
  subtitle,
  onChoose,
  selected,
  disabled,
}: {
  title: string;
  price: string;
  subtitle: string;
  onChoose: () => void;
  selected?: boolean;
  disabled?: boolean;
}) {
  return (
    <div className={`rounded-xl border p-3 ${selected ? "border-orange-400 bg-orange-50" : "border-slate-200 bg-slate-50"}`}>
      <h4 className="text-sm font-bold text-slate-900">{title}</h4>
      <p className="mt-1 text-lg font-bold text-orange-600">{price}</p>
      <p className="mt-1 text-xs text-slate-600">{subtitle}</p>
      <button
        type="button"
        onClick={onChoose}
        disabled={disabled}
        className="mt-3 w-full rounded-md bg-orange-500 px-3 py-2 text-xs font-semibold text-white hover:bg-orange-600 disabled:cursor-not-allowed disabled:opacity-50"
      >
        {disabled ? "Not Available" : "Select"}
      </button>
    </div>
  );
}

export default SuperAdminGymSubscriptionPage;
