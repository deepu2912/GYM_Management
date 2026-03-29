import { useEffect, useMemo, useState } from "react";
import { api, getApiErrorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";

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

function ReportsAttendancePage() {
  const { user } = useAuth();
  const isAdmin = useMemo(() => user?.role === "Admin", [user?.role]);
  const range = useMemo(() => currentMonthRange(), []);
  const [fromDate, setFromDate] = useState(toDateInputValue(range.start));
  const [toDate, setToDate] = useState(toDateInputValue(range.end));
  const [loading, setLoading] = useState(true);
  const [downloading, setDownloading] = useState("");
  const [error, setError] = useState("");
  const [report, setReport] = useState(null);

  const fetchReport = async () => {
    setLoading(true);
    setError("");
    try {
      const response = await api.get("/api/reports/attendance", { params: { fromDate, toDate } });
      setReport(response.data);
    } catch (err) {
      setError(getApiErrorMessage(err, "Unable to load attendance report."));
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
    await fetchReport();
  };

  const handleDownload = async (format) => {
    setDownloading(format);
    setError("");
    try {
      await downloadFile(
        `/api/reports/attendance/export/${format}`,
        { fromDate, toDate },
        `attendance-report-${fromDate}-${toDate}.${format === "pdf" ? "pdf" : "xlsx"}`
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
        <h2 className="text-2xl font-bold text-slate-900">Reports - Attendance</h2>
        <p className="mt-3 rounded-lg bg-amber-50 px-3 py-2 text-sm font-medium text-amber-800">
          Only Admin can access reporting module.
        </p>
      </section>
    );
  }

  return (
    <section className="space-y-4">
      <div>
        <h2 className="text-2xl font-bold text-slate-900">Attendance Report</h2>
        <p className="mt-1 text-sm text-slate-600">View member attendance summary and export as PDF/Excel.</p>
      </div>

      {error && <p className="rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">{error}</p>}

      <form onSubmit={handleFilter} className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="grid gap-3 md:grid-cols-4">
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
          <div className="flex items-end gap-2 md:col-span-2">
            <button type="submit" className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700">
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
      </form>

      {!loading && report && (
        <div className="grid gap-3 md:grid-cols-4">
          <article className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">Members</p>
            <p className="mt-2 text-3xl font-bold text-slate-900">{report.totalMembers ?? 0}</p>
          </article>
          <article className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">Marked Days</p>
            <p className="mt-2 text-3xl font-bold text-slate-900">{report.totalMarkedDays ?? 0}</p>
          </article>
          <article className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">Present Days</p>
            <p className="mt-2 text-3xl font-bold text-emerald-700">{report.totalPresentDays ?? 0}</p>
          </article>
          <article className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">Absent Days</p>
            <p className="mt-2 text-3xl font-bold text-rose-700">{report.totalAbsentDays ?? 0}</p>
          </article>
        </div>
      )}

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
        <div className="max-h-[520px] overflow-auto">
          <table className="min-w-full text-sm">
            <thead className="sticky top-0 bg-slate-100 text-xs uppercase tracking-wide text-slate-600">
              <tr>
                <th className="px-3 py-2 text-left">Member</th>
                <th className="px-3 py-2 text-right">Marked</th>
                <th className="px-3 py-2 text-right">Present</th>
                <th className="px-3 py-2 text-right">Absent</th>
                <th className="px-3 py-2 text-right">Attendance %</th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr>
                  <td colSpan={5} className="px-3 py-8 text-center text-slate-500">
                    Loading report...
                  </td>
                </tr>
              )}
              {!loading && !report?.items?.length && (
                <tr>
                  <td colSpan={5} className="px-3 py-8 text-center text-slate-500">
                    No records found for selected period.
                  </td>
                </tr>
              )}
              {!loading &&
                report?.items?.map((item) => (
                  <tr key={item.memberId} className="border-t border-slate-100">
                    <td className="px-3 py-2">
                      <p className="font-semibold text-slate-800">{item.memberName}</p>
                      <p className="text-xs text-slate-500">{item.memberEmail}</p>
                    </td>
                    <td className="px-3 py-2 text-right">{item.totalMarkedDays}</td>
                    <td className="px-3 py-2 text-right text-emerald-700">{item.presentDays}</td>
                    <td className="px-3 py-2 text-right text-rose-700">{item.absentDays}</td>
                    <td className="px-3 py-2 text-right">{Number(item.attendancePercentage ?? 0).toFixed(2)}%</td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
}

export default ReportsAttendancePage;
