import { useEffect, useMemo, useState } from "react";
import { api, getApiErrorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { Link, useLocation } from "react-router-dom";

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
const emptyFilters = {
  search: "",
};

function toApiDateTime(dateText) {
  return `${dateText}T00:00:00`;
}

function MemberForm({ form, onChange, onSubmit, onCancel, loading, editMode }) {
  return (
    <form onSubmit={onSubmit} className="mt-4 rounded-xl border border-slate-200 bg-slate-50 p-4">
      <h3 className="text-lg font-bold text-slate-900">{editMode ? "Edit Member" : "Add New Member"}</h3>
      <div className="mt-4 grid grid-cols-1 gap-3 md:grid-cols-2">
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
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">
              {label}
            </span>
            <input
              name={name}
              type={type}
              value={form[name]}
              onChange={onChange}
              required={!["height", "weight"].includes(name)}
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
            />
          </label>
        ))}

        <label>
          <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">
            Gender
          </span>
          <select
            name="gender"
            value={form.gender}
            onChange={onChange}
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
          >
            {genderOptions.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </label>

        {editMode && (
          <label>
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">
              Membership Status
            </span>
            <select
              name="membershipStatus"
              value={form.membershipStatus}
              onChange={onChange}
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
            >
              {statusOptions.map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>
          </label>
        )}
      </div>

      <div className="mt-4 flex gap-2">
        <button
          type="submit"
          disabled={loading}
          className="rounded-lg bg-orange-500 px-4 py-2 text-sm font-semibold text-white hover:bg-orange-600 disabled:opacity-60"
        >
          {loading ? "Saving..." : editMode ? "Update Member" : "Create Member"}
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

function MembersPage() {
  const { user } = useAuth();
  const location = useLocation();
  const isAdmin = useMemo(() => user?.role === "Admin", [user?.role]);
  const [allMembers, setAllMembers] = useState([]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [editId, setEditId] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [filters, setFilters] = useState(emptyFilters);
  const [debouncedFilters, setDebouncedFilters] = useState(emptyFilters);

  const fetchMembers = async () => {
    setLoading(true);
    setError("");
    try {
      const response = await api.get("/api/members");
      const data = Array.isArray(response.data) ? response.data : [];
      setAllMembers(data);
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to load members."));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const handle = setTimeout(() => {
      setDebouncedFilters(filters);
    }, 300);
    return () => clearTimeout(handle);
  }, [filters]);

  useEffect(() => {
    fetchMembers();
  }, []);

  const filteredMembers = useMemo(() => {
    const term = debouncedFilters.search.trim().toLowerCase();
    if (!term) {
      return allMembers;
    }

    return allMembers.filter((member) =>
      `${member.name ?? ""} ${member.email ?? ""} ${member.phone ?? ""}`.toLowerCase().includes(term)
    );
  }, [allMembers, debouncedFilters.search]);

  const totalCount = filteredMembers.length;
  const totalPages = totalCount === 0 ? 0 : Math.ceil(totalCount / pageSize);

  useEffect(() => {
    if (totalPages === 0 && page !== 1) {
      setPage(1);
      return;
    }

    if (totalPages > 0 && page > totalPages) {
      setPage(totalPages);
    }
  }, [page, totalPages]);

  const members = useMemo(() => {
    if (totalCount === 0) {
      return [];
    }

    const start = (page - 1) * pageSize;
    return filteredMembers.slice(start, start + pageSize);
  }, [filteredMembers, page, pageSize, totalCount]);

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
    setSuccess("");
    setError("");
    setEditId(null);
    setForm(emptyForm);
    setShowForm(true);
  };

  const handleEditClick = (member) => {
    setSuccess("");
    setError("");
    setEditId(member.id);
    setForm({
      name: member.name ?? "",
      dateOfBirth: member.dateOfBirth ?? "",
      gender: member.gender ?? "Male",
      phone: member.phone ?? "",
      email: member.email ?? "",
      addressLine: member.addressLine ?? "",
      city: member.city ?? "",
      state: member.state ?? "",
      pincode: member.pincode ?? "",
      height: String(member.height ?? 0),
      weight: String(member.weight ?? 0),
      joiningDate: member.joiningDate?.slice(0, 10) ?? new Date().toISOString().slice(0, 10),
      membershipStatus: member.membershipStatus ?? "Active",
    });
    setShowForm(true);
  };

  const buildPayload = () => ({
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
  });

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError("");
    setSuccess("");

    try {
      if (editId) {
        await api.put(`/api/members/${editId}`, buildPayload());
        setSuccess("Member updated successfully.");
      } else {
        const payload = buildPayload();
        delete payload.membershipStatus;
        await api.post("/api/members", payload);
        setSuccess("Member created successfully.");
      }

      await fetchMembers();
      resetForm();
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to save member."));
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (memberId) => {
    const confirmed = window.confirm("Delete this member?");
    if (!confirmed) {
      return;
    }

    setError("");
    setSuccess("");
    try {
      await api.delete(`/api/members/${memberId}`);
      await fetchMembers();
      setSuccess("Member deleted.");
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to delete member."));
    }
  };

  const handleFilterChange = (event) => {
    const { name, value } = event.target;
    setFilters((prev) => ({ ...prev, [name]: value }));
    setPage(1);
  };

  const resetFilters = () => {
    setFilters(emptyFilters);
    setPage(1);
  };

  return (
    <section>
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h2 className="text-2xl font-bold text-slate-900">Members</h2>
          <p className="mt-1 text-sm text-slate-600">
            {isAdmin
              ? "Admin can create, update and delete members."
              : "Read-only view for your role based on API authorization."}
          </p>
        </div>
        {isAdmin && (
          <button
            type="button"
            onClick={handleCreateClick}
            className="rounded-lg bg-orange-500 px-4 py-2 text-sm font-semibold text-white hover:bg-orange-600"
          >
            Add Member
          </button>
        )}
      </div>

      {error && <p className="mt-4 rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">{error}</p>}
      {success && (
        <p className="mt-4 rounded-lg bg-emerald-50 px-3 py-2 text-sm font-medium text-emerald-700">{success}</p>
      )}

      {isAdmin && showForm && (
        <MemberForm
          form={form}
          onChange={handleChange}
          onSubmit={handleSubmit}
          onCancel={resetForm}
          loading={saving}
          editMode={Boolean(editId)}
        />
      )}

      {loading && <p className="mt-4 text-slate-600">Loading members...</p>}

      {!loading && (
        <div className="mt-5 space-y-3">
          <div className="rounded-xl border border-slate-200 bg-white p-3">
            <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
              <label>
                <span className="mb-1 block text-[11px] font-semibold uppercase tracking-wide text-slate-600">
                  Search
                </span>
                <input
                  name="search"
                  value={filters.search}
                  onChange={handleFilterChange}
                  placeholder="Search by name, email or phone"
                  className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
                />
              </label>
            </div>
            <div className="mt-3 flex justify-end">
              <button
                type="button"
                onClick={resetFilters}
                className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-100"
              >
                Reset Filters
              </button>
            </div>
          </div>

          <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-slate-600">
            <p>
              Showing {members.length} of {totalCount} members
            </p>
            <label className="flex items-center gap-2">
              <span>Rows per page</span>
              <select
                value={pageSize}
                onChange={(event) => {
                  setPage(1);
                  setPageSize(Number(event.target.value));
                }}
                className="rounded-md border border-slate-300 bg-white px-2 py-1 text-xs outline-none focus:border-orange-500"
              >
                {[10, 25, 50, 100].map((size) => (
                  <option key={size} value={size}>
                    {size}
                  </option>
                ))}
              </select>
            </label>
          </div>

          <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white">
          <table className="min-w-full text-left text-sm">
            <thead className="bg-slate-50 text-slate-700">
              <tr>
                <th className="px-4 py-3">Name</th>
                <th className="px-4 py-3">Email</th>
                <th className="px-4 py-3">Phone</th>
                <th className="px-4 py-3">Gender</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Joining</th>
                {isAdmin && <th className="px-4 py-3">Actions</th>}
              </tr>
            </thead>
            <tbody>
              {members.map((member) => (
                <tr key={member.id} className="border-t border-slate-100">
                  <td className="px-4 py-3">
                    <Link
                      to={`/members/${member.id}`}
                      state={{ from: `${location.pathname}${location.search}` }}
                      className="font-semibold text-sky-700 underline decoration-sky-300 underline-offset-2 hover:text-sky-800"
                    >
                      {member.name}
                    </Link>
                  </td>
                  <td className="px-4 py-3">{member.email}</td>
                  <td className="px-4 py-3">{member.phone}</td>
                  <td className="px-4 py-3">{member.gender}</td>
                  <td className="px-4 py-3">{member.membershipStatus}</td>
                  <td className="px-4 py-3">{member.joiningDate?.slice(0, 10)}</td>
                  {isAdmin && (
                    <td className="px-4 py-3">
                      <div className="flex gap-2">
                        <button
                          type="button"
                          onClick={() => handleEditClick(member)}
                          className="rounded-md border border-slate-300 px-3 py-1 text-xs font-semibold text-slate-700 hover:bg-slate-100"
                        >
                          Edit
                        </button>
                        <button
                          type="button"
                          onClick={() => handleDelete(member.id)}
                          className="rounded-md border border-red-300 px-3 py-1 text-xs font-semibold text-red-700 hover:bg-red-50"
                        >
                          Delete
                        </button>
                      </div>
                    </td>
                  )}
                </tr>
              ))}
              {members.length === 0 && (
                <tr>
                  <td colSpan={isAdmin ? 7 : 6} className="px-4 py-6 text-center text-slate-500">
                    No members found.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
          </div>

          <div className="flex items-center justify-end gap-2">
            <button
              type="button"
              onClick={() => setPage((prev) => Math.max(prev - 1, 1))}
              disabled={page <= 1}
              className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-100 disabled:opacity-50"
            >
              Previous
            </button>
            <span className="text-xs font-medium text-slate-600">
              Page {totalPages === 0 ? 0 : page} of {totalPages}
            </span>
            <button
              type="button"
              onClick={() => setPage((prev) => Math.min(prev + 1, Math.max(totalPages, 1)))}
              disabled={page >= totalPages}
              className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-100 disabled:opacity-50"
            >
              Next
            </button>
          </div>
        </div>
      )}
    </section>
  );
}

export default MembersPage;
