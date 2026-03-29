import { useEffect, useMemo, useState } from "react";
import { api, getApiErrorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";

const currency = new Intl.NumberFormat("en-IN", {
  style: "currency",
  currency: "INR",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

function toDateInputValue(date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(
    date.getDate()
  ).padStart(2, "0")}`;
}

function currentMonthRange() {
  const now = new Date();
  const start = new Date(now.getFullYear(), now.getMonth(), 1);
  const end = new Date(now.getFullYear(), now.getMonth() + 1, 0);
  return { start, end };
}

async function downloadFile(url, params, filenameFallback) {
  const response = await api.get(url, { params, responseType: "blob" });
  const disposition = response.headers["content-disposition"] || "";
  const matched = disposition.match(/filename="?([^"]+)"?/i);
  const filename = matched?.[1] || filenameFallback;
  const blobUrl = URL.createObjectURL(response.data);
  const anchor = document.createElement("a");
  anchor.href = blobUrl;
  anchor.download = filename;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(blobUrl);
}

function ReportsFinancialPage() {
  const { user } = useAuth();
  const isAdmin = useMemo(() => user?.role === "Admin", [user?.role]);
  const range = useMemo(() => currentMonthRange(), []);
  const [fromDate, setFromDate] = useState(toDateInputValue(range.start));
  const [toDate, setToDate] = useState(toDateInputValue(range.end));
  const [search, setSearch] = useState("");
  const [membershipType, setMembershipType] = useState("");
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [loading, setLoading] = useState(true);
  const [downloading, setDownloading] = useState("");
  const [error, setError] = useState("");
  const [report, setReport] = useState(null);

  type FinancialFiltersOverride = {
    fromDate?: string;
    toDate?: string;
    search?: string;
    membershipType?: string;
    status?: string;
    pageSize?: number;
  };

  const fetchReport = async (
    nextPage = page,
    overrides: FinancialFiltersOverride = {}
  ) => {
    const resolvedFromDate = overrides.fromDate ?? fromDate;
    const resolvedToDate = overrides.toDate ?? toDate;
    const resolvedSearch = overrides.search ?? search;
    const resolvedMembershipType = overrides.membershipType ?? membershipType;
    const resolvedStatus = overrides.status ?? status;
    const resolvedPageSize = overrides.pageSize ?? pageSize;

    setLoading(true);
    setError("");
    try {
      const response = await api.get("/api/reports/financial", {
        params: {
          fromDate: resolvedFromDate,
          toDate: resolvedToDate,
          page: nextPage,
          pageSize: resolvedPageSize,
          search: resolvedSearch.trim() || undefined,
          membershipType: resolvedMembershipType || undefined,
          status: resolvedStatus || undefined,
        },
      });
      setReport(response.data);
      setPage(response.data?.page ?? nextPage);
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to load financial report."));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!isAdmin) {
      setLoading(false);
      return;
    }
    fetchReport();
  }, [isAdmin]);

  const handleFilter = async (event) => {
    event.preventDefault();
    await fetchReport(1);
  };

  const handleDownload = async (format) => {
    setDownloading(format);
    setError("");
    try {
      await downloadFile(
        `/api/reports/financial/export/${format}`,
        {
          fromDate,
          toDate,
          search: search.trim() || undefined,
          membershipType: membershipType || undefined,
          status: status || undefined,
        },
        `financial-report-${fromDate}-${toDate}.${format === "pdf" ? "pdf" : "xlsx"}`
      );
    } catch (err) {
      setError(getApiErrorMessage(err, `Unable to download ${format.toUpperCase()} report.`));
    } finally {
      setDownloading("");
    }
  };

  if (!isAdmin) {
    return (
      <section>
        <h2 className="text-2xl font-bold text-slate-900">Reports - Financial</h2>
        <p className="mt-3 rounded-lg bg-amber-50 px-3 py-2 text-sm font-medium text-amber-800">
          Only Admin can access reporting module.
        </p>
      </section>
    );
  }

  return (
    <section className="space-y-4">
      <div>
        <h2 className="text-2xl font-bold text-slate-900">Financial Report</h2>
        <p className="mt-1 text-sm text-slate-600">
          View billing, collection, due with search/filters and export as PDF/Excel.
        </p>
      </div>

      {error && <p className="rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">{error}</p>}

      <form onSubmit={handleFilter} className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="grid gap-3 md:grid-cols-6">
          <label className="md:col-span-2">
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Search</span>
            <input
              type="text"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Member name, email, plan..."
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-500"
            />
          </label>
          <label>
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">From</span>
            <input
              type="date"
              value={fromDate}
              onChange={(event) => setFromDate(event.target.value)}
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-500"
            />
          </label>
          <label>
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">To</span>
            <input
              type="date"
              value={toDate}
              onChange={(event) => setToDate(event.target.value)}
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-500"
            />
          </label>
          <label>
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Type</span>
            <select
              value={membershipType}
              onChange={(event) => setMembershipType(event.target.value)}
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-500"
            >
              <option value="">All</option>
              <option value="Single">Single</option>
              <option value="Couple">Couple</option>
            </select>
          </label>
          <label>
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Status</span>
            <select
              value={status}
              onChange={(event) => setStatus(event.target.value)}
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-500"
            >
              <option value="">All</option>
              <option value="Active">Active</option>
              <option value="Completed">Completed</option>
            </select>
          </label>
          <label>
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Records</span>
            <select
              value={pageSize}
              onChange={(event) => setPageSize(Number(event.target.value))}
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-500"
            >
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
            </select>
          </label>
        </div>
        <div className="mt-3 flex items-end gap-2">
          <button type="submit" className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700">
            Apply Filter
          </button>
          <button
            type="button"
            onClick={() => {
              setSearch("");
              setMembershipType("");
              setStatus("");
              setPageSize(10);
              fetchReport(1, { search: "", membershipType: "", status: "", pageSize: 10 });
            }}
            className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
          >
            Reset
          </button>
          <div className="flex items-end gap-2 md:col-span-2">
            <button
              type="button"
              onClick={() => handleDownload("pdf")}
              disabled={downloading !== ""}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-60"
            >
              {downloading === "pdf" ? "Downloading PDF..." : "Download PDF"}
            </button>
            <button
              type="button"
              onClick={() => handleDownload("excel")}
              disabled={downloading !== ""}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-60"
            >
              {downloading === "excel" ? "Downloading Excel..." : "Download Excel"}
            </button>
          </div>
        </div>
      </form>

      {!loading && report && (
        <div className="grid gap-3 md:grid-cols-4">
          <article className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">Membership Links</p>
            <p className="mt-2 text-3xl font-bold text-slate-900">{report.totalMemberships ?? 0}</p>
          </article>
          <article className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">Total Billing</p>
            <p className="mt-2 text-3xl font-bold text-slate-900">{currency.format(report.totalBilling ?? 0)}</p>
          </article>
          <article className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">Total Collected</p>
            <p className="mt-2 text-3xl font-bold text-emerald-700">{currency.format(report.totalCollected ?? 0)}</p>
          </article>
          <article className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">Total Due</p>
            <p className="mt-2 text-3xl font-bold text-amber-700">{currency.format(report.totalDue ?? 0)}</p>
          </article>
        </div>
      )}

      <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white">
          <table className="min-w-full text-left text-sm">
            <thead className="bg-slate-50 text-slate-700">
              <tr>
                <th className="px-4 py-3">Member</th>
                <th className="px-4 py-3">Plan</th>
                <th className="px-4 py-3">Type</th>
                <th className="px-4 py-3">Created</th>
                <th className="px-4 py-3 text-right">Net</th>
                <th className="px-4 py-3 text-right">Collected</th>
                <th className="px-4 py-3 text-right">Due</th>
                <th className="px-4 py-3">Status</th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr>
                  <td colSpan={8} className="px-4 py-6 text-center text-slate-500">
                    Loading report...
                  </td>
                </tr>
              )}
              {!loading && !report?.items?.length && (
                <tr>
                  <td colSpan={8} className="px-4 py-6 text-center text-slate-500">
                    No records found for selected period.
                  </td>
                </tr>
              )}
              {!loading &&
                report?.items?.map((item) => (
                  <tr key={item.membershipId} className="border-t border-slate-100">
                    <td className="px-4 py-3">
                      <p className="font-semibold text-slate-800">{item.memberName}</p>
                      <p className="text-xs text-slate-500">{item.memberEmail}</p>
                    </td>
                    <td className="px-4 py-3">{item.planName}</td>
                    <td className="px-4 py-3">{item.membershipType}</td>
                    <td className="px-4 py-3">{new Date(item.createdOn).toLocaleDateString("en-IN")}</td>
                    <td className="px-4 py-3 text-right">{currency.format(item.netAmount)}</td>
                    <td className="px-4 py-3 text-right text-emerald-700">{currency.format(item.collectedAmount)}</td>
                    <td className="px-4 py-3 text-right text-amber-700">{currency.format(item.dueAmount)}</td>
                    <td className="px-4 py-3">{item.status}</td>
                  </tr>
                ))}
            </tbody>
          </table>
      </div>

      {!loading && report && (
        <div className="flex items-center justify-end gap-2">
          <p className="mr-2 text-xs font-medium text-slate-600">
            Showing {report.items?.length ?? 0} of {report.totalCount ?? 0} records
          </p>
          <span className="text-xs font-medium text-slate-600">
            Page {report.totalPages === 0 ? 0 : report.page ?? 1} of {report.totalPages ?? 0}
          </span>
          <button
            type="button"
            disabled={(report.page ?? 1) <= 1}
            onClick={() => fetchReport((report.page ?? 1) - 1)}
            className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-100 disabled:opacity-50"
          >
            Previous
          </button>
          <button
            type="button"
            disabled={(report.page ?? 1) >= (report.totalPages ?? 0)}
            onClick={() => fetchReport((report.page ?? 1) + 1)}
            className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-100 disabled:opacity-50"
          >
            Next
          </button>
        </div>
      )}
    </section>
  );
}

export default ReportsFinancialPage;
