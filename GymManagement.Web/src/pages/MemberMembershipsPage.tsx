import { useEffect, useMemo, useState } from "react";
import { Link, useLocation, useSearchParams } from "react-router-dom";
import { api, getApiErrorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";

function normalizePlanType(rawType) {
  if (typeof rawType === "string") {
    return rawType.toLowerCase() === "couple" ? "Couple" : "Single";
  }
  if (typeof rawType === "number") {
    return rawType === 1 ? "Couple" : "Single";
  }
  return "Single";
}

function getDurationMonths(rawDuration) {
  if (typeof rawDuration === "number") {
    return rawDuration;
  }
  if (typeof rawDuration === "string") {
    switch (rawDuration) {
      case "OneMonth":
        return 1;
      case "ThreeMonths":
        return 3;
      case "SixMonths":
        return 6;
      case "OneYear":
        return 12;
      default:
        return 1;
    }
  }
  return 1;
}

function toDateOnlyValue(date) {
  return date.toISOString().slice(0, 10);
}

function calculateEndDate(startDateText, durationRaw) {
  if (!startDateText) {
    return "";
  }
  const start = new Date(startDateText);
  if (Number.isNaN(start.getTime())) {
    return startDateText;
  }
  const months = getDurationMonths(durationRaw);
  const end = new Date(start);
  end.setMonth(end.getMonth() + months);
  return toDateOnlyValue(end);
}

const emptyForm = {
  memberId: "",
  secondaryMemberId: "",
  membershipPlanId: "",
  discount: "0",
  description: "",
  startDate: toDateOnlyValue(new Date()),
  endDate: toDateOnlyValue(new Date()),
  isActive: true,
};

const paymentModeOptions = ["Cash", "Card", "Upi"];

const emptyPaymentForm = {
  amount: "",
  paidOn: new Date().toISOString().slice(0, 10),
  paymentMode: "Cash",
  transactionReference: "",
  notes: "",
};

const emptyFilters = {
  name: "",
  planId: "",
  type: "",
  collectionMode: "",
  endingInDays: "",
  dateFrom: "",
  dateTo: "",
};

function formatDateTime(dateText) {
  if (!dateText) {
    return "-";
  }
  const date = new Date(dateText);
  if (Number.isNaN(date.getTime())) {
    return dateText;
  }
  return date.toLocaleString("en-IN", { dateStyle: "medium", timeStyle: "short" });
}

function formatDateOnly(dateText) {
  if (!dateText) {
    return "-";
  }

  const parts = dateText.split("-");
  if (parts.length === 3) {
    const [year, month, day] = parts.map(Number);
    const date = new Date(year, month - 1, day);
    if (!Number.isNaN(date.getTime())) {
      return date.toLocaleDateString("en-GB", {
        day: "numeric",
        month: "short",
        year: "numeric",
      });
    }
  }

  const fallback = new Date(dateText);
  if (!Number.isNaN(fallback.getTime())) {
    return fallback.toLocaleDateString("en-GB", {
      day: "numeric",
      month: "short",
      year: "numeric",
    });
  }

  return dateText;
}

function toDateInputFromDateOnly(dateText) {
  if (!dateText) {
    return new Date().toISOString().slice(0, 10);
  }
  return dateText;
}

function combineSelectedDateWithCurrentTimeIso(dateText) {
  const selected = new Date(dateText);
  if (Number.isNaN(selected.getTime())) {
    return new Date().toISOString();
  }

  const now = new Date();
  selected.setHours(now.getHours(), now.getMinutes(), now.getSeconds(), now.getMilliseconds());
  return selected.toISOString();
}

function MembershipForm({
  form,
  members,
  plans,
  onChange,
  onSubmit,
  onCancel,
  loading,
  editMode,
}) {
  const availablePlans = editMode
    ? plans.filter((p) => p.isActive || p.id === form.membershipPlanId)
    : plans.filter((p) => p.isActive);
  const selectedPlan = plans.find((p) => p.id === form.membershipPlanId);
  const selectedPlanType = normalizePlanType(selectedPlan?.membershipType);
  const isCouplePlan = selectedPlanType === "Couple";
  const selectableSecondaryMembers = members.filter((m) => m.id !== form.memberId);
  const planAmount = Number(selectedPlan?.price ?? 0);
  const discountValue = Number(form.discount || 0);
  const netAmount = Math.max(planAmount - discountValue, 0);
  const minEndDate = calculateEndDate(form.startDate, selectedPlan?.duration);

  return (
    <form onSubmit={onSubmit} className="mt-4 rounded-xl border border-slate-200 bg-slate-50 p-4">
      <h3 className="text-lg font-bold text-slate-900">{editMode ? "Edit Member Membership" : "Create Member Membership"}</h3>
      <div className="mt-4 grid grid-cols-1 gap-3 md:grid-cols-2">
        <label>
          <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Member</span>
          <select
            name="memberId"
            value={form.memberId}
            onChange={onChange}
            required
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
          >
            <option value="">Select Member</option>
            {members.map((member) => (
              <option key={member.id} value={member.id}>
                {member.name} ({member.email})
              </option>
            ))}
          </select>
        </label>

        <label>
          <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Membership Plan</span>
          <select
            name="membershipPlanId"
            value={form.membershipPlanId}
            onChange={onChange}
            required
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
          >
            <option value="">Select Plan</option>
            {availablePlans.map((plan) => (
              <option key={plan.id} value={plan.id}>
                {plan.planName}
              </option>
            ))}
          </select>
          <p className="mt-1 text-xs font-medium text-slate-600">
            Plan Amount: INR {planAmount} | Net After Discount: INR {netAmount}
          </p>
        </label>

        <label>
          <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Plan Type</span>
          <input
            value={selectedPlanType}
            disabled
            className="w-full rounded-lg border border-slate-300 bg-slate-100 px-3 py-2 text-sm text-slate-700"
          />
        </label>

        {isCouplePlan && (
          <label>
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Second Member</span>
            <select
              name="secondaryMemberId"
              value={form.secondaryMemberId}
              onChange={onChange}
              required
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
            >
              <option value="">Select Second Member</option>
              {selectableSecondaryMembers.map((member) => (
                <option key={member.id} value={member.id}>
                  {member.name} ({member.email})
                </option>
              ))}
            </select>
          </label>
        )}

        <label>
          <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Discount (INR)</span>
          <input
            name="discount"
            type="number"
            min="0"
            max={planAmount}
            step="0.01"
            value={form.discount}
            onChange={onChange}
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
          />
        </label>

        <label>
          <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Start Date</span>
          <input
            name="startDate"
            type="date"
            value={form.startDate}
            onChange={onChange}
            required
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
          />
        </label>

        <label>
          <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">End Date</span>
          <input
            name="endDate"
            type="date"
            value={form.endDate}
            onChange={onChange}
            required
            min={minEndDate || form.startDate}
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
          />
          <p className="mt-1 text-[11px] text-slate-500">Minimum for selected plan: {minEndDate || "-"}</p>
        </label>

        <label className="md:col-span-2">
          <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Description</span>
          <textarea
            name="description"
            value={form.description}
            onChange={onChange}
            rows={3}
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
          />
        </label>

        {editMode && (
          <label className="inline-flex items-center gap-2 pt-6">
            <input
              name="isActive"
              type="checkbox"
              checked={form.isActive}
              onChange={onChange}
              className="h-4 w-4 rounded border-slate-300 text-orange-500 focus:ring-orange-500"
            />
            <span className="text-sm font-semibold text-slate-700">Active</span>
          </label>
        )}
      </div>

      <div className="mt-4 flex gap-2">
        <button
          type="submit"
          disabled={loading}
          className="rounded-lg bg-orange-500 px-4 py-2 text-sm font-semibold text-white hover:bg-orange-600 disabled:opacity-60"
        >
          {loading ? "Saving..." : editMode ? "Update Link" : "Create Link"}
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

function PaymentModal({
  record,
  form,
  onChange,
  onSubmit,
  onClose,
  saving,
  payments,
  summary,
  onSendInvoice,
  onOpenInvoice,
  onSendReminder,
  sendingInvoicePaymentId,
  sendingReminder,
  loading,
}) {
  if (!record) {
    return null;
  }

  const isFullyPaid = summary && summary.dueAmount <= 0;
  const maxPayableAmount = summary?.dueAmount ?? record.membershipPlan?.price ?? 0;

  return (
    <div className="fixed inset-0 z-40 flex items-center justify-center bg-slate-900/50 p-4">
      <div className="w-full max-w-xl rounded-2xl bg-white p-5 shadow-2xl">
        <div className="flex items-start justify-between gap-3">
          <div>
            <h3 className="text-lg font-bold text-slate-900">Record Payment</h3>
            <p className="text-sm text-slate-600">
              {record.member?.name} | {record.membershipPlan?.planName}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-md border border-slate-300 px-3 py-1 text-xs font-semibold text-slate-700 hover:bg-slate-100"
          >
            Close
          </button>
        </div>

        {loading && <p className="mt-4 text-sm text-slate-600">Loading payment details...</p>}

        {!loading && !isFullyPaid && (
          <form onSubmit={onSubmit} className="mt-4 grid grid-cols-1 gap-3 md:grid-cols-2">
            <label>
              <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Amount (INR)</span>
              <input
                name="amount"
                type="number"
                min="0.01"
                step="0.01"
                max={maxPayableAmount}
                value={form.amount}
                onChange={onChange}
                required
                className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
              />
              <p className="mt-1 text-[11px] text-slate-500">Remaining due: INR {summary?.dueAmount ?? "-"}</p>
            </label>

          <label>
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Paid On</span>
            <input
              name="paidOn"
              type="date"
              value={form.paidOn}
              onChange={onChange}
              required
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
              />
            </label>

            <label>
              <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Payment Mode</span>
              <select
                name="paymentMode"
                value={form.paymentMode}
                onChange={onChange}
                className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
              >
                {paymentModeOptions.map((mode) => (
                  <option key={mode} value={mode}>
                    {mode}
                  </option>
                ))}
              </select>
            </label>

            <label>
              <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">
                Transaction Reference
              </span>
              <input
                name="transactionReference"
                value={form.transactionReference}
                onChange={onChange}
                className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
              />
            </label>

            <label className="md:col-span-2">
              <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Notes</span>
              <textarea
                name="notes"
                value={form.notes}
                onChange={onChange}
                rows={3}
                className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
              />
            </label>

            <div className="md:col-span-2 flex gap-2">
              <button
                type="submit"
                disabled={saving}
                className="rounded-lg bg-orange-500 px-4 py-2 text-sm font-semibold text-white hover:bg-orange-600 disabled:opacity-60"
              >
                {saving ? "Capturing..." : "Capture Payment"}
              </button>
              <button
                type="button"
                onClick={onSendReminder}
                disabled={sendingReminder || !summary || summary.dueAmount <= 0}
                className="rounded-lg border border-indigo-300 bg-indigo-50 px-4 py-2 text-sm font-semibold text-indigo-700 hover:bg-indigo-100 disabled:opacity-60"
              >
                {sendingReminder ? "Sending..." : "Send Reminder"}
              </button>
              <button
                type="button"
                onClick={onClose}
                className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100"
              >
                Cancel
              </button>
            </div>
          </form>
        )}

        {!loading && isFullyPaid && (
          <div className="mt-4 rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm font-medium text-emerald-700">
            Payment is already fully collected for this membership link. Capture payment is hidden.
          </div>
        )}

        <div className="mt-5 rounded-xl border border-slate-200">
          <div className="border-b border-slate-200 bg-slate-50 px-4 py-2">
            <p className="text-sm font-semibold text-slate-700">Collected Payments</p>
            <p className="text-xs text-slate-500">
              Plan: INR {summary?.planAmount ?? 0} | Collected: INR {summary?.collectedAmount ?? 0} | Due: INR{" "}
              {summary?.dueAmount ?? 0}
            </p>
          </div>
          <div className="max-h-64 overflow-auto">
            <table className="min-w-full text-left text-xs">
              <thead className="bg-white text-slate-600">
                <tr>
                  <th className="px-3 py-2">Receipt</th>
                  <th className="px-3 py-2">Invoice</th>
                  <th className="px-3 py-2">Amount</th>
                  <th className="px-3 py-2">Mode</th>
                  <th className="px-3 py-2">Paid On</th>
                  <th className="px-3 py-2">Action</th>
                </tr>
              </thead>
              <tbody>
                {payments.map((payment) => (
                  <tr key={payment.id} className="border-t border-slate-100">
                    <td className="px-3 py-2">{payment.receiptNumber ?? "-"}</td>
                    <td className="px-3 py-2">
                      <button
                        type="button"
                        onClick={() => onOpenInvoice(payment.id)}
                        disabled={summary?.dueAmount > 0}
                        title={
                          summary?.dueAmount > 0
                            ? "Invoice can be opened only after all dues are cleared."
                            : "Open invoice"
                        }
                        className="font-semibold text-sky-700 underline decoration-sky-300 underline-offset-2 hover:text-sky-800 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        {payment.invoiceNumber}
                      </button>
                    </td>
                    <td className="px-3 py-2">INR {payment.amount}</td>
                    <td className="px-3 py-2">{payment.paymentMode}</td>
                    <td className="px-3 py-2">{formatDateTime(payment.paidOn)}</td>
                    <td className="px-3 py-2">
                      <button
                        type="button"
                        onClick={() => onSendInvoice(payment.id)}
                        disabled={sendingInvoicePaymentId === payment.id || summary?.dueAmount > 0}
                        className="rounded-md border border-emerald-300 px-2 py-1 text-[11px] font-semibold text-emerald-700 hover:bg-emerald-50 disabled:opacity-60"
                      >
                        {sendingInvoicePaymentId === payment.id ? "Sending..." : "Send Invoice"}
                      </button>
                    </td>
                  </tr>
                ))}
                {payments.length === 0 && (
                  <tr>
                    <td colSpan={6} className="px-3 py-4 text-center text-slate-500">
                      No payments captured yet.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
}

function MemberMembershipsPage() {
  const { user } = useAuth();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const isAdmin = useMemo(() => user?.role === "Admin", [user?.role]);
  const [records, setRecords] = useState([]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [members, setMembers] = useState([]);
  const [plans, setPlans] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [editId, setEditId] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [paymentRecord, setPaymentRecord] = useState(null);
  const [paymentSaving, setPaymentSaving] = useState(false);
  const [paymentForm, setPaymentForm] = useState(emptyPaymentForm);
  const [paymentHistory, setPaymentHistory] = useState([]);
  const [paymentSummary, setPaymentSummary] = useState(null);
  const [sendingInvoicePaymentId, setSendingInvoicePaymentId] = useState(null);
  const [sendingReminder, setSendingReminder] = useState(false);
  const [paymentDataLoading, setPaymentDataLoading] = useState(false);
  const [filters, setFilters] = useState(emptyFilters);
  const [debouncedFilters, setDebouncedFilters] = useState(emptyFilters);

  const fetchReferenceData = async () => {
    try {
      const [membersRes, plansRes] = await Promise.all([
        api.get("/api/members"),
        api.get("/api/membershipplans"),
      ]);
      setMembers(membersRes.data ?? []);
      setPlans(plansRes.data ?? []);
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to load members/plans."));
    }
  };

  const fetchRecords = async (targetPage = page, targetPageSize = pageSize, activeFilters = debouncedFilters) => {
    setLoading(true);
    setError("");
    try {
      const response = await api.get("/api/membermemberships/paged", {
        params: {
          page: targetPage,
          pageSize: targetPageSize,
          name: activeFilters.name || undefined,
          planId: activeFilters.planId || undefined,
          type: activeFilters.type || undefined,
          pendingCollectionOnly: activeFilters.collectionMode === "pending" ? true : undefined,
          collectedThisMonthOnly: activeFilters.collectionMode === "currentMonthRevenue" ? true : undefined,
          endingInDays: activeFilters.endingInDays ? Number(activeFilters.endingInDays) : undefined,
          createdFrom: activeFilters.dateFrom || undefined,
          createdTo: activeFilters.dateTo || undefined,
        },
      });
      setRecords(response.data?.items ?? []);
      setPage(response.data?.page ?? targetPage);
      setPageSize(response.data?.pageSize ?? targetPageSize);
      setTotalCount(response.data?.totalCount ?? 0);
      setTotalPages(response.data?.totalPages ?? 0);
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to load member memberships."));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchReferenceData();
  }, []);

  useEffect(() => {
    const handle = setTimeout(() => {
      setDebouncedFilters(filters);
    }, 350);

    return () => clearTimeout(handle);
  }, [filters]);

  useEffect(() => {
    fetchRecords(page, pageSize, debouncedFilters);
  }, [page, pageSize, debouncedFilters]);

  useEffect(() => {
    const pendingOnly = searchParams.get("pendingCollectionOnly") === "true";
    const collectedThisMonthOnly = searchParams.get("collectedThisMonthOnly") === "true";
    const typeFromQuery = searchParams.get("type");
    const createdFromFromQuery = searchParams.get("createdFrom");
    const createdToFromQuery = searchParams.get("createdTo");
    const endingInDaysFromQuery = searchParams.get("endingInDays");

    if (
      pendingOnly ||
      collectedThisMonthOnly ||
      typeFromQuery ||
      createdFromFromQuery ||
      createdToFromQuery ||
      endingInDaysFromQuery
    ) {
      const nextMode = pendingOnly ? "pending" : "currentMonthRevenue";
      setFilters((prev) => ({
        ...prev,
        collectionMode: pendingOnly || collectedThisMonthOnly ? nextMode : prev.collectionMode,
        type: typeFromQuery === "Single" || typeFromQuery === "Couple" ? typeFromQuery : prev.type,
        dateFrom: createdFromFromQuery ?? prev.dateFrom,
        dateTo: createdToFromQuery ?? prev.dateTo,
        endingInDays: endingInDaysFromQuery ?? prev.endingInDays,
      }));
      setPage(1);
    }
  }, [searchParams]);

  const resetForm = () => {
    setForm(emptyForm);
    setEditId(null);
    setShowForm(false);
  };

  const handleChange = (event) => {
    const { name, type, checked, value } = event.target;
    setForm((prev) => {
      if (name === "membershipPlanId") {
        const selectedPlan = plans.find((p) => p.id === value);
        const isCouplePlan = normalizePlanType(selectedPlan?.membershipType) === "Couple";
        const selectedStartDate = prev.startDate || toDateOnlyValue(new Date());
        return {
          ...prev,
          membershipPlanId: value,
          startDate: selectedStartDate,
          endDate: calculateEndDate(selectedStartDate, selectedPlan?.duration),
          discount: "0",
          secondaryMemberId: isCouplePlan ? prev.secondaryMemberId : "",
        };
      }

      if (name === "memberId" && prev.secondaryMemberId === value) {
        return {
          ...prev,
          memberId: value,
          secondaryMemberId: "",
        };
      }

      if (name === "startDate") {
        const selectedPlan = plans.find((p) => p.id === prev.membershipPlanId);
        return {
          ...prev,
          startDate: value,
          endDate: calculateEndDate(value, selectedPlan?.duration),
        };
      }

      return {
        ...prev,
        [name]: type === "checkbox" ? checked : value,
      };
    });
  };

  const handleCreateClick = () => {
    setError("");
    setSuccess("");
    setEditId(null);
    setForm(emptyForm);
    setShowForm(true);
  };

  const handleEditClick = (record) => {
    setError("");
    setSuccess("");
    setEditId(record.id);
    setForm({
      memberId: record.memberId ?? "",
      secondaryMemberId: record.secondaryMemberId ?? "",
      membershipPlanId: record.membershipPlanId ?? "",
      discount: String(record.discount ?? 0),
      description: record.description ?? "",
      startDate: record.startDate ?? new Date().toISOString().slice(0, 10),
      endDate: record.endDate ?? new Date().toISOString().slice(0, 10),
      isActive: record.isActive ?? true,
    });
    setShowForm(true);
  };

  const buildPayload = () => ({
    memberId: form.memberId,
    membershipPlanId: form.membershipPlanId,
    discount: Number(form.discount || 0),
    description: form.description.trim(),
    secondaryMemberId:
      normalizePlanType(plans.find((p) => p.id === form.membershipPlanId)?.membershipType) === "Couple"
        ? form.secondaryMemberId
        : null,
    startDate: form.startDate,
    endDate: form.endDate,
    isActive: form.isActive,
  });

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError("");
    setSuccess("");

    try {
      if (editId) {
        await api.put(`/api/membermemberships/${editId}`, buildPayload());
        setSuccess("Member membership link updated.");
      } else {
        const payload = buildPayload();
        delete payload.isActive;
        await api.post("/api/membermemberships", payload);
        setSuccess("Member membership link created.");
      }

      await fetchRecords(page, pageSize, debouncedFilters);
      resetForm();
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to save member membership link."));
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (recordId) => {
    const confirmed = window.confirm("Delete this member membership link?");
    if (!confirmed) {
      return;
    }

    setError("");
    setSuccess("");
    try {
      await api.delete(`/api/membermemberships/${recordId}`);
      const nextPage = records.length === 1 && page > 1 ? page - 1 : page;
      await fetchRecords(nextPage, pageSize, debouncedFilters);
      setSuccess("Member membership link deleted.");
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to delete member membership link."));
    }
  };

  const handlePaymentClick = (record) => {
    setError("");
    setSuccess("");
    setPaymentRecord(record);
    setPaymentForm({
      ...emptyPaymentForm,
      amount: String(Math.max(Number(record.membershipPlan?.price ?? 0) - Number(record.discount ?? 0), 0)),
      paidOn: toDateInputFromDateOnly(record.startDate),
      notes: `Payment for ${record.membershipPlan?.planName ?? "membership"}`,
    });
    void fetchPaymentData(record.id);
  };

  const closePaymentModal = () => {
    setPaymentRecord(null);
    setPaymentForm(emptyPaymentForm);
    setPaymentHistory([]);
    setPaymentSummary(null);
    setSendingInvoicePaymentId(null);
    setSendingReminder(false);
  };

  const handlePaymentFormChange = (event) => {
    const { name, value } = event.target;
    setPaymentForm((prev) => ({ ...prev, [name]: value }));
  };

  const fetchPaymentData = async (memberMembershipId) => {
    setPaymentDataLoading(true);
    try {
      const [historyRes, summaryRes] = await Promise.all([
        api.get(`/api/payments/membermembership/${memberMembershipId}`),
        api.get(`/api/payments/membermembership/${memberMembershipId}/summary`),
      ]);
      setPaymentHistory(historyRes.data ?? []);
      const summary = summaryRes.data ?? null;
      setPaymentSummary(summary);
      if (summary) {
        setPaymentForm((prev) => ({
          ...prev,
          amount: summary.dueAmount > 0 ? String(summary.dueAmount) : "",
        }));
      }
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to load payment history."));
    } finally {
      setPaymentDataLoading(false);
    }
  };

  const handlePaymentSubmit = async (event) => {
    event.preventDefault();
    if (!paymentRecord) {
      return;
    }

    setPaymentSaving(true);
    setError("");
    setSuccess("");
    try {
      const enteredAmount = Number(paymentForm.amount);
      if (paymentSummary && enteredAmount > paymentSummary.dueAmount) {
        setError(`Amount cannot be greater than remaining due (INR ${paymentSummary.dueAmount}).`);
        setPaymentSaving(false);
        return;
      }

      const payload = {
        memberId: paymentRecord.memberId,
        memberMembershipId: paymentRecord.id,
        amount: enteredAmount,
        paidOn: combineSelectedDateWithCurrentTimeIso(paymentForm.paidOn),
        paymentMode: paymentForm.paymentMode,
        transactionReference: paymentForm.transactionReference.trim() || null,
        notes: paymentForm.notes.trim() || null,
      };

      const response = await api.post("/api/payments", payload);
      const invoiceEmailSent = response.data?.invoiceEmailSent;
      const invoiceEmailError = response.data?.invoiceEmailError;
      const invoiceReady = response.data?.invoiceReady;
      const receiptNumber = response.data?.receiptNumber;
      const masterInvoiceNumber = response.data?.invoiceNumber;
      const receiptEmailSent = response.data?.receiptEmailSent;
      const receiptEmailError = response.data?.receiptEmailError;

      if (invoiceEmailSent && receiptEmailSent) {
        setSuccess(
          `Payment captured (Receipt: ${receiptNumber}) and receipt emailed. Invoice ${masterInvoiceNumber} is ready and emailed to member.`
        );
      } else if (invoiceEmailSent) {
        setSuccess(`Payment captured (Receipt: ${receiptNumber}). Invoice ${masterInvoiceNumber} is emailed to member.`);
      } else if (invoiceReady === false) {
        setSuccess(
          `Payment captured (Receipt: ${receiptNumber}). Linked to master invoice ${masterInvoiceNumber}. Invoice will be generated after all dues are cleared.${
            receiptEmailSent ? " Receipt emailed to member." : ""
          }`
        );
      } else if (receiptEmailError) {
        setSuccess(`Payment captured (Receipt: ${receiptNumber}). Receipt email not sent: ${receiptEmailError}`);
      } else if (invoiceEmailError) {
        setSuccess(`Payment recorded. Invoice email not sent: ${invoiceEmailError}`);
      } else {
        setSuccess("Payment recorded successfully.");
      }
      await fetchPaymentData(paymentRecord.id);
      setPaymentForm((prev) => ({ ...prev, transactionReference: "", notes: "" }));
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to record payment."));
    } finally {
      setPaymentSaving(false);
    }
  };

  const handleSendInvoice = async (paymentId) => {
    setSendingInvoicePaymentId(paymentId);
    setError("");
    setSuccess("");
    try {
      const response = await api.post(`/api/payments/${paymentId}/send-invoice-email`, {});
      if (response.data?.sent) {
        setSuccess("Invoice email sent.");
      } else {
        setSuccess("Invoice email request processed.");
      }
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to send invoice email."));
    } finally {
      setSendingInvoicePaymentId(null);
    }
  };

  const handleSendReminder = async () => {
    if (!paymentRecord) {
      return;
    }
    setSendingReminder(true);
    setError("");
    setSuccess("");
    try {
      const response = await api.post(`/api/payments/membermembership/${paymentRecord.id}/send-reminder`, {
        notes: paymentForm.notes.trim() || null,
      });
      if (response.data?.sent) {
        setSuccess(`Reminder sent. Due amount: INR ${response.data?.dueAmount ?? 0}`);
      } else {
        setSuccess("Reminder request processed.");
      }
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to send reminder."));
    } finally {
      setSendingReminder(false);
    }
  };

  const handleOpenInvoice = async (paymentId) => {
    setError("");
    try {
      const response = await api.get(`/api/payments/${paymentId}/invoice/pdf`, {
        responseType: "blob",
      });
      const file = new Blob([response.data], { type: "application/pdf" });
      const url = URL.createObjectURL(file);
      window.open(url, "_blank", "noopener,noreferrer");
      setTimeout(() => URL.revokeObjectURL(url), 30000);
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to open invoice."));
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
          <h2 className="text-2xl font-bold text-slate-900">Member Membership Links</h2>
          <p className="mt-1 text-sm text-slate-600">
            {isAdmin
              ? "Link members to plans as Single or Couple, with optional second member for Couple type."
              : "Read-only view for your role based on API authorization."}
          </p>
        </div>
        {isAdmin && (
          <button
            type="button"
            onClick={handleCreateClick}
            className="rounded-lg bg-orange-500 px-4 py-2 text-sm font-semibold text-white hover:bg-orange-600"
          >
            Add Link
          </button>
        )}
      </div>

      {error && <p className="mt-4 rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">{error}</p>}
      {success && (
        <p className="mt-4 rounded-lg bg-emerald-50 px-3 py-2 text-sm font-medium text-emerald-700">{success}</p>
      )}

      {isAdmin && showForm && (
        <MembershipForm
          form={form}
          members={members}
          plans={plans}
          onChange={handleChange}
          onSubmit={handleSubmit}
          onCancel={resetForm}
          loading={saving}
          editMode={Boolean(editId)}
        />
      )}

      {loading && <p className="mt-4 text-slate-600">Loading member memberships...</p>}

      {!loading && (
        <div className="mt-5 space-y-3">
          <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-slate-600">
            <p>
              Showing {records.length} of {totalCount} links
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
          <div className="border-b border-slate-200 bg-slate-50 p-3">
            <div className="grid grid-cols-1 gap-3 md:grid-cols-2 lg:grid-cols-8">
              <label className="lg:col-span-2">
                <span className="mb-1 block text-[11px] font-semibold uppercase tracking-wide text-slate-600">
                  Name / Plan
                </span>
                <input
                  name="name"
                  value={filters.name}
                  onChange={handleFilterChange}
                  placeholder="Search member or plan"
                  className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
                />
              </label>
              <label>
                <span className="mb-1 block text-[11px] font-semibold uppercase tracking-wide text-slate-600">Plan</span>
                <select
                  name="planId"
                  value={filters.planId}
                  onChange={handleFilterChange}
                  className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
                >
                  <option value="">All Plans</option>
                  {plans.map((plan) => (
                    <option key={plan.id} value={plan.id}>
                      {plan.planName}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                <span className="mb-1 block text-[11px] font-semibold uppercase tracking-wide text-slate-600">Type</span>
                <select
                  name="type"
                  value={filters.type}
                  onChange={handleFilterChange}
                  className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
                >
                  <option value="">All Types</option>
                  <option value="Single">Single</option>
                  <option value="Couple">Couple</option>
                </select>
              </label>
              <label>
                <span className="mb-1 block text-[11px] font-semibold uppercase tracking-wide text-slate-600">
                  Collection
                </span>
                <select
                  name="collectionMode"
                  value={filters.collectionMode}
                  onChange={handleFilterChange}
                  className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
                >
                  <option value="">All</option>
                  <option value="pending">Pending Only</option>
                  <option value="currentMonthRevenue">Collected This Month</option>
                </select>
              </label>
              <label>
                <span className="mb-1 block text-[11px] font-semibold uppercase tracking-wide text-slate-600">
                  Ending In (Days)
                </span>
                <input
                  name="endingInDays"
                  type="number"
                  min="0"
                  value={filters.endingInDays}
                  onChange={handleFilterChange}
                  placeholder="e.g. 7"
                  className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
                />
              </label>
              <label>
                <span className="mb-1 block text-[11px] font-semibold uppercase tracking-wide text-slate-600">Created From</span>
                <input
                  name="dateFrom"
                  type="date"
                  value={filters.dateFrom}
                  onChange={handleFilterChange}
                  className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
                />
              </label>
              <label>
                <span className="mb-1 block text-[11px] font-semibold uppercase tracking-wide text-slate-600">Created To</span>
                <input
                  name="dateTo"
                  type="date"
                  value={filters.dateTo}
                  onChange={handleFilterChange}
                  className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-orange-500"
                />
              </label>
            </div>
            <div className="mt-3 flex items-center justify-between">
              <p className="text-xs text-slate-600">Filters are applied on the server.</p>
              <button
                type="button"
                onClick={resetFilters}
                className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-100"
              >
                Reset Filters
              </button>
            </div>
          </div>

          <table className="min-w-full text-left text-sm">
            <thead className="bg-slate-50 text-slate-700">
              <tr>
                <th className="px-4 py-3">Primary Member</th>
                <th className="px-4 py-3">Second Member</th>
                <th className="px-4 py-3">Plan</th>
                <th className="px-4 py-3">Type</th>
                <th className="px-4 py-3">Created On</th>
                <th className="px-4 py-3">Start</th>
                <th className="px-4 py-3">End</th>
                <th className="px-4 py-3">Active</th>
                {isAdmin && <th className="px-4 py-3">Actions</th>}
              </tr>
            </thead>
            <tbody>
              {records.map((record) => (
                <tr key={record.id} className="border-t border-slate-100">
                  <td className="px-4 py-3">
                    {record.member?.id ? (
                      <Link
                        to={`/members/${record.member.id}`}
                        state={{ from: `${location.pathname}${location.search}` }}
                        className="font-semibold text-sky-700 underline decoration-sky-300 underline-offset-2 hover:text-sky-800"
                      >
                        {record.member?.name ?? "-"}
                      </Link>
                    ) : (
                      record.member?.name ?? "-"
                    )}
                  </td>
                  <td className="px-4 py-3">
                    {record.secondaryMember?.id ? (
                      <Link
                        to={`/members/${record.secondaryMember.id}`}
                        state={{ from: `${location.pathname}${location.search}` }}
                        className="font-semibold text-sky-700 underline decoration-sky-300 underline-offset-2 hover:text-sky-800"
                      >
                        {record.secondaryMember?.name ?? "-"}
                      </Link>
                    ) : (
                      record.secondaryMember?.name ?? "-"
                    )}
                  </td>
                  <td className="px-4 py-3">{record.membershipPlan?.planName ?? "-"}</td>
                  <td className="px-4 py-3">{normalizePlanType(record.membershipPlan?.membershipType)}</td>
                  <td className="px-4 py-3">{formatDateTime(record.createdOn)}</td>
                  <td className="px-4 py-3">{formatDateOnly(record.startDate)}</td>
                  <td className="px-4 py-3">{formatDateOnly(record.endDate)}</td>
                  <td className="px-4 py-3">{record.isActive ? "Yes" : "No"}</td>
                  {isAdmin && (
                    <td className="px-4 py-3">
                      <div className="flex gap-2">
                        <button
                          type="button"
                          onClick={() => handleEditClick(record)}
                          disabled={record.hasPayments}
                          title={
                            record.hasPayments
                              ? "Payment is collected on behalf of this plan so it can't be edited."
                              : "Edit"
                          }
                          className="rounded-md border border-slate-300 px-3 py-1 text-xs font-semibold text-slate-700 hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-50 disabled:hover:bg-transparent"
                        >
                          Edit
                        </button>
                        <button
                          type="button"
                          onClick={() => handleDelete(record.id)}
                          disabled={record.hasPayments}
                          title={
                            record.hasPayments
                              ? "Payment is collected on behalf of this plan so it can't be deleted."
                              : "Delete"
                          }
                          className="rounded-md border border-red-300 px-3 py-1 text-xs font-semibold text-red-700 hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-50 disabled:hover:bg-transparent"
                        >
                          Delete
                        </button>
                        <button
                          type="button"
                          onClick={() => handlePaymentClick(record)}
                          className="rounded-md border border-emerald-300 px-3 py-1 text-xs font-semibold text-emerald-700 hover:bg-emerald-50"
                        >
                          Payment
                        </button>
                      </div>
                    </td>
                  )}
                </tr>
              ))}
              {records.length === 0 && (
                <tr>
                  <td colSpan={isAdmin ? 9 : 8} className="px-4 py-6 text-center text-slate-500">
                    No member membership links found.
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

      <PaymentModal
        record={paymentRecord}
        form={paymentForm}
        onChange={handlePaymentFormChange}
        onSubmit={handlePaymentSubmit}
        onClose={closePaymentModal}
        saving={paymentSaving}
        payments={paymentHistory}
        summary={paymentSummary}
        onSendInvoice={handleSendInvoice}
        onOpenInvoice={handleOpenInvoice}
        onSendReminder={handleSendReminder}
        sendingInvoicePaymentId={sendingInvoicePaymentId}
        sendingReminder={sendingReminder}
        loading={paymentDataLoading}
      />
    </section>
  );
}

export default MemberMembershipsPage;
