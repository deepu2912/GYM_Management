import { useEffect, useMemo, useState } from "react";
import { api, getApiErrorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";

const durationOptions = ["OneMonth", "ThreeMonths", "SixMonths", "OneYear"];
const membershipTypeOptions = ["Single", "Couple"];

function normalizePlanType(rawType) {
  if (typeof rawType === "string") {
    return rawType.toLowerCase() === "couple" ? "Couple" : "Single";
  }
  if (typeof rawType === "number") {
    return rawType === 1 ? "Couple" : "Single";
  }
  return "Single";
}

const emptyForm = {
  planName: "",
  membershipType: "Single",
  duration: "OneMonth",
  price: "0",
  description: "",
};

function PlanForm({ form, onChange, onSubmit, onCancel, loading, editMode }) {
  return (
    <form onSubmit={onSubmit} className="mt-4 rounded-xl border border-slate-200 bg-slate-50 p-4">
      <h3 className="text-lg font-bold text-slate-900">{editMode ? "Edit Plan" : "Create Plan"}</h3>
      <div className="mt-4 grid grid-cols-1 gap-3 md:grid-cols-2">
        <label>
          <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">
            Plan Name
          </span>
          <input
            name="planName"
            value={form.planName}
            onChange={onChange}
            required
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
          />
        </label>

        <label>
          <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">
            Membership Type
          </span>
          <select
            name="membershipType"
            value={form.membershipType}
            onChange={onChange}
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
          >
            {membershipTypeOptions.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </label>

        <label>
          <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">
            Duration
          </span>
          <select
            name="duration"
            value={form.duration}
            onChange={onChange}
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
          >
            {durationOptions.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </label>

        <label>
          <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">
            Price (INR)
          </span>
          <input
            name="price"
            type="number"
            min="0"
            step="0.01"
            value={form.price}
            onChange={onChange}
            required
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
          />
        </label>

        <label className="md:col-span-2">
          <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">
            Description
          </span>
          <textarea
            name="description"
            value={form.description}
            onChange={onChange}
            rows={3}
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
          />
        </label>
      </div>

      <div className="mt-4 flex gap-2">
        <button
          type="submit"
          disabled={loading}
          className="rounded-lg bg-orange-500 px-4 py-2 text-sm font-semibold text-white hover:bg-orange-600 disabled:opacity-60"
        >
          {loading ? "Saving..." : editMode ? "Update Plan" : "Create Plan"}
        </button>
        <button
          type="button"
          onClick={onCancel}
          className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100"
        >
          Cancel
        </button>
      </div>
    </form>
  );
}

function PlansPage() {
  const { user } = useAuth();
  const isAdmin = useMemo(() => user?.role === "Admin", [user?.role]);
  const [plans, setPlans] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [editId, setEditId] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const activePlans = useMemo(() => plans.filter((p) => p.isActive !== false), [plans]);
  const inactivePlans = useMemo(() => plans.filter((p) => p.isActive === false), [plans]);

  const fetchPlans = async () => {
    setLoading(true);
    setError("");
    try {
      const response = await api.get("/api/membershipplans");
      setPlans(response.data ?? []);
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to load membership plans."));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPlans();
  }, []);

  const resetForm = () => {
    setForm(emptyForm);
    setEditId(null);
    setShowForm(false);
  };

  const handleChange = (event) => {
    const { name, value } = event.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleCreateClick = () => {
    setError("");
    setSuccess("");
    setEditId(null);
    setForm(emptyForm);
    setShowForm(true);
  };

  const handleEditClick = (plan) => {
    setError("");
    setSuccess("");
    setEditId(plan.id);
    setForm({
      planName: plan.planName ?? "",
      membershipType: normalizePlanType(plan.membershipType),
      duration: plan.duration ?? "OneMonth",
      price: String(plan.price ?? 0),
      description: plan.description ?? "",
    });
    setShowForm(true);
  };

  const buildPayload = () => ({
    planName: form.planName.trim(),
    membershipType: form.membershipType,
    duration: form.duration,
    price: Number(form.price || 0),
    description: form.description.trim(),
  });

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError("");
    setSuccess("");

    try {
      if (editId) {
        await api.put(`/api/membershipplans/${editId}`, buildPayload());
        setSuccess("Membership plan updated.");
      } else {
        await api.post("/api/membershipplans", buildPayload());
        setSuccess("Membership plan created.");
      }
      await fetchPlans();
      resetForm();
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to save membership plan."));
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (planId) => {
    const confirmed = window.confirm("Delete this membership plan?");
    if (!confirmed) {
      return;
    }

    setError("");
    setSuccess("");
    try {
      await api.delete(`/api/membershipplans/${planId}`);
      setPlans((prev) => prev.filter((x) => x.id !== planId));
      setSuccess("Membership plan deleted.");
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to delete membership plan."));
    }
  };

  const handleStatusChange = async (planId, isActive) => {
    setError("");
    setSuccess("");
    try {
      await api.patch(`/api/membershipplans/${planId}/status`, { isActive });
      await fetchPlans();
      setSuccess(`Membership plan ${isActive ? "activated" : "deactivated"}.`);
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to update plan status."));
    }
  };

  return (
    <section>
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h2 className="text-2xl font-bold text-slate-900">Membership Plans</h2>
          <p className="mt-1 text-sm text-slate-600">
            {isAdmin
              ? "Admin can create, update and delete plans."
              : "Read-only view for your role based on API authorization."}
          </p>
        </div>
        {isAdmin && (
          <button
            type="button"
            onClick={handleCreateClick}
            className="rounded-lg bg-orange-500 px-4 py-2 text-sm font-semibold text-white hover:bg-orange-600"
          >
            Add Plan
          </button>
        )}
      </div>

      {error && <p className="mt-4 rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">{error}</p>}
      {success && (
        <p className="mt-4 rounded-lg bg-emerald-50 px-3 py-2 text-sm font-medium text-emerald-700">{success}</p>
      )}

      {isAdmin && showForm && (
        <PlanForm
          form={form}
          onChange={handleChange}
          onSubmit={handleSubmit}
          onCancel={resetForm}
          loading={saving}
          editMode={Boolean(editId)}
        />
      )}

      {loading && <p className="mt-4 text-slate-600">Loading plans...</p>}

      {!loading && (
        <div className="mt-5 space-y-6">
          <div>
            <h3 className="text-sm font-bold uppercase tracking-wide text-slate-600">Active Plans</h3>
            <div className="mt-3 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              {activePlans.map((plan) => (
            <article key={plan.id} className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
              <h3 className="text-xl font-bold text-slate-900">{plan.planName}</h3>
              <p className="mt-1 text-sm text-slate-600">{plan.description || "No description"}</p>
              <div className="mt-4 flex items-end justify-between">
                <span className="text-sm font-semibold text-slate-500">
                  {normalizePlanType(plan.membershipType)} | {plan.duration}
                </span>
                <span className="text-lg font-bold text-orange-600">INR {plan.price}</span>
              </div>

              {isAdmin && (
                <div className="mt-4 flex gap-2">
                  <button
                    type="button"
                    onClick={() => handleEditClick(plan)}
                    className="rounded-md border border-slate-300 px-3 py-1 text-xs font-semibold text-slate-700 hover:bg-slate-100"
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    onClick={() => handleDelete(plan.id)}
                    disabled={plan.hasLinkedMemberships}
                    title={plan.hasLinkedMemberships ? "Plan linked with memberships cannot be deleted." : "Delete"}
                    className="rounded-md border border-red-300 px-3 py-1 text-xs font-semibold text-red-700 hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    Delete
                  </button>
                  <button
                    type="button"
                    onClick={() => handleStatusChange(plan.id, false)}
                    className="rounded-md border border-amber-300 px-3 py-1 text-xs font-semibold text-amber-700 hover:bg-amber-50"
                  >
                    Inactivate
                  </button>
                </div>
              )}
            </article>
              ))}
              {activePlans.length === 0 && (
                <p className="rounded-xl border border-slate-200 bg-white p-6 text-center text-slate-500 sm:col-span-2 xl:col-span-3">
                  No active plans found.
                </p>
              )}
            </div>
          </div>

          <div>
            <h3 className="text-sm font-bold uppercase tracking-wide text-slate-600">Inactive Plans</h3>
            <div className="mt-3 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              {inactivePlans.map((plan) => (
                <article key={plan.id} className="rounded-xl border border-slate-200 bg-slate-50 p-4 shadow-sm">
                  <div className="flex items-start justify-between gap-2">
                    <h3 className="text-xl font-bold text-slate-700">{plan.planName}</h3>
                    <span className="rounded-full bg-slate-200 px-2 py-0.5 text-[10px] font-semibold uppercase text-slate-700">
                      Inactive
                    </span>
                  </div>
                  <p className="mt-1 text-sm text-slate-600">{plan.description || "No description"}</p>
                  <div className="mt-4 flex items-end justify-between">
                    <span className="text-sm font-semibold text-slate-500">
                      {normalizePlanType(plan.membershipType)} | {plan.duration}
                    </span>
                    <span className="text-lg font-bold text-slate-700">INR {plan.price}</span>
                  </div>
                  {isAdmin && (
                    <div className="mt-4 flex gap-2">
                      <button
                        type="button"
                        onClick={() => handleStatusChange(plan.id, true)}
                        className="rounded-md border border-emerald-300 px-3 py-1 text-xs font-semibold text-emerald-700 hover:bg-emerald-50"
                      >
                        Activate
                      </button>
                    </div>
                  )}
                </article>
              ))}
            </div>
          </div>

          {plans.length === 0 && (
            <p className="rounded-xl border border-slate-200 bg-white p-6 text-center text-slate-500 sm:col-span-2 xl:col-span-3">
              No plans found.
            </p>
          )}
        </div>
      )}
    </section>
  );
}

export default PlansPage;
