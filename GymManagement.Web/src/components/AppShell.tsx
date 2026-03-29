import { useEffect, useMemo, useRef, useState } from "react";
import { Link, NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { api } from "../api/client";
import { useAuth } from "../context/AuthContext";

type HeaderMemberSearchItem = {
  id: string;
  name: string;
  email: string;
  phone: string;
};

type SubscriptionStatus = {
  plan: string;
  validTill: string | null;
  lifetimePlanActivated: boolean;
  isExpired: boolean;
  daysRemaining: number;
  currentPlanAmount: number;
};

type SubscriptionPlanOption = {
  id: string;
  code: string;
  name: string;
  price: number;
  durationMonths: number;
  isLifetime: boolean;
  isMaintenance: boolean;
  isActive: boolean;
  sortOrder: number;
  description: string;
};

function navClass({ isActive }, collapsed = false) {
  return [
    "group flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition",
    isActive
      ? "bg-indigo-50 text-indigo-600"
      : "text-slate-600 hover:bg-slate-100 hover:text-slate-900",
    collapsed ? "justify-center" : "",
  ].join(" ");
}

function isPathMatch(pathname, targetPath) {
  if (targetPath === "/") {
    return pathname === "/";
  }
  return pathname === targetPath || pathname.startsWith(`${targetPath}/`);
}

function initials(name) {
  if (!name) {
    return "U";
  }
  const parts = name.trim().split(/\s+/);
  return `${parts[0]?.[0] ?? ""}${parts[1]?.[0] ?? ""}`.toUpperCase() || "U";
}

function AppShell() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [searchResults, setSearchResults] = useState<HeaderMemberSearchItem[]>([]);
  const [searching, setSearching] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [subscription, setSubscription] = useState<SubscriptionStatus | null>(null);
  const [loadingSubscription, setLoadingSubscription] = useState(false);
  const [subscriptionModalOpen, setSubscriptionModalOpen] = useState(false);
  const [processingSubscription, setProcessingSubscription] = useState(false);
  const [subscriptionError, setSubscriptionError] = useState("");
  const [subscriptionSuccess, setSubscriptionSuccess] = useState("");
  const [selectedPlanCode, setSelectedPlanCode] = useState("");
  const [subscriptionPlans, setSubscriptionPlans] = useState<SubscriptionPlanOption[]>([]);
  const [subscriptionPaymentMode, setSubscriptionPaymentMode] = useState("UPI");
  const [subscriptionTxnRef, setSubscriptionTxnRef] = useState("");
  const [subscriptionNotes, setSubscriptionNotes] = useState("");
  const profileMenuRef = useRef<HTMLDivElement | null>(null);
  const searchContainerRef = useRef<HTMLDivElement | null>(null);
  const isAdmin = useMemo(() => user?.role === "Admin", [user?.role]);
  const isTrainer = useMemo(() => user?.role === "Trainer", [user?.role]);
  const isSuperAdmin = useMemo(() => user?.role === "SuperAdmin", [user?.role]);
  const isGymAdmin = isAdmin && !isSuperAdmin;
  const canSearchMembers = isAdmin || isTrainer;

  const menuItems = isSuperAdmin
    ? [
        { to: "/super-admin/gyms", label: "Gyms", icon: "GY" },
        { to: "/super-admin/subscription-plans", label: "Subscription Plans", icon: "SP" },
        { to: "/super-admin/invoice-settings", label: "Invoice Settings", icon: "IV" },
        { to: "/profile", label: "Profile", icon: "PR" },
      ]
    : [
        { to: "/", label: "Dashboard", icon: "DB" },
        {
          key: "management",
          label: "Management",
          icon: "MG",
          children: [
            { to: "/members", label: "Members" },
            { to: "/plans", label: "Plans" },
            { to: "/member-memberships", label: "Member Links" },
          ],
        },
        ...(isAdmin
          ? [
              {
                key: "reports",
                label: "Reports",
                icon: "RP",
                children: [
                  { to: "/reports/financial", label: "Financial Report" },
                  { to: "/reports/attendance", label: "Attendance Report" },
                  { to: "/reports/payment-dues", label: "Payment Dues" },
                  { to: "/reports/payment-collections", label: "Payment Collections" },
                ],
              },
              { to: "/business-details", label: "Business Details", icon: "BP" },
            ]
          : []),
        { to: "/profile", label: "Profile", icon: "PR" },
      ];

  const [openSubmenus, setOpenSubmenus] = useState(() =>
    menuItems.reduce((acc, item) => {
      if (item.children?.length) {
        acc[item.key] = item.children.some((child) => isPathMatch(location.pathname, child.to));
      }
      return acc;
    }, {}),
  );

  useEffect(() => {
    setOpenSubmenus((prev) => {
      const next = { ...prev };
      let changed = false;

      menuItems.forEach((item) => {
        if (!item.children?.length) {
          return;
        }

        const shouldOpen = item.children.some((child) => isPathMatch(location.pathname, child.to));
        if (shouldOpen && !next[item.key]) {
          next[item.key] = true;
          changed = true;
        }
      });

      return changed ? next : prev;
    });
  }, [location.pathname, isAdmin, isSuperAdmin]);

  useEffect(() => {
    const handleDocumentMouseDown = (event: MouseEvent) => {
      if (!profileOpen) {
        return;
      }

      if (profileMenuRef.current && !profileMenuRef.current.contains(event.target as Node)) {
        setProfileOpen(false);
      }
    };

    document.addEventListener("mousedown", handleDocumentMouseDown);
    return () => {
      document.removeEventListener("mousedown", handleDocumentMouseDown);
    };
  }, [profileOpen]);

  useEffect(() => {
    const handleDocumentMouseDown = (event: MouseEvent) => {
      if (!searchOpen) {
        return;
      }

      if (searchContainerRef.current && !searchContainerRef.current.contains(event.target as Node)) {
        setSearchOpen(false);
      }
    };

    document.addEventListener("mousedown", handleDocumentMouseDown);
    return () => {
      document.removeEventListener("mousedown", handleDocumentMouseDown);
    };
  }, [searchOpen]);

  useEffect(() => {
    if (!canSearchMembers) {
      return;
    }

    const query = searchTerm.trim();
    if (query.length < 3) {
      setSearchResults([]);
      setSearching(false);
      setSearchOpen(false);
      return;
    }

    const timeoutId = window.setTimeout(async () => {
      setSearching(true);
      try {
        const response = await api.get("/api/members/paged", {
          params: {
            page: 1,
            pageSize: 8,
            search: query,
          },
        });
        setSearchResults((response.data?.items ?? []) as HeaderMemberSearchItem[]);
        setSearchOpen(true);
      } catch {
        setSearchResults([]);
      } finally {
        setSearching(false);
      }
    }, 300);

    return () => {
      clearTimeout(timeoutId);
    };
  }, [searchTerm, canSearchMembers]);

  const fetchSubscription = async () => {
    if (isSuperAdmin) {
      setSubscription(null);
      return;
    }

    setLoadingSubscription(true);
    try {
      const response = await api.get("/api/subscription/current");
      setSubscription((response.data ?? null) as SubscriptionStatus | null);
    } catch {
      setSubscription(null);
    } finally {
      setLoadingSubscription(false);
    }
  };

  useEffect(() => {
    fetchSubscription();
  }, [isSuperAdmin, user?.role]);

  const fetchSubscriptionPlans = async () => {
    if (isSuperAdmin) {
      setSubscriptionPlans([]);
      return;
    }

    try {
      const response = await api.get("/api/subscription-plans/active");
      setSubscriptionPlans((response.data ?? []) as SubscriptionPlanOption[]);
    } catch {
      setSubscriptionPlans([]);
    }
  };

  useEffect(() => {
    fetchSubscriptionPlans();
  }, [isSuperAdmin, user?.role]);

  const getPlanAmount = (planCode: string) => {
    if (!planCode) {
      return 0;
    }
    return subscriptionPlans.find((plan) => plan.code === planCode)?.price ?? 0;
  };

  const paySubscription = async () => {
    if (!selectedPlanCode) {
      setSubscriptionError("Please select a subscription plan first.");
      return;
    }

    setProcessingSubscription(true);
    setSubscriptionError("");
    setSubscriptionSuccess("");
    try {
      const response = await api.post("/api/subscription/pay", {
        planCode: selectedPlanCode,
        paymentMode: subscriptionPaymentMode,
        transactionReference: subscriptionTxnRef || null,
        notes: subscriptionNotes || null,
        paidOn: new Date().toISOString(),
        amount: getPlanAmount(selectedPlanCode),
      });
      await fetchSubscription();
      const invoiceNumber = response.data?.invoiceNumber;
      const mailStatus = response.data?.invoiceEmailSent
        ? "Invoice emailed to gym admin."
        : response.data?.invoiceEmailError
          ? `Invoice email failed: ${response.data.invoiceEmailError}`
          : "Invoice email status unavailable.";
      setSubscriptionSuccess(`Payment recorded (${invoiceNumber}). ${mailStatus}`);
      setSubscriptionModalOpen(false);
      setSelectedPlanCode("");
      setSubscriptionTxnRef("");
      setSubscriptionNotes("");
    } catch (err) {
      setSubscriptionError(
        (err as any)?.response?.data?.message ?? "Unable to activate subscription."
      );
    } finally {
      setProcessingSubscription(false);
    }
  };

  const showSubscriptionBanner =
    !isSuperAdmin &&
    !loadingSubscription &&
    subscription &&
    (subscription.isExpired || subscription.daysRemaining <= 30);

  const handleMemberPick = (memberId: string) => {
    setSearchOpen(false);
    setSearchTerm("");
    setSearchResults([]);
    navigate(`/members/${memberId}`);
  };

  return (
    <div className="min-h-screen bg-slate-50">
      <div className="flex min-h-screen">
        <aside
          className={`border-r border-slate-200 bg-white px-3 py-4 transition-all duration-200 ${
            sidebarCollapsed ? "w-[86px]" : "w-[280px]"
          }`}
        >
          <div className="flex items-center gap-3 px-2 pb-6">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-indigo-600 text-sm font-bold text-white">
              GM
            </div>
            {!sidebarCollapsed && <h1 className="text-2xl font-bold text-slate-900">ManageMyGym</h1>}
          </div>

          {!sidebarCollapsed && <p className="px-3 pb-3 text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Menu</p>}
          <nav className="space-y-1">
            {menuItems.map((item) => (
              <div key={item.key ?? item.to}>
                {item.children?.length ? (
                  <>
                    <button
                      type="button"
                      onClick={() => {
                        if (sidebarCollapsed) {
                          setSidebarCollapsed(false);
                          setOpenSubmenus((prev) => ({ ...prev, [item.key]: true }));
                          return;
                        }
                        setOpenSubmenus((prev) => ({ ...prev, [item.key]: !prev[item.key] }));
                      }}
                      className={[
                        "group flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition",
                        item.children.some((child) => isPathMatch(location.pathname, child.to))
                          ? "bg-indigo-50 text-indigo-600"
                          : "text-slate-600 hover:bg-slate-100 hover:text-slate-900",
                        sidebarCollapsed ? "justify-center" : "",
                      ].join(" ")}
                    >
                      <span className="flex h-8 w-8 items-center justify-center rounded-md border border-slate-200 bg-slate-50 text-[10px] font-bold text-slate-600">
                        {item.icon}
                      </span>
                      {!sidebarCollapsed && <span className="flex-1 text-left">{item.label}</span>}
                      {!sidebarCollapsed && (
                        <svg
                          viewBox="0 0 20 20"
                          className={`h-4 w-4 transition-transform ${openSubmenus[item.key] ? "rotate-180" : ""}`}
                          fill="none"
                          stroke="currentColor"
                          strokeWidth="2"
                        >
                          <path d="m5 7 5 6 5-6" strokeLinecap="round" strokeLinejoin="round" />
                        </svg>
                      )}
                    </button>

                    {!sidebarCollapsed && (
                      <div
                        className={`ml-11 grid overflow-hidden transition-all duration-200 ${
                          openSubmenus[item.key] ? "mt-1 grid-rows-[1fr] opacity-100" : "grid-rows-[0fr] opacity-0"
                        }`}
                      >
                        <div className="min-h-0 space-y-1">
                          {item.children.map((child) => (
                            <NavLink
                              key={child.to}
                              to={child.to}
                              className={({ isActive }) =>
                                [
                                  "block rounded-lg px-3 py-1.5 text-xs font-semibold transition",
                                  isActive || isPathMatch(location.pathname, child.to)
                                    ? "bg-indigo-50 text-indigo-600"
                                    : "text-slate-500 hover:bg-slate-100 hover:text-slate-700",
                                ].join(" ")
                              }
                            >
                              {child.label}
                            </NavLink>
                          ))}
                        </div>
                      </div>
                    )}
                  </>
                ) : (
                  <NavLink to={item.to} end={item.to === "/"} className={(s) => navClass(s, sidebarCollapsed)}>
                    <span className="flex h-8 w-8 items-center justify-center rounded-md border border-slate-200 bg-slate-50 text-[10px] font-bold text-slate-600">
                      {item.icon}
                    </span>
                    {!sidebarCollapsed && <span>{item.label}</span>}
                  </NavLink>
                )}
              </div>
            ))}
          </nav>
        </aside>

        <div className="flex min-h-screen flex-1 flex-col">
          <header className="border-b border-slate-200 bg-white px-4 py-3 sm:px-6">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="flex items-center gap-3">
                <button
                  type="button"
                  className="inline-flex h-11 w-11 items-center justify-center rounded-xl border border-slate-200 bg-white text-slate-600 hover:bg-slate-50"
                  onClick={() => setSidebarCollapsed((prev) => !prev)}
                  aria-label={sidebarCollapsed ? "Expand sidebar" : "Collapse sidebar"}
                >
                  <svg viewBox="0 0 24 24" className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M3 6h18M3 12h18M3 18h18" strokeLinecap="round" />
                  </svg>
                </button>

                <div className="relative hidden sm:block" ref={searchContainerRef}>
                  <div className="flex items-center gap-2 rounded-xl border border-slate-200 bg-slate-50 px-3 py-2 sm:min-w-[360px]">
                    <svg viewBox="0 0 24 24" className="h-5 w-5 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2">
                      <circle cx="11" cy="11" r="8" />
                      <path d="M21 21l-4.3-4.3" strokeLinecap="round" />
                    </svg>
                    <input
                      type="text"
                      value={searchTerm}
                      autoComplete="false"
                      onChange={(event) => setSearchTerm(event.target.value)}
                      onKeyDown={(event) => {
                        if (event.key === "Enter" && searchResults.length > 0) {
                          event.preventDefault();
                          handleMemberPick(searchResults[0].id);
                        }
                      }}
                      disabled={!canSearchMembers}
                      placeholder={canSearchMembers ? "Search members by name, email, or phone..." : "Member search is unavailable"}
                      className="w-full bg-transparent text-sm text-slate-700 outline-none placeholder:text-slate-400 disabled:cursor-not-allowed disabled:text-slate-400"
                    />
                  </div>

                  {canSearchMembers && searchOpen && (
                    <div className="absolute left-0 right-0 z-40 mt-2 max-h-80 overflow-auto rounded-xl border border-slate-200 bg-white p-1 shadow-lg">
                      {searching ? (
                        <p className="px-3 py-2 text-sm text-slate-500">Searching...</p>
                      ) : searchResults.length === 0 ? (
                        <p className="px-3 py-2 text-sm text-slate-500">
                          {searchTerm.trim().length < 3 ? "Type at least 3 characters." : "No members found."}
                        </p>
                      ) : (
                        searchResults.map((member) => (
                          <button
                            key={member.id}
                            type="button"
                            onClick={() => handleMemberPick(member.id)}
                            className="flex w-full items-start justify-between rounded-lg px-3 py-2 text-left hover:bg-slate-50"
                          >
                            <span>
                              <span className="block text-sm font-semibold text-slate-900">{member.name}</span>
                              <span className="block text-xs text-slate-500">{member.email}</span>
                            </span>
                            <span className="text-xs font-medium text-slate-500">{member.phone}</span>
                          </button>
                        ))
                      )}
                    </div>
                  )}
                </div>
              </div>

              <div className="relative" ref={profileMenuRef}>
                <button
                  type="button"
                  onClick={() => setProfileOpen((prev) => !prev)}
                  className="flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2 text-left hover:bg-slate-50"
                >
                  <span className="flex h-8 w-8 items-center justify-center overflow-hidden rounded-full bg-indigo-100 text-xs font-bold text-indigo-700">
                    {user?.profilePhotoDataUri ? (
                      <img src={user.profilePhotoDataUri} alt="Profile" className="h-full w-full object-cover" />
                    ) : (
                      initials(user?.name)
                    )}
                  </span>
                  <span className="hidden sm:block">
                    <span className="block text-sm font-semibold text-slate-900">{user?.name}</span>
                    <span className="block text-xs text-slate-500">{user?.role}</span>
                  </span>
                </button>

                {profileOpen && (
                  <div className="absolute right-0 z-30 mt-2 w-64 rounded-2xl border border-slate-200 bg-white p-2 text-slate-700 shadow-xl">
                    <Link
                      to="/profile"
                      onClick={() => setProfileOpen(false)}
                      className="block rounded-lg px-3 py-2 text-sm font-medium hover:bg-slate-50"
                    >
                      Profile
                    </Link>
                    <Link
                      to="/business-details"
                      onClick={() => setProfileOpen(false)}
                      className={`block rounded-lg px-3 py-2 text-sm font-medium ${
                        isAdmin ? "hover:bg-slate-50" : "cursor-not-allowed text-slate-400"
                      }`}
                    >
                      Business Profile
                    </Link>
                    <Link
                      to="/reset-password"
                      onClick={() => setProfileOpen(false)}
                      className="block rounded-lg px-3 py-2 text-sm font-medium hover:bg-slate-50"
                    >
                      Reset Password
                    </Link>
                    <button
                      type="button"
                      onClick={logout}
                      className="mt-2 w-full rounded-lg bg-indigo-600 px-3 py-2 text-sm font-semibold text-white hover:bg-indigo-700"
                    >
                      Logout
                    </button>
                  </div>
                )}
              </div>
            </div>
          </header>

          {showSubscriptionBanner && (
            <div
              className={`mx-4 mt-3 rounded-xl border px-4 py-3 text-sm sm:mx-6 ${
                subscription.isExpired
                  ? "border-red-200 bg-red-50 text-red-700"
                  : "border-amber-200 bg-amber-50 text-amber-800"
              }`}
            >
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <p className="font-semibold">
                    {subscription.isExpired
                      ? "Your subscription has expired."
                      : `Your subscription will end in ${subscription.daysRemaining} day(s).`}
                  </p>
                  <p className="text-xs">
                    {subscription.isExpired
                      ? "Select a plan and pay to continue adding members, plans and links."
                      : "Buy/renew a plan to continue using all gym activities without interruption."}
                  </p>
                </div>
                {isGymAdmin ? (
                  <button
                    type="button"
                    onClick={() => setSubscriptionModalOpen(true)}
                    className={`rounded-lg px-3 py-2 text-xs font-semibold text-white ${
                      subscription.isExpired ? "bg-red-600 hover:bg-red-700" : "bg-amber-600 hover:bg-amber-700"
                    }`}
                  >
                    {subscription.isExpired ? "Select Plan" : "Renew Plan"}
                  </button>
                ) : (
                  <p className="text-xs font-semibold">Please contact your gym admin to renew.</p>
                )}
              </div>
            </div>
          )}

          {subscriptionSuccess && (
            <p className="mx-4 mt-3 rounded-lg bg-emerald-50 px-3 py-2 text-sm font-medium text-emerald-700 sm:mx-6">
              {subscriptionSuccess}
            </p>
          )}

          <main className="flex-1 p-4 sm:p-6">
            <Outlet key={location.pathname} />
          </main>

          <footer className="border-t border-slate-200 bg-white px-4 py-3 text-xs text-slate-500 sm:px-6">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <p>ManageMyGym Console</p>
              <p>Profile, business profile and admin controls are in the header menu.</p>
            </div>
          </footer>
        </div>
      </div>

      {subscriptionModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="w-full max-w-2xl rounded-2xl border border-slate-200 bg-white p-5 shadow-2xl">
            <div className="flex items-start justify-between gap-3">
              <div>
                <h3 className="text-lg font-bold text-slate-900">Choose Subscription Plan</h3>
                <p className="mt-1 text-sm text-slate-600">
                  After expiry, creating members, plans and membership links is blocked until renewal.
                </p>
              </div>
              <button
                type="button"
                onClick={() => setSubscriptionModalOpen(false)}
                className="rounded-md border border-slate-300 px-2 py-1 text-xs font-semibold text-slate-700 hover:bg-slate-100"
              >
                Close
              </button>
            </div>

            {subscriptionError && (
              <p className="mt-3 rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">{subscriptionError}</p>
            )}

            <div className="mt-4 grid grid-cols-1 gap-3 md:grid-cols-3">
              {!subscription?.lifetimePlanActivated && (
                subscriptionPlans
                  .filter((plan) => !plan.isMaintenance)
                  .map((plan) => (
                    <PlanCard
                      key={plan.id}
                      title={plan.name}
                      price={`₹${plan.price.toLocaleString()}`}
                      subtitle={
                        plan.description?.trim()
                          ? plan.description
                          : `${plan.durationMonths} month(s) validity`
                      }
                      onChoose={() => setSelectedPlanCode(plan.code)}
                      selected={selectedPlanCode === plan.code}
                      disabled={processingSubscription}
                    />
                  ))
              )}

              {subscription?.lifetimePlanActivated && (
                <>
                  {subscriptionPlans
                    .filter((plan) => plan.isLifetime)
                    .map((plan) => (
                      <PlanCard
                        key={plan.id}
                        title={plan.name}
                        price="Active"
                        subtitle="One-time plan already adopted"
                        onChoose={() => {}}
                        disabled
                      />
                    ))}
                  {subscriptionPlans
                    .filter((plan) => plan.isMaintenance)
                    .map((plan) => (
                      <PlanCard
                        key={plan.id}
                        title={plan.name}
                        price={`₹${plan.price.toLocaleString()}`}
                        subtitle={plan.description || "Maintenance renewal"}
                        onChoose={() => setSelectedPlanCode(plan.code)}
                        selected={selectedPlanCode === plan.code}
                        disabled={processingSubscription}
                      />
                    ))}
                </>
              )}
            </div>

            <div className="mt-4 rounded-xl border border-slate-200 bg-slate-50 p-3">
              <h4 className="text-sm font-bold text-slate-900">Payment Details</h4>
              <div className="mt-3 grid grid-cols-1 gap-3 md:grid-cols-3">
                <label className="md:col-span-1">
                  <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Payment Mode</span>
                  <select
                    value={subscriptionPaymentMode}
                    onChange={(event) => setSubscriptionPaymentMode(event.target.value)}
                    className="h-10 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm outline-none focus:border-orange-500"
                  >
                    <option value="UPI">UPI</option>
                    <option value="Card">Card</option>
                    <option value="BankTransfer">Bank Transfer</option>
                    <option value="Cash">Cash</option>
                  </select>
                </label>
                <label className="md:col-span-2">
                  <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Transaction Ref (Optional)</span>
                  <input
                    value={subscriptionTxnRef}
                    onChange={(event) => setSubscriptionTxnRef(event.target.value)}
                    className="h-10 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm outline-none focus:border-orange-500"
                  />
                </label>
                <label className="md:col-span-3">
                  <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-600">Notes (Optional)</span>
                  <input
                    value={subscriptionNotes}
                    onChange={(event) => setSubscriptionNotes(event.target.value)}
                    className="h-10 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm outline-none focus:border-orange-500"
                  />
                </label>
              </div>
              <div className="mt-3 flex items-center justify-between">
                <p className="text-sm font-semibold text-slate-700">
                  Amount: <span className="text-orange-600">₹{getPlanAmount(selectedPlanCode).toLocaleString()}</span>
                </p>
                <button
                  type="button"
                  onClick={paySubscription}
                  disabled={processingSubscription || !selectedPlanCode}
                  className="rounded-md bg-orange-500 px-4 py-2 text-xs font-semibold text-white hover:bg-orange-600 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {processingSubscription ? "Processing..." : "Pay & Activate"}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function PlanCard({
  title,
  price,
  subtitle,
  onChoose,
  selected,
  disabled,
}: {
  title: string;
  price: string;
  subtitle: string;
  onChoose: () => void;
  selected?: boolean;
  disabled?: boolean;
}) {
  return (
    <div className={`rounded-xl border p-3 ${selected ? "border-orange-400 bg-orange-50" : "border-slate-200 bg-slate-50"}`}>
      <h4 className="text-sm font-bold text-slate-900">{title}</h4>
      <p className="mt-1 text-lg font-bold text-orange-600">{price}</p>
      <p className="mt-1 text-xs text-slate-600">{subtitle}</p>
      <button
        type="button"
        onClick={onChoose}
        disabled={disabled}
        className="mt-3 w-full rounded-md bg-orange-500 px-3 py-2 text-xs font-semibold text-white hover:bg-orange-600 disabled:cursor-not-allowed disabled:opacity-50"
      >
        {disabled ? "Not Available" : "Select"}
      </button>
    </div>
  );
}

export default AppShell;
