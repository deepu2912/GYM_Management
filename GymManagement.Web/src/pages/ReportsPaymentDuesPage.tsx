import { useEffect, useMemo, useState } from "react";
import { api, getApiErrorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";

const currency = new Intl.NumberFormat("en-IN", {
  style: "currency",
  currency: "INR",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

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

function ReportsPaymentDuesPage() {
  const { user } = useAuth();
  const isAdmin = useMemo(() => user?.role === "Admin", [user?.role]);
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [loading, setLoading] = useState(true);
  const [downloading, setDownloading] = useState("");
  const [error, setError] = useState("");
  const [report, setReport] = useState(null);

  const fetchReport = async (nextFromDate = fromDate, nextToDate = toDate) => {
    setLoading(true);
    setError("");
    try {
      const response = await api.get("/api/reports/payment-dues", {
        params: {
          fromDate: nextFromDate || undefined,
          toDate: nextToDate || undefined,
        },
      });
      setReport(response.data);
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to load payment dues report."));
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

  const handleDownload = async (format) => {
    setDownloading(format);
    setError("");
    try {
      await downloadFile(
        `/api/reports/payment-dues/export/${format}`,
        {
          fromDate: fromDate || undefined,
          toDate: toDate || undefined,
        },
        `payment-dues-report.${format === "pdf" ? "pdf" : "xlsx"}`
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
        <h2 className="text-2xl font-bold text-slate-900">Reports - Payment Dues</h2>
        <p className="mt-3 rounded-lg bg-amber-50 px-3 py-2 text-sm font-medium text-amber-800">
          Only Admin can access reporting module.
        </p>
      </section>
    );
  }

  return (
    <section className="space-y-4">
      <div>
        <h2 className="text-2xl font-bold text-slate-900">Payment Dues Report</h2>
        <p className="mt-1 text-sm text-slate-600">Track member dues with an optional custom date range.</p>
      </div>

      {error && <p className="rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">{error}</p>}

      <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="grid gap-3 md:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_auto]">
          <label>
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">From Date</span>
            <input
              type="date"
              value={fromDate}
              onChange={(event) => setFromDate(event.target.value)}
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-500"
            />
          </label>
          <label>
            <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">To Date</span>
            <input
              type="date"
              value={toDate}
              onChange={(event) => setToDate(event.target.value)}
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-500"
            />
          </label>
          <div className="flex items-end gap-2">
            <button type="button" onClick={() => fetchReport(fromDate, toDate)} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700">
              Apply Filter
            </button>
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
      </div>

      {!loading && report && (
        <div className="grid gap-3 md:grid-cols-4">
          <article className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">Members with Due</p>
            <p className="mt-2 text-3xl font-bold text-slate-900">{report.totalMembersWithDue ?? 0}</p>
          </article>
          <article className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">Total Charges</p>
            <p className="mt-2 text-3xl font-bold text-slate-900">{currency.format(report.totalCharges ?? 0)}</p>
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
              <th className="px-4 py-3 text-right">Charges</th>
              <th className="px-4 py-3 text-right">Collected</th>
              <th className="px-4 py-3 text-right">Due</th>
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr>
                <td colSpan={4} className="px-4 py-6 text-center text-slate-500">
                  Loading report...
                </td>
              </tr>
            )}
            {!loading && !report?.items?.length && (
              <tr>
                <td colSpan={4} className="px-4 py-6 text-center text-slate-500">
                  No dues found.
                </td>
              </tr>
            )}
            {!loading &&
              report?.items?.map((item) => (
                <tr key={item.memberId} className="border-t border-slate-100">
                  <td className="px-4 py-3">
                    <p className="font-semibold text-slate-800">{item.memberName}</p>
                    <p className="text-xs text-slate-500">{item.memberEmail}</p>
                  </td>
                  <td className="px-4 py-3 text-right">{currency.format(item.totalCharges)}</td>
                  <td className="px-4 py-3 text-right text-emerald-700">{currency.format(item.totalCollected)}</td>
                  <td className="px-4 py-3 text-right text-amber-700">{currency.format(item.dueAmount)}</td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

export default ReportsPaymentDuesPage;
