import { useEffect, useState, type ChangeEvent, type FormEvent } from "react";
import { api, getApiErrorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";

type SubscriptionPlan = {
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

const emptyForm = {
  code: "",
  name: "",
  price: "",
  maintenanceFee: "0",
  durationMonths: "12",
  isLifetime: false,
  isMaintenance: false,
  isActive: true,
  sortOrder: "0",
  description: "",
};

function SuperAdminSubscriptionPlansPage() {
  const { user } = useAuth();
  const [plans, setPlans] = useState<SubscriptionPlan[]>([]);
  const [form, setForm] = useState(emptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [editId, setEditId] = useState<string | null>(null);
  const isSuperAdmin = user?.role === "SuperAdmin";

  const loadPlans = async () => {
    setLoading(true);
    setError("");
    try {
      const response = await api.get("/api/subscription-plans");
      setPlans((response.data ?? []) as SubscriptionPlan[]);
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to load subscription plans."));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isSuperAdmin) {
      loadPlans();
    } else {
      setLoading(false);
    }
  }, [isSuperAdmin]);

  const resetForm = () => {
    setEditId(null);
    setForm(emptyForm);
  };

  const handleChange = (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value, type } = event.target;
    const checked = "checked" in event.target ? event.target.checked : false;

    setForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const validate = () => {
    if (!form.code.trim()) {
      return "Code is required.";
    }
    if (!form.name.trim()) {
      return "Name is required.";
    }
    const price = Number(form.price);
    if (!Number.isFinite(price) || price < 0) {
      return "Price must be valid.";
    }
    const durationMonths = Number(form.durationMonths);
    if (!form.isLifetime && (!Number.isInteger(durationMonths) || durationMonths <= 0)) {
      return "Duration (months) must be greater than 0 for non-lifetime plans.";
    }
    if (form.isLifetime) {
      const m = Number(form.maintenanceFee);
      if (!Number.isFinite(m) || m < 0) return "Maintenance fee must be valid.";
    }
    return "";
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError("");
    setSuccess("");

    const validationError = validate();
    if (validationError) {
      setError(validationError);
      return;
    }

    setSaving(true);
    const payload = {
      code: form.code.trim().toUpperCase(),
      name: form.name.trim(),
      // include maintenance fee in total price for lifetime plans
      price: form.isLifetime ? Number(form.price) + Number(form.maintenanceFee || 0) : Number(form.price),
      durationMonths: Number(form.durationMonths),
      isLifetime: form.isLifetime,
      // For lifetime plans maintenance is treated as part of the plan price
      isMaintenance: form.isLifetime ? false : form.isMaintenance,
      isActive: form.isActive,
      sortOrder: Number(form.sortOrder || 0),
      description: form.description.trim(),
    };

    try {
      if (editId) {
        await api.put(`/api/subscription-plans/${editId}`, payload);
        setSuccess("Subscription plan updated successfully.");
      } else {
        await api.post("/api/subscription-plans", payload);
        setSuccess("Subscription plan created successfully.");
      }

      resetForm();
      await loadPlans();
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to save subscription plan."));
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = (plan: SubscriptionPlan) => {
    setEditId(plan.id);
    setForm({
      code: plan.code,
      name: plan.name,
      price: String(plan.price),
      maintenanceFee: "0",
      durationMonths: String(plan.durationMonths),
      isLifetime: plan.isLifetime,
      isMaintenance: plan.isMaintenance,
      isActive: plan.isActive,
      sortOrder: String(plan.sortOrder),
      description: plan.description ?? "",
    });
    setError("");
    setSuccess("");
  };

  const handleToggleStatus = async (plan: SubscriptionPlan) => {
    setError("");
    setSuccess("");
    try {
      await api.patch(`/api/subscription-plans/${plan.id}/status`, { isActive: !plan.isActive });
      setSuccess(`Plan marked as ${!plan.isActive ? "Active" : "Inactive"}.`);
      await loadPlans();
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to update plan status."));
    }
  };

  if (!isSuperAdmin) {
    return <p className="text-slate-600">Only Super Admin can access this page.</p>;
  }

  return (
    <section className="space-y-5">
      <div>
        <h2 className="text-2xl font-bold text-slate-900">Subscription Plans</h2>
        <p className="mt-1 text-sm text-slate-600">Create and manage gym subscription plans used for billing.</p>
      </div>

      {error && <p className="rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">{error}</p>}
      {success && <p className="rounded-lg bg-emerald-50 px-3 py-2 text-sm font-medium text-emerald-700">{success}</p>}

      <form onSubmit={handleSubmit} className="rounded-xl border border-slate-200 bg-white p-4">
        <h3 className="text-lg font-bold text-slate-900">{editId ? "Edit Plan" : "Create Plan"}</h3>

        <div className="mt-3 grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-4">
          <Field name="code" label="Code" value={form.code} onChange={handleChange} required />
          <Field name="name" label="Name" value={form.name} onChange={handleChange} required />
          <Field name="price" type="number" label="Price" value={form.price} onChange={handleChange} required />
          {form.isLifetime && (
            <Field name="maintenanceFee" type="number" label="Maintenance Fee" value={form.maintenanceFee} onChange={handleChange} required />
          )}
          <Field
            name="durationMonths"
            type="number"
            label="Duration (Months)"
            value={form.durationMonths}
            onChange={handleChange}
            required={!form.isLifetime}
          />
          <Field name="sortOrder" type="number" label="Sort Order" value={form.sortOrder} onChange={handleChange} />
          <label className="xl:col-span-3">
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Description</span>
            <textarea
              name="description"
              value={form.description}
              onChange={handleChange}
              rows={2}
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
            />
          </label>
        </div>

        <div className="mt-3 flex flex-wrap gap-4">
          <label className="inline-flex items-center gap-2 text-sm font-medium text-slate-700">
            <input
              type="checkbox"
              name="isLifetime"
              checked={form.isLifetime}
              onChange={handleChange}
              className="h-4 w-4 rounded border-slate-300 text-orange-500 focus:ring-orange-500"
            />
            Lifetime Plan
          </label>
          {!form.isLifetime && (
            <label className="inline-flex items-center gap-2 text-sm font-medium text-slate-700">
              <input
                type="checkbox"
                name="isMaintenance"
                checked={form.isMaintenance}
                onChange={handleChange}
                className="h-4 w-4 rounded border-slate-300 text-orange-500 focus:ring-orange-500"
              />
              Maintenance Plan
            </label>
          )}
          <label className="inline-flex items-center gap-2 text-sm font-medium text-slate-700">
            <input
              type="checkbox"
              name="isActive"
              checked={form.isActive}
              onChange={handleChange}
              className="h-4 w-4 rounded border-slate-300 text-orange-500 focus:ring-orange-500"
            />
            Active
          </label>
        </div>

        <div className="mt-4 flex gap-2">
          <button
            type="submit"
            disabled={saving}
            className="rounded-md bg-orange-500 px-3 py-2 text-sm font-semibold text-white hover:bg-orange-600 disabled:opacity-60"
          >
            {saving ? "Saving..." : editId ? "Update Plan" : "Create Plan"}
          </button>
          <button
            type="button"
            onClick={resetForm}
            className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100"
          >
            Cancel
          </button>
        </div>
      </form>

      <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white">
        <table className="min-w-full text-left text-sm">
          <thead className="bg-slate-50 text-slate-700">
            <tr>
              <th className="px-4 py-3">Code</th>
              <th className="px-4 py-3">Name</th>
              <th className="px-4 py-3">Price</th>
              <th className="px-4 py-3">Duration</th>
              <th className="px-4 py-3">Type</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3">Actions</th>
            </tr>
          </thead>
          <tbody>
            {!loading &&
              plans.map((plan) => (
                <tr key={plan.id} className="border-t border-slate-100">
                  <td className="px-4 py-3 font-semibold text-slate-900">{plan.code}</td>
                  <td className="px-4 py-3">{plan.name}</td>
                  <td className="px-4 py-3">₹{plan.price.toLocaleString()}</td>
                  <td className="px-4 py-3">{plan.durationMonths} month(s)</td>
                  <td className="px-4 py-3">{plan.isLifetime ? "Lifetime" : plan.isMaintenance ? "Maintenance" : "Standard"}</td>
                  <td className="px-4 py-3">{plan.isActive ? "Active" : "Inactive"}</td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => handleEdit(plan)}
                        className="rounded-md border border-slate-300 px-3 py-1 text-xs font-semibold text-slate-700 hover:bg-slate-100"
                      >
                        Edit
                      </button>
                      <button
                        type="button"
                        onClick={() => handleToggleStatus(plan)}
                        className={`rounded-md px-3 py-1 text-xs font-semibold ${
                          plan.isActive
                            ? "border border-amber-300 text-amber-700 hover:bg-amber-50"
                            : "border border-emerald-300 text-emerald-700 hover:bg-emerald-50"
                        }`}
                      >
                        {plan.isActive ? "Set Inactive" : "Set Active"}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            {!loading && plans.length === 0 && (
              <tr>
                <td colSpan={7} className="px-4 py-6 text-center text-slate-500">
                  No subscription plans configured yet.
                </td>
              </tr>
            )}
            {loading && (
              <tr>
                <td colSpan={7} className="px-4 py-6 text-center text-slate-500">
                  Loading plans...
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function Field({
  name,
  label,
  value,
  onChange,
  required,
  type = "text",
}: {
  name: string;
  label: string;
  value: string;
  onChange: (event: ChangeEvent<HTMLInputElement>) => void;
  required?: boolean;
  type?: string;
}) {
  return (
    <label>
      <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">{label}</span>
      <input
        name={name}
        type={type}
        value={value}
        onChange={onChange}
        required={required}
        className="h-10 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm outline-none focus:border-orange-500"
      />
    </label>
  );
}

export default SuperAdminSubscriptionPlansPage;
