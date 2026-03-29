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

function ReportsPaymentCollectionsPage() {
  const { user } = useAuth();
  const isAdmin = useMemo(() => user?.role === "Admin", [user?.role]);
  const range = useMemo(() => currentMonthRange(), []);
  const [fromDate, setFromDate] = useState(toDateInputValue(range.start));
  const [toDate, setToDate] = useState(toDateInputValue(range.end));
  const [search, setSearch] = useState("");
  const [paymentMode, setPaymentMode] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [loading, setLoading] = useState(true);
  const [downloading, setDownloading] = useState("");
  const [error, setError] = useState("");
  const [report, setReport] = useState(null);

  const fetchReport = async (nextPage = page) => {
    setLoading(true);
    setError("");
    try {
      const response = await api.get("/api/reports/payment-collections", {
        params: {
          fromDate,
          toDate,
          page: nextPage,
          pageSize,
          search: search.trim() || undefined,
          paymentMode: paymentMode || undefined,
        },
      });
      setReport(response.data);
      setPage(response.data?.page ?? nextPage);
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to load payment collections report."));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!isAdmin) {
      setLoading(false);
      return;
    }
    fetchReport(1);
  }, [isAdmin]);

  const handleDownload = async (format) => {
    setDownloading(format);
    setError("");
    try {
      await downloadFile(
        `/api/reports/payment-collections/export/${format}`,
        {
          fromDate,
          toDate,
          search: search.trim() || undefined,
          paymentMode: paymentMode || undefined,
        },
        `payment-collections-report-${fromDate}-${toDate}.${format === "pdf" ? "pdf" : "xlsx"}`
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
        <h2 className="text-2xl font-bold text-slate-900">Reports - Payment Collections</h2>
        <p className="mt-3 rounded-lg bg-amber-50 px-3 py-2 text-sm font-medium text-amber-800">
          Only Admin can access reporting module.
        </p>
      </section>
    );
  }

  return (
    <section className="space-y-4">
      <div>
        <h2 className="text-2xl font-bold text-slate-900">Payment Collections Report</h2>
        <p className="mt-1 text-sm text-slate-600">Track collected payments with payment-mode filter and downloads.</p>
      </div>

      {error && <p className="rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">{error}</p>}

      <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="grid gap-3 md:grid-cols-5">
          <label>
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">From</span>
            <input type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-500" />
          </label>
          <label>
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">To</span>
            <input type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-500" />
          </label>
          <label>
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Search</span>
            <input type="text" value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Member, receipt, invoice..." className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-500" />
          </label>
          <label>
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Payment Mode</span>
            <select value={paymentMode} onChange={(e) => setPaymentMode(e.target.value)} className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-500">
              <option value="">All</option>
              <option value="Cash">Cash</option>
              <option value="UPI">UPI</option>
              <option value="Card">Card</option>
              <option value="BankTransfer">Bank Transfer</option>
            </select>
          </label>
          <label>
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Records</span>
            <select value={pageSize} onChange={(e) => setPageSize(Number(e.target.value))} className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-500">
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
            </select>
          </label>
        </div>
        <div className="mt-3 flex items-end gap-2">
          <button type="button" onClick={() => fetchReport(1)} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700">
            Apply Filter
          </button>
          <button type="button" onClick={() => handleDownload("pdf")} disabled={downloading !== ""} className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-60">
            {downloading === "pdf" ? "Downloading PDF..." : "Download PDF"}
          </button>
          <button type="button" onClick={() => handleDownload("excel")} disabled={downloading !== ""} className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-60">
            {downloading === "excel" ? "Downloading Excel..." : "Download Excel"}
          </button>
        </div>
      </div>

      {!loading && report && (
        <div className="grid gap-3 md:grid-cols-2">
          <article className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">Total Receipts</p>
            <p className="mt-2 text-3xl font-bold text-slate-900">{report.totalReceipts ?? 0}</p>
          </article>
          <article className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">Total Collection</p>
            <p className="mt-2 text-3xl font-bold text-emerald-700">{currency.format(report.totalCollectionAmount ?? 0)}</p>
          </article>
        </div>
      )}

      <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white">
        <table className="min-w-full text-left text-sm">
          <thead className="bg-slate-50 text-slate-700">
            <tr>
              <th className="px-4 py-3">Paid On</th>
              <th className="px-4 py-3">Receipt</th>
              <th className="px-4 py-3">Invoice</th>
              <th className="px-4 py-3">Member</th>
              <th className="px-4 py-3">Mode</th>
              <th className="px-4 py-3 text-right">Amount</th>
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr>
                <td colSpan={6} className="px-4 py-6 text-center text-slate-500">Loading report...</td>
              </tr>
            )}
            {!loading && !report?.items?.length && (
              <tr>
                <td colSpan={6} className="px-4 py-6 text-center text-slate-500">No collections found.</td>
              </tr>
            )}
            {!loading &&
              report?.items?.map((item) => (
                <tr key={item.paymentId} className="border-t border-slate-100">
                  <td className="px-4 py-3">{new Date(item.paidOn).toLocaleDateString("en-IN")}</td>
                  <td className="px-4 py-3">{item.receiptNumber}</td>
                  <td className="px-4 py-3">{item.invoiceNumber}</td>
                  <td className="px-4 py-3">
                    <p className="font-semibold text-slate-800">{item.memberName}</p>
                    <p className="text-xs text-slate-500">{item.memberEmail}</p>
                  </td>
                  <td className="px-4 py-3">{item.paymentMode}</td>
                  <td className="px-4 py-3 text-right text-emerald-700">{currency.format(item.amount)}</td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      {!loading && report && (
        <div className="flex items-center justify-end gap-2">
          <span className="text-xs font-medium text-slate-600">Page {report.totalPages === 0 ? 0 : report.page ?? 1} of {report.totalPages ?? 0}</span>
          <button type="button" disabled={(report.page ?? 1) <= 1} onClick={() => fetchReport((report.page ?? 1) - 1)} className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-100 disabled:opacity-50">
            Previous
          </button>
          <button type="button" disabled={(report.page ?? 1) >= (report.totalPages ?? 0)} onClick={() => fetchReport((report.page ?? 1) + 1)} className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-100 disabled:opacity-50">
            Next
          </button>
        </div>
      )}
    </section>
  );
}

export default ReportsPaymentCollectionsPage;
