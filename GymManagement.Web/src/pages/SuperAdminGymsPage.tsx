import { useEffect, useMemo, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { api, getApiErrorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";

const INDIAN_STATES = [
  "Andhra Pradesh",
  "Arunachal Pradesh",
  "Assam",
  "Bihar",
  "Chhattisgarh",
  "Goa",
  "Gujarat",
  "Haryana",
  "Himachal Pradesh",
  "Jharkhand",
  "Karnataka",
  "Kerala",
  "Madhya Pradesh",
  "Maharashtra",
  "Manipur",
  "Meghalaya",
  "Mizoram",
  "Nagaland",
  "Odisha",
  "Punjab",
  "Rajasthan",
  "Sikkim",
  "Tamil Nadu",
  "Telangana",
  "Tripura",
  "Uttar Pradesh",
  "Uttarakhand",
  "West Bengal",
  "Andaman and Nicobar Islands",
  "Chandigarh",
  "Dadra and Nagar Haveli and Daman and Diu",
  "Delhi",
  "Jammu and Kashmir",
  "Ladakh",
  "Lakshadweep",
  "Puducherry",
];

const CITIES_BY_STATE = {
  "Andhra Pradesh": ["Visakhapatnam", "Vijayawada", "Guntur", "Tirupati"],
  "Arunachal Pradesh": ["Itanagar", "Naharlagun", "Tawang"],
  Assam: ["Guwahati", "Dibrugarh", "Silchar"],
  Bihar: ["Patna", "Gaya", "Muzaffarpur"],
  Chhattisgarh: ["Raipur", "Bhilai", "Bilaspur"],
  Goa: ["Panaji", "Margao", "Vasco da Gama"],
  Gujarat: ["Ahmedabad", "Surat", "Vadodara", "Rajkot"],
  Haryana: ["Gurugram", "Faridabad", "Panipat"],
  "Himachal Pradesh": ["Shimla", "Dharamshala", "Mandi"],
  Jharkhand: ["Ranchi", "Jamshedpur", "Dhanbad"],
  Karnataka: ["Bengaluru", "Mysuru", "Mangaluru", "Hubballi"],
  Kerala: ["Thiruvananthapuram", "Kochi", "Kozhikode"],
  "Madhya Pradesh": ["Bhopal", "Indore", "Jabalpur", "Gwalior"],
  Maharashtra: ["Mumbai", "Pune", "Nagpur", "Nashik"],
  Manipur: ["Imphal", "Thoubal", "Bishnupur"],
  Meghalaya: ["Shillong", "Tura", "Nongpoh"],
  Mizoram: ["Aizawl", "Lunglei", "Champhai"],
  Nagaland: ["Kohima", "Dimapur", "Mokokchung"],
  Odisha: ["Bhubaneswar", "Cuttack", "Rourkela"],
  Punjab: ["Ludhiana", "Amritsar", "Jalandhar"],
  Rajasthan: ["Jaipur", "Jodhpur", "Udaipur", "Kota"],
  Sikkim: ["Gangtok", "Namchi", "Gyalshing"],
  "Tamil Nadu": ["Chennai", "Coimbatore", "Madurai", "Salem"],
  Telangana: ["Hyderabad", "Warangal", "Nizamabad"],
  Tripura: ["Agartala", "Udaipur", "Dharmanagar"],
  "Uttar Pradesh": ["Lucknow", "Kanpur", "Varanasi", "Noida"],
  Uttarakhand: ["Dehradun", "Haridwar", "Haldwani"],
  "West Bengal": ["Kolkata", "Howrah", "Durgapur", "Siliguri"],
  "Andaman and Nicobar Islands": ["Port Blair"],
  Chandigarh: ["Chandigarh"],
  "Dadra and Nagar Haveli and Daman and Diu": ["Daman", "Silvassa"],
  Delhi: ["New Delhi", "Delhi"],
  "Jammu and Kashmir": ["Srinagar", "Jammu"],
  Ladakh: ["Leh", "Kargil"],
  Lakshadweep: ["Kavaratti"],
  Puducherry: ["Puducherry", "Karaikal"],
};

const emptyForm = {
  gymName: "",
  email: "",
  phone: "",
  addressLine: "",
  city: "",
  state: "",
  pincode: "",
  gstNumber: "",
  hsnSacCode: "9997",
  gstRatePercent: "18",
  isGstApplicable: true,
  bankName: "",
  accountName: "",
  accountNumber: "",
  ifscCode: "",
  upiId: "",
  isActive: true,
  adminName: "",
  adminEmail: "",
};

function isValidEmail(value) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

function SuperAdminGymsPage() {
  const { user } = useAuth();
  const [gyms, setGyms] = useState([]);
  const [searchQuery, setSearchQuery] = useState("");
  const [form, setForm] = useState(emptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [editId, setEditId] = useState(null);
  const [showForm, setShowForm] = useState(false);
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});
  const isSuperAdmin = user?.role === "SuperAdmin";
  const cityOptions = form.state ? CITIES_BY_STATE[form.state] ?? [] : [];
  const resolvedCityOptions =
    form.city && !cityOptions.includes(form.city) ? [...cityOptions, form.city] : cityOptions;
  const fieldRefs = useRef({});
  const filteredGyms = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();
    if (!query) {
      return gyms;
    }

    return gyms.filter((gym) => {
      const haystack = [
        gym.gymName,
        gym.city,
        gym.email,
        gym.phone,
        gym.admin?.name,
        gym.admin?.email,
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();
      return haystack.includes(query);
    });
  }, [gyms, searchQuery]);

  const setFieldRef = (fieldName) => (element) => {
    fieldRefs.current[fieldName] = element;
  };

  const focusField = (fieldName) => {
    const element = fieldRefs.current[fieldName];
    if (!element) {
      return;
    }
    element.focus();
    element.scrollIntoView({ behavior: "smooth", block: "center" });
  };

  const clearFieldError = (fieldName) => {
    setFormErrors((prev) => {
      if (!prev[fieldName]) {
        return prev;
      }
      const next = { ...prev };
      delete next[fieldName];
      return next;
    });
  };

  const fetchGyms = async () => {
    setLoading(true);
    setError("");
    try {
      const response = await api.get("/api/gyms");
      setGyms(response.data ?? []);
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to load gyms."));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isSuperAdmin) {
      fetchGyms();
    } else {
      setLoading(false);
    }
  }, [isSuperAdmin]);

  const handleChange = (event) => {
    const { name, value, type, checked } = event.target;

    if (name === "phone") {
      setForm((prev) => ({
        ...prev,
        phone: value.replace(/\D/g, "").slice(0, 10),
      }));
      clearFieldError("phone");
      return;
    }

    if (name === "pincode") {
      setForm((prev) => ({
        ...prev,
        pincode: value.replace(/\D/g, "").slice(0, 6),
      }));
      clearFieldError("pincode");
      return;
    }

    if (name === "state") {
      setForm((prev) => ({
        ...prev,
        state: value,
        city: "",
      }));
      clearFieldError("state");
      clearFieldError("city");
      return;
    }

    setForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
    clearFieldError(name);
  };

  const closeForm = () => {
    setShowForm(false);
    setEditId(null);
    setForm(emptyForm);
    setFormErrors({});
  };

  const handleCreateClick = () => {
    setError("");
    setSuccess("");
    setEditId(null);
    setForm(emptyForm);
    setFormErrors({});
    setShowForm(true);
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setError("");
    setSuccess("");
    setFormErrors({});

    const nextErrors: Record<string, string> = {};
    if (!isValidEmail(form.email.trim())) {
      nextErrors.email = "Gym Email must be a valid email address.";
    }
    if (!isValidEmail(form.adminEmail.trim())) {
      nextErrors.adminEmail = "Admin Email must be a valid email address.";
    }
    if (!/^\d{10}$/.test(form.phone.trim())) {
      nextErrors.phone = "Phone must be exactly 10 digits.";
    }
    if (!/^\d{6}$/.test(form.pincode.trim())) {
      nextErrors.pincode = "Pincode must be exactly 6 digits.";
    }
    if (!INDIAN_STATES.includes(form.state)) {
      nextErrors.state = "Please select a valid State.";
    }
    if (!form.city || !resolvedCityOptions.includes(form.city)) {
      nextErrors.city = "Please select a valid City.";
    }
    const gstRate = Number(form.gstRatePercent);
    if (!Number.isFinite(gstRate) || gstRate < 0 || gstRate > 100) {
      nextErrors.gstRatePercent = "GST Rate % must be between 0 and 100.";
    }

    if (Object.keys(nextErrors).length > 0) {
      setFormErrors(nextErrors);
      const firstField = Object.keys(nextErrors)[0];
      focusField(firstField);
      setError("Please fix the highlighted fields.");
      return;
    }

    setSaving(true);

    try {
      const payload = {
        gymName: form.gymName.trim(),
        email: form.email.trim(),
        phone: form.phone.trim(),
        addressLine: form.addressLine.trim(),
        city: form.city.trim(),
        state: form.state.trim(),
        pincode: form.pincode.trim(),
        gstNumber: form.gstNumber.trim(),
        hsnSacCode: form.hsnSacCode.trim() || "9997",
        gstRatePercent: Number(form.gstRatePercent || 0),
        isGstApplicable: form.isGstApplicable,
        bankName: form.bankName.trim(),
        accountName: form.accountName.trim(),
        accountNumber: form.accountNumber.trim(),
        ifscCode: form.ifscCode.trim(),
        upiId: form.upiId.trim() || null,
        isActive: form.isActive,
        adminName: form.adminName.trim(),
        adminEmail: form.adminEmail.trim(),
      };

      if (editId) {
        await api.put(`/api/gyms/${editId}`, payload);
        setSuccess("Gym updated successfully.");
      } else {
        const response = await api.post("/api/gyms", payload);
        const onboardingEmailSent = response.data?.onboardingEmailSent;
        const onboardingEmailError = response.data?.onboardingEmailError;

        if (onboardingEmailSent) {
          setSuccess("Gym created and onboarding email sent with login credentials.");
        } else if (onboardingEmailError) {
          setSuccess(`Gym created, but onboarding email failed: ${onboardingEmailError}`);
        } else {
          setSuccess("Gym created. Onboarding email status unavailable.");
        }
      }

      closeForm();
      await fetchGyms();
    } catch (err) {
      const serverErrors = err?.response?.data?.errors;
      if (serverErrors && typeof serverErrors === "object") {
        const fieldNameMap = {
          GymName: "gymName",
          Email: "email",
          Phone: "phone",
          AddressLine: "addressLine",
          City: "city",
          State: "state",
          Pincode: "pincode",
          GstNumber: "gstNumber",
          HsnSacCode: "hsnSacCode",
          GstRatePercent: "gstRatePercent",
          IsGstApplicable: "isGstApplicable",
          BankName: "bankName",
          AccountName: "accountName",
          AccountNumber: "accountNumber",
          IfscCode: "ifscCode",
          UpiId: "upiId",
          IsActive: "isActive",
          AdminName: "adminName",
          AdminEmail: "adminEmail",
        };

        const mappedErrors: Record<string, string> = {};
        Object.entries(serverErrors).forEach(([apiField, messages]) => {
          const targetField = fieldNameMap[apiField] ?? `${apiField.charAt(0).toLowerCase()}${apiField.slice(1)}`;
          if (Array.isArray(messages) && messages.length > 0) {
            mappedErrors[targetField] = messages[0];
          }
        });

        if (Object.keys(mappedErrors).length > 0) {
          setFormErrors(mappedErrors);
          focusField(Object.keys(mappedErrors)[0]);
          setError("Please fix the highlighted fields.");
        } else {
          setError(getApiErrorMessage(err, "Unable to save gym."));
        }
      } else {
        setError(getApiErrorMessage(err, "Unable to save gym."));
      }
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = (gym) => {
    setEditId(gym.id);
    setForm({
      gymName: gym.gymName ?? "",
      email: gym.email ?? "",
      phone: gym.phone ?? "",
      addressLine: gym.addressLine ?? "",
      city: gym.city ?? "",
      state: gym.state ?? "",
      pincode: gym.pincode ?? "",
      gstNumber: gym.gstNumber ?? "",
      hsnSacCode: gym.hsnSacCode ?? "9997",
      gstRatePercent: String(gym.gstRatePercent ?? 18),
      isGstApplicable: gym.isGstApplicable ?? true,
      bankName: gym.bankName ?? "",
      accountName: gym.accountName ?? "",
      accountNumber: gym.accountNumber ?? "",
      ifscCode: gym.ifscCode ?? "",
      upiId: gym.upiId ?? "",
      isActive: gym.isActive ?? true,
      adminName: gym.admin?.name ?? "",
      adminEmail: gym.admin?.email ?? "",
    });
    setError("");
    setSuccess("");
    setFormErrors({});
    setShowForm(true);
  };

  const handleToggleStatus = async (gym) => {
    setError("");
    setSuccess("");
    try {
      const targetStatus = !gym.isActive;
      await api.patch(`/api/gyms/${gym.id}/status`, { isActive: targetStatus });
      setSuccess(`Gym marked as ${targetStatus ? "Active" : "Inactive"}.`);
      await fetchGyms();
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to update gym status."));
    }
  };

  if (!isSuperAdmin) {
    return <p className="text-slate-600">Only Super Admin can access this page.</p>;
  }

  return (
    <section className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-2xl font-bold text-slate-900">Gym Setup</h2>
          <p className="mt-1 text-sm text-slate-600">
            Manage gyms and automatically onboard gym admins with emailed login credentials.
          </p>
        </div>
        <div className="flex w-full flex-wrap items-center justify-end gap-2 sm:w-auto">
          <input
            type="text"
            value={searchQuery}
            onChange={(event) => setSearchQuery(event.target.value)}
            placeholder="Search gym/city/admin/email/mobile..."
            className="h-10 min-w-[260px] rounded-lg border border-slate-300 bg-white px-3 text-sm outline-none focus:border-orange-500"
          />
          {!showForm && (
            <button
              type="button"
              onClick={handleCreateClick}
              className="rounded-lg bg-orange-500 px-4 py-2 text-sm font-semibold text-white hover:bg-orange-600"
            >
              Create Gym
            </button>
          )}
        </div>
      </div>

      {error && <p className="rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">{error}</p>}
      {success && <p className="rounded-lg bg-emerald-50 px-3 py-2 text-sm font-medium text-emerald-700">{success}</p>}

      {showForm && (
        <form onSubmit={handleSubmit} className="rounded-xl border border-slate-200 bg-white p-4">
          <h3 className="text-lg font-bold text-slate-900">{editId ? "Edit Gym" : "Create Gym"}</h3>

          <div className="mt-4 grid grid-cols-1 gap-4 xl:grid-cols-2">
            <div className="rounded-lg border border-slate-200 bg-slate-50 p-3">
              <h4 className="text-sm font-bold text-slate-800">Basic Details</h4>
              <div className="mt-3 grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-3">
                {[
                  { name: "gymName", label: "Gym Name", type: "text" },
                  { name: "email", label: "Gym Email", type: "email" },
                  { name: "addressLine", label: "Address Line", type: "text" },
                  { name: "adminName", label: "Admin Name", type: "text" },
                  { name: "adminEmail", label: "Admin Email", type: "email" },
                ].map(({ name, label, type }) => (
                  <label
                    key={name}
                    className={name === "addressLine" ? "md:col-span-2 xl:col-span-3" : ""}
                  >
                    <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">{label}</span>
                    <input
                      id={`field-${name}`}
                      ref={setFieldRef(name)}
                      name={name}
                      type={type}
                      value={form[name]}
                      onChange={handleChange}
                      required
                      aria-invalid={Boolean(formErrors[name])}
                      aria-describedby={formErrors[name] ? `error-${name}` : undefined}
                      className={`h-10 w-full rounded-lg border bg-white px-3 text-sm outline-none focus:border-orange-500 ${
                        formErrors[name] ? "border-red-500" : "border-slate-300"
                      }`}
                    />
                    {formErrors[name] && (
                      <span id={`error-${name}`} className="mt-1 block text-xs font-medium text-red-600">
                        {formErrors[name]}
                      </span>
                    )}
                  </label>
                ))}

                <label>
                  <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Phone</span>
                  <input
                    id="field-phone"
                    ref={setFieldRef("phone")}
                    name="phone"
                    value={form.phone}
                    onChange={handleChange}
                    required
                    inputMode="numeric"
                    pattern="\d{10}"
                    maxLength={10}
                    placeholder="10-digit mobile number"
                    aria-invalid={Boolean(formErrors.phone)}
                    aria-describedby={formErrors.phone ? "error-phone" : undefined}
                    className={`h-10 w-full rounded-lg border bg-white px-3 text-sm outline-none focus:border-orange-500 ${
                      formErrors.phone ? "border-red-500" : "border-slate-300"
                    }`}
                  />
                  {formErrors.phone && (
                    <span id="error-phone" className="mt-1 block text-xs font-medium text-red-600">
                      {formErrors.phone}
                    </span>
                  )}
                </label>

                <label>
                  <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Pincode</span>
                  <input
                    id="field-pincode"
                    ref={setFieldRef("pincode")}
                    name="pincode"
                    value={form.pincode}
                    onChange={handleChange}
                    required
                    inputMode="numeric"
                    pattern="\d{6}"
                    maxLength={6}
                    placeholder="6-digit pincode"
                    aria-invalid={Boolean(formErrors.pincode)}
                    aria-describedby={formErrors.pincode ? "error-pincode" : undefined}
                    className={`h-10 w-full rounded-lg border bg-white px-3 text-sm outline-none focus:border-orange-500 ${
                      formErrors.pincode ? "border-red-500" : "border-slate-300"
                    }`}
                  />
                  {formErrors.pincode && (
                    <span id="error-pincode" className="mt-1 block text-xs font-medium text-red-600">
                      {formErrors.pincode}
                    </span>
                  )}
                </label>

                <label>
                  <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">State</span>
                  <select
                    id="field-state"
                    ref={setFieldRef("state")}
                    name="state"
                    value={form.state}
                    onChange={handleChange}
                    required
                    aria-invalid={Boolean(formErrors.state)}
                    aria-describedby={formErrors.state ? "error-state" : undefined}
                    className={`h-10 w-full rounded-lg border bg-white px-3 text-sm outline-none focus:border-orange-500 ${
                      formErrors.state ? "border-red-500" : "border-slate-300"
                    }`}
                  >
                    <option value="">Select State</option>
                    {INDIAN_STATES.map((state) => (
                      <option key={state} value={state}>
                        {state}
                      </option>
                    ))}
                  </select>
                  {formErrors.state && (
                    <span id="error-state" className="mt-1 block text-xs font-medium text-red-600">
                      {formErrors.state}
                    </span>
                  )}
                </label>

                <label>
                  <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">City</span>
                  <select
                    id="field-city"
                    ref={setFieldRef("city")}
                    name="city"
                    value={form.city}
                    onChange={handleChange}
                    required
                    disabled={!form.state}
                    aria-invalid={Boolean(formErrors.city)}
                    aria-describedby={formErrors.city ? "error-city" : undefined}
                    className={`h-10 w-full rounded-lg border bg-white px-3 text-sm outline-none focus:border-orange-500 disabled:bg-slate-100 ${
                      formErrors.city ? "border-red-500" : "border-slate-300"
                    }`}
                  >
                    <option value="">{form.state ? "Select City" : "Select State First"}</option>
                    {resolvedCityOptions.map((city) => (
                      <option key={city} value={city}>
                        {city}
                      </option>
                    ))}
                  </select>
                  {formErrors.city && (
                    <span id="error-city" className="mt-1 block text-xs font-medium text-red-600">
                      {formErrors.city}
                    </span>
                  )}
                </label>
              </div>
            </div>

            <div className="rounded-lg border border-slate-200 bg-slate-50 p-3">
              <h4 className="text-sm font-bold text-slate-800">Bank & Tax Details</h4>
              <div className="mt-3 grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-3">
                {[
                  ["bankName", "Bank Name"],
                  ["accountName", "Account Name"],
                  ["accountNumber", "Account Number"],
                  ["ifscCode", "IFSC Code"],
                  ["upiId", "UPI ID (Optional)"],
                  ["gstNumber", "GST Number"],
                  ["hsnSacCode", "HSN/SAC Code"],
                ].map(([name, label]) => (
                  <label key={name}>
                    <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">{label}</span>
                    <input
                      id={`field-${name}`}
                      ref={setFieldRef(name)}
                      name={name}
                      value={form[name]}
                      onChange={handleChange}
                      required={name !== "upiId"}
                      aria-invalid={Boolean(formErrors[name])}
                      aria-describedby={formErrors[name] ? `error-${name}` : undefined}
                      className={`h-10 w-full rounded-lg border bg-white px-3 text-sm outline-none focus:border-orange-500 ${
                        formErrors[name] ? "border-red-500" : "border-slate-300"
                      }`}
                    />
                    {formErrors[name] && (
                      <span id={`error-${name}`} className="mt-1 block text-xs font-medium text-red-600">
                        {formErrors[name]}
                      </span>
                    )}
                  </label>
                ))}

                <label>
                  <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">GST Rate %</span>
                  <input
                    id="field-gstRatePercent"
                    ref={setFieldRef("gstRatePercent")}
                    name="gstRatePercent"
                    type="number"
                    min="0"
                    max="100"
                    step="0.01"
                    value={form.gstRatePercent}
                    onChange={handleChange}
                    aria-invalid={Boolean(formErrors.gstRatePercent)}
                    aria-describedby={formErrors.gstRatePercent ? "error-gstRatePercent" : undefined}
                    className={`h-10 w-full rounded-lg border bg-white px-3 text-sm outline-none focus:border-orange-500 ${
                      formErrors.gstRatePercent ? "border-red-500" : "border-slate-300"
                    }`}
                  />
                  {formErrors.gstRatePercent && (
                    <span id="error-gstRatePercent" className="mt-1 block text-xs font-medium text-red-600">
                      {formErrors.gstRatePercent}
                    </span>
                  )}
                </label>

                <label className="inline-flex items-center gap-2 pt-7 md:col-span-2 xl:col-span-3">
                  <input
                    type="checkbox"
                    name="isGstApplicable"
                    checked={form.isGstApplicable}
                    onChange={handleChange}
                    className="h-4 w-4 rounded border-slate-300 text-orange-500 focus:ring-orange-500"
                  />
                  <span className="text-sm font-semibold text-slate-700">GST Applicable</span>
                </label>

                {editId && (
                  <label className="inline-flex items-center gap-2 md:col-span-2 xl:col-span-3">
                    <input
                      type="checkbox"
                      name="isActive"
                      checked={form.isActive}
                      onChange={handleChange}
                      className="h-4 w-4 rounded border-slate-300 text-orange-500 focus:ring-orange-500"
                    />
                    <span className="text-sm font-semibold text-slate-700">Gym Active</span>
                  </label>
                )}
              </div>
            </div>
          </div>

          {!editId && (
            <p className="mt-3 rounded-lg border border-indigo-200 bg-indigo-50 px-3 py-2 text-[11px] font-medium text-indigo-700">
              Admin password is auto-generated and sent on email during onboarding.
            </p>
          )}

          <div className="mt-3 flex gap-2">
            <button
              type="submit"
              disabled={saving}
              className="rounded-md bg-orange-500 px-3 py-2 text-sm font-semibold text-white hover:bg-orange-600 disabled:opacity-60"
            >
              {saving ? "Saving..." : editId ? "Update Gym" : "Create Gym"}
            </button>
            <button
              type="button"
              onClick={closeForm}
              className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100"
            >
              Cancel
            </button>
          </div>
        </form>
      )}

      <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white">
        <table className="min-w-full text-left text-sm">
          <thead className="bg-slate-50 text-slate-700">
            <tr>
              <th className="px-4 py-3">Gym</th>
              <th className="px-4 py-3">City</th>
              <th className="px-4 py-3">Admin</th>
              <th className="px-4 py-3">Email</th>
              <th className="px-4 py-3">Phone</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3">Actions</th>
            </tr>
          </thead>
          <tbody>
            {!loading &&
              filteredGyms.map((gym) => (
                <tr key={gym.id} className="border-t border-slate-100">
                  <td className="px-4 py-3 font-semibold text-slate-900">
                    <Link to={`/super-admin/gyms/${gym.id}`} className="text-indigo-600 hover:text-indigo-700 hover:underline">
                      {gym.gymName}
                    </Link>
                  </td>
                  <td className="px-4 py-3">{gym.city}</td>
                  <td className="px-4 py-3">{gym.admin?.name ?? "-"}</td>
                  <td className="px-4 py-3">{gym.email}</td>
                  <td className="px-4 py-3">{gym.phone}</td>
                  <td className="px-4 py-3">{gym.isActive ? "Active" : "Inactive"}</td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2">
                      <Link
                        to={`/super-admin/gyms/${gym.id}/subscription`}
                        className="rounded-md border border-indigo-300 px-3 py-1 text-xs font-semibold text-indigo-700 hover:bg-indigo-50"
                      >
                        Subscription
                      </Link>
                      <button
                        type="button"
                        onClick={() => handleEdit(gym)}
                        className="rounded-md border border-slate-300 px-3 py-1 text-xs font-semibold text-slate-700 hover:bg-slate-100"
                      >
                        Edit
                      </button>
                      <button
                        type="button"
                        onClick={() => handleToggleStatus(gym)}
                        className={`rounded-md px-3 py-1 text-xs font-semibold ${
                          gym.isActive
                            ? "border border-amber-300 text-amber-700 hover:bg-amber-50"
                            : "border border-emerald-300 text-emerald-700 hover:bg-emerald-50"
                        }`}
                      >
                        {gym.isActive ? "Set Inactive" : "Set Active"}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            {!loading && filteredGyms.length === 0 && (
              <tr>
                <td colSpan={7} className="px-4 py-6 text-center text-slate-500">
                  {gyms.length === 0 ? "No gyms configured yet." : "No gyms match your search."}
                </td>
              </tr>
            )}
            {loading && (
              <tr>
                <td colSpan={7} className="px-4 py-6 text-center text-slate-500">
                  Loading gyms...
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}

export default SuperAdminGymsPage;
