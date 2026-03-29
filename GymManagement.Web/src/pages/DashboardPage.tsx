import { useEffect, useMemo, useState } from "react";
import {
  ArcElement,
  CategoryScale,
  Chart as ChartJS,
  Filler,
  Legend,
  LineElement,
  LinearScale,
  PointElement,
  Tooltip,
} from "chart.js";
import { Doughnut, Line } from "react-chartjs-2";
import { useNavigate } from "react-router-dom";
import { api } from "../api/client";

ChartJS.register(
  ArcElement,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Filler,
  Tooltip,
  Legend
);

const currency = new Intl.NumberFormat("en-IN", {
  style: "currency",
  currency: "INR",
  maximumFractionDigits: 0,
});

const FILTER_OPTIONS = {
  CURRENT_MONTH: "CURRENT_MONTH",
  PREVIOUS_MONTH: "PREVIOUS_MONTH",
  DATE_RANGE: "DATE_RANGE",
  YEAR: "YEAR",
};

function parseDateOnly(dateText) {
  const [year, month, day] = dateText.split("-").map(Number);
  return new Date(year, month - 1, day);
}

function toDateInputValue(date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(
    date.getDate()
  ).padStart(2, "0")}`;
}

function getMonthRange(referenceDate, offset = 0) {
  const base = new Date(referenceDate.getFullYear(), referenceDate.getMonth() + offset, 1);
  const start = new Date(base.getFullYear(), base.getMonth(), 1);
  const end = new Date(base.getFullYear(), base.getMonth() + 1, 0);
  return { start, end };
}

function formatRangeLabel(start, end) {
  if (!start || !end) {
    return "Invalid range";
  }
  const options = { day: "2-digit", month: "short", year: "numeric" };
  const from = start.toLocaleDateString("en-IN", options);
  const to = end.toLocaleDateString("en-IN", options);
  return `${from} - ${to}`;
}

function KPI({ title, value, subtitle, accentClass, valueClass, badge, onClick }) {
  const isClickable = typeof onClick === "function";
  return (
    <article
      onClick={onClick}
      className={`group relative overflow-hidden rounded-2xl border border-slate-200 bg-white/95 p-5 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md ${
        isClickable ? "cursor-pointer" : ""
      }`}
      role={isClickable ? "button" : undefined}
      tabIndex={isClickable ? 0 : undefined}
      onKeyDown={
        isClickable
          ? (event) => {
              if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                onClick();
              }
            }
          : undefined
      }
    >
      <div className={`absolute inset-x-0 top-0 h-1.5 ${accentClass}`} />
      <div className="flex items-start justify-between gap-2">
        <p className="text-[11px] font-semibold uppercase tracking-[0.16em] text-slate-500">{title}</p>
        {badge && (
          <span className="rounded-full border border-slate-200 bg-slate-50 px-2.5 py-1 text-[10px] font-semibold text-slate-600">
            {badge}
          </span>
        )}
      </div>
      <p className={`mt-3 text-4xl font-extrabold leading-none tracking-tight ${valueClass ?? "text-slate-900"}`}>{value}</p>
      <p className="mt-2 text-xs font-medium text-slate-500">{subtitle}</p>
    </article>
  );
}

function DashboardPage() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [filterLoading, setFilterLoading] = useState(true);
  const [error, setError] = useState("");
  const [coupleMembershipCount, setCoupleMembershipCount] = useState(0);
  const [coupleUsersCount, setCoupleUsersCount] = useState(0);
  const [singleUsersCount, setSingleUsersCount] = useState(0);
  const [totalMembershipUsers, setTotalMembershipUsers] = useState(0);
  const [monthlyRevenue, setMonthlyRevenue] = useState(0);
  const [revenueTrend, setRevenueTrend] = useState([]);
  const [totalUsers, setTotalUsers] = useState(0);
  const [activeUsers, setActiveUsers] = useState(0);
  const [membershipEndingSoonCount, setMembershipEndingSoonCount] = useState(0);
  const [completedPlansCount, setCompletedPlansCount] = useState(0);
  const [pendingCollectionAmount, setPendingCollectionAmount] = useState(0);
  const [filterType, setFilterType] = useState(FILTER_OPTIONS.CURRENT_MONTH);

  const now = useMemo(() => new Date(), []);
  const currentMonthRange = useMemo(() => getMonthRange(now, 0), [now]);
  const [rangeFrom, setRangeFrom] = useState(toDateInputValue(currentMonthRange.start));
  const [rangeTo, setRangeTo] = useState(toDateInputValue(currentMonthRange.end));
  const [selectedYear, setSelectedYear] = useState(now.getFullYear());

  const yearOptions = useMemo(() => [now.getFullYear(), now.getFullYear() - 1, now.getFullYear() - 2], [now]);

  useEffect(() => {
    async function loadRevenueSummary() {
      try {
        const paymentsSummaryRes = await api.get("/api/payments/dashboard-summary");
        setMonthlyRevenue(paymentsSummaryRes.data?.monthlyRevenue ?? 0);
        setPendingCollectionAmount(paymentsSummaryRes.data?.pendingCollectionAmount ?? 0);
        setRevenueTrend(paymentsSummaryRes.data?.revenueTrend ?? []);
        setTotalUsers(paymentsSummaryRes.data?.totalUsers ?? 0);
        setActiveUsers(paymentsSummaryRes.data?.activeUsers ?? 0);
        setMembershipEndingSoonCount(paymentsSummaryRes.data?.membershipEndingSoonCount ?? 0);
        setCompletedPlansCount(paymentsSummaryRes.data?.completedPlansCount ?? 0);
      } catch {
        setError("Unable to load dashboard revenue data for this account.");
      } finally {
        setLoading(false);
      }
    }

    loadRevenueSummary();
  }, []);

  const filterRange = useMemo(() => {
    if (filterType === FILTER_OPTIONS.CURRENT_MONTH) {
      return getMonthRange(now, 0);
    }
    if (filterType === FILTER_OPTIONS.PREVIOUS_MONTH) {
      return getMonthRange(now, -1);
    }
    if (filterType === FILTER_OPTIONS.YEAR) {
      return {
        start: new Date(selectedYear, 0, 1),
        end: new Date(selectedYear, 11, 31),
      };
    }

    return {
      start: rangeFrom ? parseDateOnly(rangeFrom) : null,
      end: rangeTo ? parseDateOnly(rangeTo) : null,
    };
  }, [filterType, now, selectedYear, rangeFrom, rangeTo]);

  useEffect(() => {
    async function loadMembershipSummary() {
      if (!filterRange.start || !filterRange.end || filterRange.start > filterRange.end) {
        setCoupleMembershipCount(0);
        setCoupleUsersCount(0);
        setSingleUsersCount(0);
        setTotalMembershipUsers(0);
        setFilterLoading(false);
        return;
      }

      const fromDate = toDateInputValue(filterRange.start);
      const toDate = toDateInputValue(filterRange.end);
      setFilterLoading(true);

      try {
        const response = await api.get("/api/membermemberships/dashboard-summary", {
          params: { fromDate, toDate },
        });

        setCoupleMembershipCount(response.data?.coupleMembershipCount ?? 0);
        setCoupleUsersCount(response.data?.coupleUsersCount ?? 0);
        setSingleUsersCount(response.data?.singleUsersCount ?? 0);
        setTotalMembershipUsers(response.data?.totalMembershipUsers ?? 0);
      } catch {
        setError("Unable to load membership summary for the selected period.");
      } finally {
        setFilterLoading(false);
      }
    }

    loadMembershipSummary();
  }, [filterRange.start, filterRange.end]);
  const selectedRangeLabel = useMemo(
    () => formatRangeLabel(filterRange.start, filterRange.end),
    [filterRange.start, filterRange.end]
  );
  const buildFilteredMembershipsUrl = (extraParams = {}) => {
    const params = new URLSearchParams();

    if (filterRange.start && filterRange.end && filterRange.start <= filterRange.end) {
      params.set("createdFrom", toDateInputValue(filterRange.start));
      params.set("createdTo", toDateInputValue(filterRange.end));
    }

    Object.entries(extraParams).forEach(([key, value]) => {
      if (value !== undefined && value !== null && String(value).trim() !== "") {
        params.set(key, String(value));
      }
    });

    const query = params.toString();
    return `/member-memberships${query ? `?${query}` : ""}`;
  };

  const statusChartData = useMemo(
    () => ({
      labels: ["Single Users", "Couple Users"],
      datasets: [
        {
          data: [singleUsersCount, coupleUsersCount],
          backgroundColor: ["#4f46e5", "#818cf8"],
          borderWidth: 0,
        },
      ],
    }),
    [singleUsersCount, coupleUsersCount]
  );

  const revenueChartData = useMemo(
    () => ({
      labels: revenueTrend.map((x) => x.month),
      datasets: [
        {
          label: "Revenue",
          data: revenueTrend.map((x) => x.revenue),
          fill: true,
          tension: 0.35,
          borderColor: "#4f46e5",
          backgroundColor: "rgba(79, 70, 229, 0.15)",
          pointBackgroundColor: "#4f46e5",
          pointRadius: 4,
        },
      ],
    }),
    [revenueTrend]
  );

  return (
    <section className="space-y-5">
      {error && <p className="rounded-xl bg-red-50 p-4 text-sm font-medium text-red-700">{error}</p>}

      <div className="space-y-2">
        <p className="text-xs font-semibold uppercase tracking-[0.16em] text-slate-500">Overall Snapshot</p>
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <KPI
            title="Total Users"
            value={loading ? "..." : totalUsers}
            subtitle="All members in your gym"
            accentClass="bg-cyan-500"
            valueClass="text-slate-900"
            badge="Global"
            onClick={() => navigate("/members")}
          />
          <KPI
            title="Active Users"
            value={loading ? "..." : activeUsers}
            subtitle="Users with valid membership plan"
            accentClass="bg-emerald-500"
            valueClass="text-slate-900"
            badge="Global"
            onClick={() => navigate("/members?activePlansOnly=true")}
          />
          <KPI
            title="Ending In 7 Days"
            value={loading ? "..." : membershipEndingSoonCount}
            subtitle="Memberships ending soon"
            accentClass="bg-amber-500"
            valueClass="text-slate-900"
            badge="Global"
            onClick={() => navigate("/member-memberships?endingInDays=7")}
          />
          <KPI
            title="Completed Plans"
            value={loading ? "..." : completedPlansCount}
            subtitle="Users with ended plans and no active plan"
            accentClass="bg-rose-500"
            valueClass="text-slate-900"
            badge="Global"
            onClick={() => navigate("/members?completedPlansOnly=true")}
          />
        </div>
      </div>

      <div className="rounded-2xl border border-slate-200 bg-gradient-to-r from-slate-50 via-white to-indigo-50 p-4 shadow-sm">
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <h3 className="text-base font-semibold text-slate-900">Membership Filter</h3>
          <div className="flex flex-col items-end gap-2">
            <span className="rounded-full border border-slate-200 bg-white px-3 py-1 text-xs font-semibold text-slate-600">
              {selectedRangeLabel}
            </span>
            <div className="grid w-full min-w-[300px] gap-2 sm:grid-cols-2">
              <div className="rounded-xl border border-emerald-200 bg-emerald-50 px-3 py-2">
                <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-emerald-700">Current Month Revenue</p>
                <button
                  type="button"
                  onClick={() => navigate("/member-memberships?collectedThisMonthOnly=true")}
                  className="mt-1 text-left text-lg font-extrabold text-emerald-800 underline decoration-emerald-400 underline-offset-2 hover:text-emerald-900"
                >
                  {loading ? "..." : currency.format(monthlyRevenue)}
                </button>
              </div>
              <div className="rounded-xl border border-amber-200 bg-amber-50 px-3 py-2">
                <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-amber-700">Pending Collection</p>
                <button
                  type="button"
                  onClick={() => navigate("/member-memberships?pendingCollectionOnly=true")}
                  className="mt-1 text-left text-lg font-extrabold text-amber-800 underline decoration-amber-400 underline-offset-2 hover:text-amber-900"
                >
                  {loading ? "..." : currency.format(pendingCollectionAmount)}
                </button>
              </div>
            </div>
          </div>
        </div>
        <div className="grid gap-3 md:grid-cols-4">
          <label>
            <span className="mb-1 block text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">Filter Type</span>
            <select
              value={filterType}
              onChange={(event) => setFilterType(event.target.value)}
              className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-400"
            >
              <option value={FILTER_OPTIONS.CURRENT_MONTH}>Current Month</option>
              <option value={FILTER_OPTIONS.PREVIOUS_MONTH}>Previous Month</option>
              <option value={FILTER_OPTIONS.DATE_RANGE}>Select Date Between</option>
              <option value={FILTER_OPTIONS.YEAR}>Select Year</option>
            </select>
          </label>

          {filterType === FILTER_OPTIONS.DATE_RANGE && (
            <>
              <label>
                <span className="mb-1 block text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">From</span>
                <input
                  type="date"
                  value={rangeFrom}
                  onChange={(event) => setRangeFrom(event.target.value)}
                  className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-400"
                />
              </label>
              <label>
                <span className="mb-1 block text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">To</span>
                <input
                  type="date"
                  value={rangeTo}
                  onChange={(event) => setRangeTo(event.target.value)}
                  className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-400"
                />
              </label>
            </>
          )}

          {filterType === FILTER_OPTIONS.YEAR && (
            <label>
              <span className="mb-1 block text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">Year</span>
              <select
                value={selectedYear}
                onChange={(event) => setSelectedYear(Number(event.target.value))}
                className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-400"
              >
                {yearOptions.map((year) => (
                  <option key={year} value={year}>
                    {year}
                  </option>
                ))}
              </select>
            </label>
          )}
        </div>
      </div>

      <div className="space-y-2">
        <p className="text-xs font-semibold uppercase tracking-[0.16em] text-slate-500">Filtered Membership KPIs</p>
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <KPI
          title="Total Membership Users"
          value={loading || filterLoading ? "..." : totalMembershipUsers}
          subtitle="Single + couple users for selected period"
          accentClass="bg-slate-700"
          valueClass="text-slate-900"
          badge="Filtered"
          onClick={() => navigate(buildFilteredMembershipsUrl())}
        />
        <KPI
          title="Couple Memberships"
          value={loading || filterLoading ? "..." : coupleMembershipCount}
          subtitle="Links with couple plan type"
          accentClass="bg-indigo-500"
          valueClass="text-slate-900"
          badge="Filtered"
          onClick={() => navigate(buildFilteredMembershipsUrl({ type: "Couple" }))}
        />
        <KPI
          title="Single Memberships"
          value={loading || filterLoading ? "..." : singleUsersCount}
          subtitle="Links with single plan type"
          accentClass="bg-violet-500"
          valueClass="text-slate-900"
          badge="Filtered"
          onClick={() => navigate(buildFilteredMembershipsUrl({ type: "Single" }))}
        />
      </div>
      </div>

      <div className="grid gap-4 xl:grid-cols-3">
        <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm xl:col-span-2">
          <h3 className="text-lg font-bold text-slate-900">Revenue Trend</h3>
          <p className="text-sm text-slate-500">Last 6 months payment collections</p>
          <div className="mt-4 h-[320px]">
            <Line
              data={revenueChartData}
              options={{
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                  x: { grid: { display: false } },
                  y: { grid: { color: "rgba(148, 163, 184, 0.2)" } },
                },
              }}
            />
          </div>
        </article>

        <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <h3 className="text-lg font-bold text-slate-900">Membership Mix</h3>
          <p className="text-sm text-slate-500">Single users vs couple users (filtered)</p>
          <div className="mt-4 h-[320px]">
            <Doughnut
              data={statusChartData}
              options={{
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: "bottom" } },
                cutout: "72%",
              }}
            />
          </div>
        </article>
      </div>
    </section>
  );
}

export default DashboardPage;
