import { useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { api, getApiErrorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";

const genderOptions = ["Male", "Female", "Other"];
const statusOptions = ["Active", "Inactive"];

const emptyForm = {
  name: "",
  dateOfBirth: "",
  gender: "Male",
  phone: "",
  email: "",
  addressLine: "",
  city: "",
  state: "",
  pincode: "",
  height: "0",
  weight: "0",
  joiningDate: new Date().toISOString().slice(0, 10),
  membershipStatus: "Active",
};

function toApiDateTime(dateText) {
  return `${dateText}T00:00:00`;
}

function formatDateOnly(dateText) {
  if (!dateText) {
    return "-";
  }

  const date = new Date(dateText);
  if (Number.isNaN(date.getTime())) {
    return dateText;
  }

  return date.toLocaleDateString("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
  });
}

function normalizePlanType(rawType) {
  if (typeof rawType === "string") {
    return rawType.toLowerCase() === "couple" ? "Couple" : "Single";
  }
  if (typeof rawType === "number") {
    return rawType === 1 ? "Couple" : "Single";
  }
  return "Single";
}

function MemberProfilePage() {
  const { user } = useAuth();
  const isAdmin = useMemo(() => user?.role === "Admin", [user?.role]);
  const canUploadPhoto = useMemo(() => user?.role === "Admin" || user?.role === "Member", [user?.role]);
  const navigate = useNavigate();
  const location = useLocation();
  const { memberId } = useParams();

  const [member, setMember] = useState(null);
  const [plans, setPlans] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState(emptyForm);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const photoUrl = useMemo(() => {
    if (!member?.profilePhotoPath) {
      return "";
    }
    if (member.profilePhotoPath.startsWith("http")) {
      return member.profilePhotoPath;
    }
    return member.profilePhotoPath;
  }, [member?.profilePhotoPath]);

  const fromPath = typeof location.state?.from === "string" ? location.state.from : "";

  const fetchData = async () => {
    if (!memberId) {
      return;
    }

    setLoading(true);
    setError("");
    try {
      const [memberRes, plansRes] = await Promise.all([
        api.get(`/api/members/${memberId}`),
        api.get(`/api/membermemberships/member/${memberId}`),
      ]);
      const memberData = memberRes.data;
      setMember(memberData);
      setPlans(plansRes.data ?? []);
      setForm({
        name: memberData?.name ?? "",
        dateOfBirth: memberData?.dateOfBirth ?? "",
        gender: memberData?.gender ?? "Male",
        phone: memberData?.phone ?? "",
        email: memberData?.email ?? "",
        addressLine: memberData?.addressLine ?? "",
        city: memberData?.city ?? "",
        state: memberData?.state ?? "",
        pincode: memberData?.pincode ?? "",
        height: String(memberData?.height ?? 0),
        weight: String(memberData?.weight ?? 0),
        joiningDate: memberData?.joiningDate?.slice(0, 10) ?? new Date().toISOString().slice(0, 10),
        membershipStatus: memberData?.membershipStatus ?? "Active",
      });
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to load member profile."));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, [memberId]);

  const handleChange = (event) => {
    const { name, value } = event.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleSave = async (event) => {
    event.preventDefault();
    if (!memberId || !isAdmin) {
      return;
    }

    setSaving(true);
    setError("");
    setSuccess("");
    try {
      const payload = {
        name: form.name.trim(),
        dateOfBirth: form.dateOfBirth,
        gender: form.gender,
        phone: form.phone.trim(),
        email: form.email.trim(),
        addressLine: form.addressLine.trim(),
        city: form.city.trim(),
        state: form.state.trim(),
        pincode: form.pincode.trim(),
        height: Number(form.height || 0),
        weight: Number(form.weight || 0),
        joiningDate: toApiDateTime(form.joiningDate),
        membershipStatus: form.membershipStatus,
      };
      await api.put(`/api/members/${memberId}`, payload);
      setSuccess("Member profile updated.");
      setEditing(false);
      await fetchData();
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to update member profile."));
    } finally {
      setSaving(false);
    }
  };

  const handlePhotoUpload = async (event) => {
    const file = event.target.files?.[0];
    if (!file || !memberId) {
      return;
    }

    setUploading(true);
    setError("");
    setSuccess("");
    try {
      const formData = new FormData();
      formData.append("file", file);
      await api.post(`/api/members/${memberId}/upload-photo`, formData);
      setSuccess("Profile photo updated.");
      await fetchData();
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to upload profile photo."));
    } finally {
      setUploading(false);
      event.target.value = "";
    }
  };

  if (loading) {
    return <p className="text-slate-600">Loading member profile...</p>;
  }

  if (!member) {
    return <p className="text-slate-600">Member not found.</p>;
  }

  return (
    <section>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-2xl font-bold text-slate-900">Member Profile</h2>
          <p className="mt-1 text-sm text-slate-600">Detailed profile, photo and linked membership plans.</p>
        </div>
        <button
          type="button"
          onClick={() => {
            if (fromPath) {
              navigate(fromPath);
            } else {
              navigate("/members");
            }
          }}
          className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100"
        >
          Back
        </button>
      </div>

      {error && <p className="mt-4 rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">{error}</p>}
      {success && (
        <p className="mt-4 rounded-lg bg-emerald-50 px-3 py-2 text-sm font-medium text-emerald-700">{success}</p>
      )}

      <div className="mt-5 grid grid-cols-1 gap-4 lg:grid-cols-3">
        <div className="rounded-xl border border-slate-200 bg-white p-4">
          <div className="flex flex-col items-center gap-3">
            <div className="h-36 w-36 overflow-hidden rounded-full border border-slate-300 bg-slate-100">
              {photoUrl ? (
                <img src={photoUrl} alt={member.name} className="h-full w-full object-cover" />
              ) : (
                <div className="flex h-full w-full items-center justify-center text-sm font-semibold text-slate-500">
                  No Photo
                </div>
              )}
            </div>
            {canUploadPhoto && (
              <label className="w-full">
                <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">
                  Upload Photo
                </span>
                <input
                  type="file"
                  accept="image/*"
                  onChange={handlePhotoUpload}
                  disabled={uploading}
                  className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-xs outline-none file:mr-2 file:rounded-md file:border-0 file:bg-orange-100 file:px-2 file:py-1 file:font-semibold file:text-orange-700 disabled:opacity-60"
                />
              </label>
            )}
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4 lg:col-span-2">
          <div className="mb-3 flex items-center justify-between">
            <h3 className="text-lg font-bold text-slate-900">Profile Details</h3>
            {isAdmin && !editing && (
              <button
                type="button"
                onClick={() => setEditing(true)}
                className="rounded-md border border-slate-300 px-3 py-1 text-xs font-semibold text-slate-700 hover:bg-slate-100"
              >
                Edit
              </button>
            )}
          </div>

          {!editing && (
            <div className="grid grid-cols-1 gap-3 text-sm md:grid-cols-2">
              <p><span className="font-semibold text-slate-700">Name:</span> {member.name}</p>
              <p><span className="font-semibold text-slate-700">Email:</span> {member.email}</p>
              <p><span className="font-semibold text-slate-700">Phone:</span> {member.phone}</p>
              <p><span className="font-semibold text-slate-700">Gender:</span> {member.gender}</p>
              <p><span className="font-semibold text-slate-700">DOB:</span> {formatDateOnly(member.dateOfBirth)}</p>
              <p><span className="font-semibold text-slate-700">Joining:</span> {formatDateOnly(member.joiningDate)}</p>
              <p><span className="font-semibold text-slate-700">Height:</span> {member.height}</p>
              <p><span className="font-semibold text-slate-700">Weight:</span> {member.weight}</p>
              <p><span className="font-semibold text-slate-700">Status:</span> {member.membershipStatus}</p>
              <p className="md:col-span-2">
                <span className="font-semibold text-slate-700">Address:</span>{" "}
                {member.addressLine}, {member.city}, {member.state} - {member.pincode}
              </p>
            </div>
          )}

          {editing && (
            <form onSubmit={handleSave} className="grid grid-cols-1 gap-3 md:grid-cols-2">
              {[
                ["name", "Name", "text"],
                ["dateOfBirth", "Date of Birth", "date"],
                ["phone", "Phone", "text"],
                ["email", "Email", "email"],
                ["addressLine", "Address", "text"],
                ["city", "City", "text"],
                ["state", "State", "text"],
                ["pincode", "Pincode", "text"],
                ["height", "Height", "number"],
                ["weight", "Weight", "number"],
                ["joiningDate", "Joining Date", "date"],
              ].map(([name, label, type]) => (
                <label key={name} className={name === "addressLine" ? "md:col-span-2" : ""}>
                  <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">{label}</span>
                  <input
                    name={name}
                    type={type}
                    value={form[name]}
                    onChange={handleChange}
                    required={!["height", "weight"].includes(name)}
                    className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
                  />
                </label>
              ))}

              <label>
                <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Gender</span>
                <select
                  name="gender"
                  value={form.gender}
                  onChange={handleChange}
                  className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
                >
                  {genderOptions.map((option) => (
                    <option key={option} value={option}>
                      {option}
                    </option>
                  ))}
                </select>
              </label>

              <label>
                <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Membership Status</span>
                <select
                  name="membershipStatus"
                  value={form.membershipStatus}
                  onChange={handleChange}
                  className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
                >
                  {statusOptions.map((option) => (
                    <option key={option} value={option}>
                      {option}
                    </option>
                  ))}
                </select>
              </label>

              <div className="md:col-span-2 flex gap-2">
                <button
                  type="submit"
                  disabled={saving}
                  className="rounded-lg bg-orange-500 px-4 py-2 text-sm font-semibold text-white hover:bg-orange-600 disabled:opacity-60"
                >
                  {saving ? "Saving..." : "Save"}
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setEditing(false);
                    setError("");
                  }}
                  className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100"
                >
                  Cancel
                </button>
              </div>
            </form>
          )}
        </div>
      </div>

      <div className="mt-6 rounded-xl border border-slate-200 bg-white">
        <div className="border-b border-slate-200 bg-slate-50 px-4 py-3">
          <h3 className="text-base font-bold text-slate-900">Linked Plan Details</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full text-left text-sm">
            <thead className="bg-white text-slate-700">
              <tr>
                <th className="px-4 py-3">Plan</th>
                <th className="px-4 py-3">Type</th>
                <th className="px-4 py-3">Price</th>
                <th className="px-4 py-3">Discount</th>
                <th className="px-4 py-3">Start</th>
                <th className="px-4 py-3">End</th>
                <th className="px-4 py-3">Active</th>
              </tr>
            </thead>
            <tbody>
              {plans.map((plan) => (
                <tr key={plan.id} className="border-t border-slate-100">
                  <td className="px-4 py-3">{plan.membershipPlan?.planName ?? "-"}</td>
                  <td className="px-4 py-3">{normalizePlanType(plan.membershipPlan?.membershipType)}</td>
                  <td className="px-4 py-3">INR {plan.membershipPlan?.price ?? 0}</td>
                  <td className="px-4 py-3">INR {plan.discount ?? 0}</td>
                  <td className="px-4 py-3">{formatDateOnly(plan.startDate)}</td>
                  <td className="px-4 py-3">{formatDateOnly(plan.endDate)}</td>
                  <td className="px-4 py-3">{plan.isActive ? "Yes" : "No"}</td>
                </tr>
              ))}
              {plans.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-4 py-6 text-center text-slate-500">
                    No linked plans found.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
}

export default MemberProfilePage;
