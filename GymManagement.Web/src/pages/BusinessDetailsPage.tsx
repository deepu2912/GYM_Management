import { useEffect, useMemo, useState } from "react";
import { api, getApiErrorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";

const emptyForm = {
  gymName: "",
  email: "",
  phone: "",
  addressLine: "",
  city: "",
  state: "",
  pincode: "",
  gstNumber: "",
  bankName: "",
  accountName: "",
  accountNumber: "",
  ifscCode: "",
  upiId: "",
  logoDataUri: "",
  hsnSacCode: "9997",
  gstRatePercent: "18",
  isGstApplicable: true,
};

const formFields = [
  { name: "gymName", label: "Gym Name", type: "text", required: true, full: false },
  { name: "email", label: "Email", type: "email", required: true, full: false },
  { name: "phone", label: "Phone", type: "text", required: true, full: false },
  { name: "addressLine", label: "Address", type: "text", required: true, full: true },
  { name: "city", label: "City", type: "text", required: true, full: false },
  { name: "state", label: "State", type: "text", required: true, full: false },
  { name: "pincode", label: "Pincode", type: "text", required: true, full: false },
  { name: "bankName", label: "Bank Name", type: "text", required: true, full: false },
  { name: "accountName", label: "Account Name", type: "text", required: true, full: false },
  { name: "accountNumber", label: "Account Number", type: "text", required: true, full: false },
  { name: "ifscCode", label: "IFSC Code", type: "text", required: true, full: false },
  { name: "upiId", label: "UPI ID", type: "text", required: false, full: false },
] as const;

function BusinessDetailsPage() {
  const { user } = useAuth();
  const isAdmin = useMemo(() => user?.role === "Admin", [user?.role]);
  const [form, setForm] = useState(emptyForm);
  const [originalForm, setOriginalForm] = useState(emptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const fetchProfile = async () => {
    setLoading(true);
    setError("");
    try {
      const response = await api.get("/api/gymprofile");
      const profile = response.data ?? {};
      setForm({
        gymName: profile.gymName ?? "",
        email: profile.email ?? "",
        phone: profile.phone ?? "",
        addressLine: profile.addressLine ?? "",
        city: profile.city ?? "",
        state: profile.state ?? "",
        pincode: profile.pincode ?? "",
        gstNumber: profile.gstNumber ?? "",
        bankName: profile.bankName ?? "",
        accountName: profile.accountName ?? "",
        accountNumber: profile.accountNumber ?? "",
        ifscCode: profile.ifscCode ?? "",
        upiId: profile.upiId ?? "",
        logoDataUri: profile.logoDataUri ?? "",
        hsnSacCode: profile.hsnSacCode ?? "9997",
        gstRatePercent: profile.gstRatePercent?.toString?.() ?? "18",
        isGstApplicable: profile.isGstApplicable ?? true,
      });
      setOriginalForm({
        gymName: profile.gymName ?? "",
        email: profile.email ?? "",
        phone: profile.phone ?? "",
        addressLine: profile.addressLine ?? "",
        city: profile.city ?? "",
        state: profile.state ?? "",
        pincode: profile.pincode ?? "",
        gstNumber: profile.gstNumber ?? "",
        bankName: profile.bankName ?? "",
        accountName: profile.accountName ?? "",
        accountNumber: profile.accountNumber ?? "",
        ifscCode: profile.ifscCode ?? "",
        upiId: profile.upiId ?? "",
        logoDataUri: profile.logoDataUri ?? "",
        hsnSacCode: profile.hsnSacCode ?? "9997",
        gstRatePercent: profile.gstRatePercent?.toString?.() ?? "18",
        isGstApplicable: profile.isGstApplicable ?? true,
      });
    } catch (err) {
      if (err.response?.status === 404) {
        setForm(emptyForm);
        setOriginalForm(emptyForm);
      } else {
        setError(getApiErrorMessage(err, "Unable to load business details."));
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchProfile();
  }, []);

  const handleChange = (event) => {
    const { name, value, type, checked } = event.target;
    setForm((prev) => ({ ...prev, [name]: type === "checkbox" ? checked : value }));
  };

  const handleLogoChange = (event) => {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      const result = typeof reader.result === "string" ? reader.result : "";
      setForm((prev) => ({ ...prev, logoDataUri: result }));
    };
    reader.readAsDataURL(file);
  };

  const handleSubmit = async () => {
    if (!isAdmin || !isEditing) {
      return;
    }

    setSaving(true);
    setError("");
    setSuccess("");
    try {
      await api.put("/api/gymprofile", {
        gymName: form.gymName.trim(),
        email: form.email.trim(),
        phone: form.phone.trim(),
        addressLine: form.addressLine.trim(),
        city: form.city.trim(),
        state: form.state.trim(),
        pincode: form.pincode.trim(),
        gstNumber: form.isGstApplicable ? form.gstNumber.trim() : "",
        bankName: form.bankName.trim(),
        accountName: form.accountName.trim(),
        accountNumber: form.accountNumber.trim(),
        ifscCode: form.ifscCode.trim(),
        upiId: form.upiId.trim() || null,
        logoDataUri: form.logoDataUri || null,
        hsnSacCode: form.hsnSacCode.trim() || "9997",
        gstRatePercent: Number(form.gstRatePercent || 0),
        isGstApplicable: form.isGstApplicable,
      });
      setSuccess("Business details saved successfully.");
      setIsEditing(false);
      await fetchProfile();
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to save business details."));
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = () => {
    setError("");
    setSuccess("");
    setIsEditing(true);
  };

  const handleCancel = () => {
    setForm(originalForm);
    setError("");
    setSuccess("");
    setIsEditing(false);
  };

  if (!isAdmin) {
    return (
      <section>
        <h2 className="text-2xl font-bold text-slate-900">Business Details</h2>
        <p className="mt-3 rounded-lg bg-amber-50 px-3 py-2 text-sm font-medium text-amber-800">
          Only Admin can manage business details for invoicing.
        </p>
      </section>
    );
  }

  return (
    <section>
      <div>
        <h2 className="text-2xl font-bold text-slate-900">Business Details</h2>
        <p className="mt-1 text-sm text-slate-600">
          Configure invoice profile details to avoid fallback N/A values in generated invoices.
        </p>
      </div>

      {error && <p className="mt-4 rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">{error}</p>}
      {success && (
        <p className="mt-4 rounded-lg bg-emerald-50 px-3 py-2 text-sm font-medium text-emerald-700">{success}</p>
      )}

      {loading && <p className="mt-4 text-slate-600">Loading business details...</p>}

      {!loading && (
        <form onSubmit={(event) => event.preventDefault()} className="mt-4 rounded-xl border border-slate-200 bg-slate-50 p-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            {formFields.map((field) => (
              <label key={field.name} className={field.full ? "md:col-span-2" : ""}>
                <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">
                  {field.label}
                </span>
                <input
                  name={field.name}
                  type={field.type}
                  value={form[field.name]}
                  onChange={handleChange}
                  required={field.required}
                  disabled={!isEditing}
                  className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500 disabled:bg-slate-100 disabled:text-slate-500"
                />
              </label>
            ))}
          </div>

          <label className="mt-3 inline-flex items-center gap-2">
            <input
              name="isGstApplicable"
              type="checkbox"
              checked={form.isGstApplicable}
              onChange={handleChange}
              disabled={!isEditing}
              className="h-4 w-4 rounded border-slate-300 text-orange-500 focus:ring-orange-500"
            />
            <span className="text-sm font-semibold text-slate-700">GST Applicable (inclusive calculation)</span>
          </label>

          <div className="mt-3 grid grid-cols-1 gap-3 md:grid-cols-2">
            <label>
              <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">GST Number</span>
              <input
                name="gstNumber"
                type="text"
                value={form.gstNumber}
                onChange={handleChange}
                required={form.isGstApplicable}
                disabled={!isEditing || !form.isGstApplicable}
                className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500 disabled:bg-slate-100 disabled:text-slate-400"
                placeholder={form.isGstApplicable ? "Enter GST Number" : "Not required when GST is off"}
              />
            </label>
            <label>
              <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">HSN/SAC</span>
              <input
                name="hsnSacCode"
                type="text"
                value={form.hsnSacCode}
                onChange={handleChange}
                disabled={!isEditing}
                className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500 disabled:bg-slate-100 disabled:text-slate-500"
                placeholder="e.g. 9997"
              />
            </label>
            <label>
              <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">GST %</span>
              <input
                name="gstRatePercent"
                type="number"
                min={0}
                max={100}
                step="0.01"
                value={form.gstRatePercent}
                onChange={handleChange}
                disabled={!isEditing || !form.isGstApplicable}
                className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500 disabled:bg-slate-100 disabled:text-slate-500"
              />
            </label>
          </div>

          <div className="mt-3 grid grid-cols-1 gap-3 md:grid-cols-2">
            <label>
              <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">
                Brand Logo
              </span>
              <input
                type="file"
                accept="image/*"
                onChange={handleLogoChange}
                disabled={!isEditing}
                className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none file:mr-3 file:rounded-md file:border-0 file:bg-orange-100 file:px-3 file:py-1.5 file:text-xs file:font-semibold file:text-orange-700 hover:file:bg-orange-200 disabled:bg-slate-100 disabled:text-slate-500"
              />
              <p className="mt-1 text-xs text-slate-500">Used in invoice PDF header.</p>
            </label>
            <div>
              <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Preview</span>
              <div className="flex h-24 items-center justify-center rounded-lg border border-slate-300 bg-white">
                {form.logoDataUri ? (
                  <img src={form.logoDataUri} alt="Logo preview" className="max-h-20 max-w-full object-contain" />
                ) : (
                  <span className="text-xs text-slate-400">No logo uploaded</span>
                )}
              </div>
            </div>
          </div>

          <div className="mt-4 flex gap-2">
            {!isEditing ? (
              <button
                type="button"
                onClick={handleEdit}
                className="rounded-lg bg-orange-500 px-4 py-2 text-sm font-semibold text-white hover:bg-orange-600"
              >
                Edit Details
              </button>
            ) : (
              <>
                <button
                  type="button"
                  onClick={handleSubmit}
                  disabled={saving}
                  className="rounded-lg bg-orange-500 px-4 py-2 text-sm font-semibold text-white hover:bg-orange-600 disabled:opacity-60"
                >
                  {saving ? "Updating..." : "Update Details"}
                </button>
                <button
                  type="button"
                  onClick={handleCancel}
                  disabled={saving}
                  className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100 disabled:opacity-60"
                >
                  Cancel
                </button>
              </>
            )}
          </div>
        </form>
      )}
    </section>
  );
}

export default BusinessDetailsPage;
