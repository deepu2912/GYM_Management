import { useEffect, useRef, useState, type ChangeEvent, type FormEvent, type HTMLAttributes } from "react";
import { api, getApiErrorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";

type InvoiceSettingsForm = {
  businessName: string;
  email: string;
  phone: string;
  addressLine: string;
  city: string;
  state: string;
  pincode: string;
  gstNumber: string;
  bankName: string;
  accountName: string;
  accountNumber: string;
  ifscCode: string;
  upiId: string;
  authorizedSignatory: string;
  termsAndConditions: string;
};

const emptyForm: InvoiceSettingsForm = {
  businessName: "",
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
  authorizedSignatory: "",
  termsAndConditions:
    "1. Subscription payments are non-refundable once activated.\n2. Access continues till plan validity date.\n3. Late renewal may block new member and plan creation.",
};

function isValidEmail(value: string) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

function SuperAdminInvoiceSettingsPage() {
  const { user } = useAuth();
  const [form, setForm] = useState<InvoiceSettingsForm>(emptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});
  const fieldRefs = useRef<Record<string, HTMLInputElement | HTMLTextAreaElement | null>>({});
  const isSuperAdmin = user?.role === "SuperAdmin";

  const setFieldRef = (name: string) => (element: HTMLInputElement | HTMLTextAreaElement | null) => {
    fieldRefs.current[name] = element;
  };

  const focusField = (name: string) => {
    const target = fieldRefs.current[name];
    if (!target) {
      return;
    }
    target.focus();
    target.scrollIntoView({ behavior: "smooth", block: "center" });
  };

  const clearFieldError = (name: string) => {
    setFormErrors((prev) => {
      if (!prev[name]) {
        return prev;
      }
      const next = { ...prev };
      delete next[name];
      return next;
    });
  };

  const loadSettings = async () => {
    setLoading(true);
    setError("");
    try {
      const response = await api.get("/api/super-admin/invoice-settings");
      const data = (response.data ?? {}) as Partial<InvoiceSettingsForm>;
      setForm({
        businessName: data.businessName ?? "",
        email: data.email ?? "",
        phone: data.phone ?? "",
        addressLine: data.addressLine ?? "",
        city: data.city ?? "",
        state: data.state ?? "",
        pincode: data.pincode ?? "",
        gstNumber: data.gstNumber ?? "",
        bankName: data.bankName ?? "",
        accountName: data.accountName ?? "",
        accountNumber: data.accountNumber ?? "",
        ifscCode: data.ifscCode ?? "",
        upiId: data.upiId ?? "",
        authorizedSignatory: data.authorizedSignatory ?? "",
        termsAndConditions: data.termsAndConditions?.trim()
          ? data.termsAndConditions
          : emptyForm.termsAndConditions,
      });
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to load invoice settings."));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isSuperAdmin) {
      loadSettings();
    } else {
      setLoading(false);
    }
  }, [isSuperAdmin]);

  const handleChange = (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = event.target;

    if (name === "phone") {
      setForm((prev) => ({ ...prev, phone: value.replace(/\D/g, "").slice(0, 10) }));
      clearFieldError(name);
      return;
    }

    if (name === "pincode") {
      setForm((prev) => ({ ...prev, pincode: value.replace(/\D/g, "").slice(0, 6) }));
      clearFieldError(name);
      return;
    }

    setForm((prev) => ({ ...prev, [name]: value }));
    clearFieldError(name);
  };

  const handleSave = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError("");
    setSuccess("");
    setFormErrors({});

    const trimmed = {
      businessName: form.businessName.trim(),
      email: form.email.trim(),
      phone: form.phone.trim(),
      addressLine: form.addressLine.trim(),
      city: form.city.trim(),
      state: form.state.trim(),
      pincode: form.pincode.trim(),
      gstNumber: form.gstNumber.trim(),
      bankName: form.bankName.trim(),
      accountName: form.accountName.trim(),
      accountNumber: form.accountNumber.trim(),
      ifscCode: form.ifscCode.trim(),
      upiId: form.upiId.trim(),
      authorizedSignatory: form.authorizedSignatory.trim(),
      termsAndConditions: form.termsAndConditions.trim(),
    };

    const nextErrors: Record<string, string> = {};
    if (!trimmed.businessName) nextErrors.businessName = "Business name is required.";
    if (!isValidEmail(trimmed.email)) nextErrors.email = "A valid email is required.";
    if (!/^\d{10}$/.test(trimmed.phone)) nextErrors.phone = "Phone must be exactly 10 digits.";
    if (!trimmed.addressLine) nextErrors.addressLine = "Address is required.";
    if (!trimmed.city) nextErrors.city = "City is required.";
    if (!trimmed.state) nextErrors.state = "State is required.";
    if (!/^\d{6}$/.test(trimmed.pincode)) nextErrors.pincode = "Pincode must be exactly 6 digits.";
    if (!trimmed.gstNumber) nextErrors.gstNumber = "GST number is required.";
    if (!trimmed.bankName) nextErrors.bankName = "Bank name is required.";
    if (!trimmed.accountName) nextErrors.accountName = "Account holder name is required.";
    if (!trimmed.accountNumber) nextErrors.accountNumber = "Account number is required.";
    if (!trimmed.ifscCode) nextErrors.ifscCode = "IFSC code is required.";
    if (!trimmed.authorizedSignatory) nextErrors.authorizedSignatory = "Signatory name is required.";
    if (!trimmed.termsAndConditions) nextErrors.termsAndConditions = "Terms & conditions are required.";

    if (Object.keys(nextErrors).length > 0) {
      setFormErrors(nextErrors);
      const firstField = Object.keys(nextErrors)[0];
      focusField(firstField);
      setError("Please fix the highlighted fields.");
      return;
    }

    setSaving(true);
    try {
      await api.put("/api/super-admin/invoice-settings", {
        businessName: trimmed.businessName,
        email: trimmed.email,
        phone: trimmed.phone,
        addressLine: trimmed.addressLine,
        city: trimmed.city,
        state: trimmed.state,
        pincode: trimmed.pincode,
        gstNumber: trimmed.gstNumber,
        bankName: trimmed.bankName,
        accountName: trimmed.accountName,
        accountNumber: trimmed.accountNumber,
        ifscCode: trimmed.ifscCode,
        upiId: trimmed.upiId || null,
        authorizedSignatory: trimmed.authorizedSignatory,
        termsAndConditions: trimmed.termsAndConditions,
      });
      setSuccess("Invoice settings saved successfully.");
      setForm((prev) => ({ ...prev, ...trimmed }));
    } catch (err: any) {
      const serverErrors = err?.response?.data?.errors;
      if (serverErrors && typeof serverErrors === "object") {
        const map: Record<string, string> = {};
        Object.entries(serverErrors).forEach(([key, value]) => {
          const targetField = `${key.charAt(0).toLowerCase()}${key.slice(1)}`;
          if (Array.isArray(value) && value.length > 0) {
            map[targetField] = value[0] as string;
          }
        });

        if (Object.keys(map).length > 0) {
          setFormErrors(map);
          focusField(Object.keys(map)[0]);
          setError("Please fix the highlighted fields.");
        } else {
          setError(getApiErrorMessage(err, "Unable to save invoice settings."));
        }
      } else {
        setError(getApiErrorMessage(err, "Unable to save invoice settings."));
      }
    } finally {
      setSaving(false);
    }
  };

  if (!isSuperAdmin) {
    return <p className="text-slate-600">Only Super Admin can access this page.</p>;
  }

  return (
    <section className="space-y-5">
      <div>
        <h2 className="text-2xl font-bold text-slate-900">Invoice Settings</h2>
        <p className="mt-1 text-sm text-slate-600">
          Configure the super-admin business details used as invoice issuer details for gym subscription billing.
        </p>
      </div>

      <div className="rounded-xl border border-indigo-200 bg-indigo-50 p-4">
        <h3 className="text-sm font-bold uppercase tracking-[0.14em] text-indigo-700">Required Invoice Parameters</h3>
        <div className="mt-2 grid grid-cols-1 gap-2 text-sm text-indigo-900 md:grid-cols-2">
          <p>Business name, email, phone, and billing address</p>
          <p>GST number, bank account details, and IFSC code</p>
          <p>Authorized signatory name for invoice footer</p>
          <p>Standard terms and conditions shown in invoice</p>
        </div>
      </div>

      {error && <p className="rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">{error}</p>}
      {success && <p className="rounded-lg bg-emerald-50 px-3 py-2 text-sm font-medium text-emerald-700">{success}</p>}

      {loading ? (
        <p className="text-slate-600">Loading invoice settings...</p>
      ) : (
        <form onSubmit={handleSave} className="rounded-xl border border-slate-200 bg-white p-4">
          <div className="grid grid-cols-1 gap-4 xl:grid-cols-2">
            <div className="rounded-lg border border-slate-200 bg-slate-50 p-3">
              <h4 className="text-sm font-bold text-slate-800">Issuer Details</h4>
              <div className="mt-3 grid grid-cols-1 gap-3 md:grid-cols-2">
                <Field
                  name="businessName"
                  label="Business Name"
                  value={form.businessName}
                  onChange={handleChange}
                  error={formErrors.businessName}
                  setFieldRef={setFieldRef}
                />
                <Field
                  name="email"
                  type="email"
                  label="Invoice Email"
                  value={form.email}
                  onChange={handleChange}
                  error={formErrors.email}
                  setFieldRef={setFieldRef}
                />
                <Field
                  name="phone"
                  label="Phone"
                  value={form.phone}
                  onChange={handleChange}
                  error={formErrors.phone}
                  maxLength={10}
                  inputMode="numeric"
                  setFieldRef={setFieldRef}
                />
                <Field
                  name="pincode"
                  label="Pincode"
                  value={form.pincode}
                  onChange={handleChange}
                  error={formErrors.pincode}
                  maxLength={6}
                  inputMode="numeric"
                  setFieldRef={setFieldRef}
                />
                <Field
                  name="city"
                  label="City"
                  value={form.city}
                  onChange={handleChange}
                  error={formErrors.city}
                  setFieldRef={setFieldRef}
                />
                <Field
                  name="state"
                  label="State"
                  value={form.state}
                  onChange={handleChange}
                  error={formErrors.state}
                  setFieldRef={setFieldRef}
                />
                <Field
                  name="gstNumber"
                  label="GST Number"
                  value={form.gstNumber}
                  onChange={handleChange}
                  error={formErrors.gstNumber}
                  setFieldRef={setFieldRef}
                />
                <Field
                  name="authorizedSignatory"
                  label="Authorized Signatory"
                  value={form.authorizedSignatory}
                  onChange={handleChange}
                  error={formErrors.authorizedSignatory}
                  setFieldRef={setFieldRef}
                />
                <Field
                  name="addressLine"
                  label="Address"
                  value={form.addressLine}
                  onChange={handleChange}
                  error={formErrors.addressLine}
                  setFieldRef={setFieldRef}
                  className="md:col-span-2"
                />
              </div>
            </div>

            <div className="rounded-lg border border-slate-200 bg-slate-50 p-3">
              <h4 className="text-sm font-bold text-slate-800">Banking Details</h4>
              <div className="mt-3 grid grid-cols-1 gap-3 md:grid-cols-2">
                <Field
                  name="bankName"
                  label="Bank Name"
                  value={form.bankName}
                  onChange={handleChange}
                  error={formErrors.bankName}
                  setFieldRef={setFieldRef}
                />
                <Field
                  name="accountName"
                  label="Account Name"
                  value={form.accountName}
                  onChange={handleChange}
                  error={formErrors.accountName}
                  setFieldRef={setFieldRef}
                />
                <Field
                  name="accountNumber"
                  label="Account Number"
                  value={form.accountNumber}
                  onChange={handleChange}
                  error={formErrors.accountNumber}
                  setFieldRef={setFieldRef}
                />
                <Field
                  name="ifscCode"
                  label="IFSC Code"
                  value={form.ifscCode}
                  onChange={handleChange}
                  error={formErrors.ifscCode}
                  setFieldRef={setFieldRef}
                />
                <Field
                  name="upiId"
                  label="UPI ID (Optional)"
                  value={form.upiId}
                  onChange={handleChange}
                  error={formErrors.upiId}
                  setFieldRef={setFieldRef}
                />
              </div>
            </div>
          </div>

          <label className="mt-4 block">
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Terms & Conditions</span>
            <textarea
              id="field-termsAndConditions"
              ref={setFieldRef("termsAndConditions")}
              name="termsAndConditions"
              value={form.termsAndConditions}
              onChange={handleChange}
              rows={5}
              aria-invalid={Boolean(formErrors.termsAndConditions)}
              aria-describedby={formErrors.termsAndConditions ? "error-termsAndConditions" : undefined}
              className={`w-full rounded-lg border bg-white px-3 py-2 text-sm outline-none focus:border-orange-500 ${
                formErrors.termsAndConditions ? "border-red-500" : "border-slate-300"
              }`}
            />
            {formErrors.termsAndConditions && (
              <span id="error-termsAndConditions" className="mt-1 block text-xs font-medium text-red-600">
                {formErrors.termsAndConditions}
              </span>
            )}
          </label>

          <div className="mt-4 flex gap-2">
            <button
              type="submit"
              disabled={saving}
              className="rounded-md bg-orange-500 px-4 py-2 text-sm font-semibold text-white hover:bg-orange-600 disabled:opacity-60"
            >
              {saving ? "Saving..." : "Save Invoice Settings"}
            </button>
            <button
              type="button"
              onClick={loadSettings}
              disabled={saving}
              className="rounded-md border border-slate-300 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100 disabled:opacity-60"
            >
              Reload
            </button>
          </div>
        </form>
      )}
    </section>
  );
}

function Field({
  name,
  label,
  value,
  onChange,
  error,
  setFieldRef,
  className,
  type = "text",
  maxLength,
  inputMode,
}: {
  name: string;
  label: string;
  value: string;
  onChange: (event: ChangeEvent<HTMLInputElement>) => void;
  error?: string;
  setFieldRef: (name: string) => (element: HTMLInputElement | HTMLTextAreaElement | null) => void;
  className?: string;
  type?: string;
  maxLength?: number;
  inputMode?: HTMLAttributes<HTMLInputElement>["inputMode"];
}) {
  return (
    <label className={className ?? ""}>
      <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">{label}</span>
      <input
        id={`field-${name}`}
        ref={setFieldRef(name)}
        name={name}
        type={type}
        value={value}
        onChange={onChange}
        maxLength={maxLength}
        inputMode={inputMode}
        aria-invalid={Boolean(error)}
        aria-describedby={error ? `error-${name}` : undefined}
        className={`h-10 w-full rounded-lg border bg-white px-3 text-sm outline-none focus:border-orange-500 ${
          error ? "border-red-500" : "border-slate-300"
        }`}
      />
      {error && (
        <span id={`error-${name}`} className="mt-1 block text-xs font-medium text-red-600">
          {error}
        </span>
      )}
    </label>
  );
}

export default SuperAdminInvoiceSettingsPage;
