import { useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

const initialState = {
  name: "",
  email: "",
  password: "",
  dateOfBirth: "",
  gender: "Male",
  phone: "",
  addressLine: "",
  city: "",
  state: "",
  pincode: "",
  height: "0",
  weight: "0",
};

function RegisterPage() {
  const navigate = useNavigate();
  const { registerMember, getApiErrorMessage } = useAuth();
  const [form, setForm] = useState(initialState);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const canSubmit = useMemo(
    () =>
      Boolean(
        form.name &&
          form.email &&
          form.password &&
          form.dateOfBirth &&
          form.phone &&
          form.addressLine &&
          form.city &&
          form.state &&
          form.pincode
      ),
    [form]
  );

  const handleChange = (event) => {
    const { name, value } = event.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setLoading(true);
    setError("");

    try {
      await registerMember({
        ...form,
        height: Number(form.height || 0),
        weight: Number(form.weight || 0),
      });
      navigate("/", { replace: true });
    } catch (err) {
      setError(getApiErrorMessage(err, "Registration failed."));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <div className="w-full max-w-2xl rounded-2xl border border-white/60 bg-white/90 p-8 shadow-xl">
        <h1 className="text-3xl font-bold text-slate-900">Member Registration</h1>
        <p className="mt-1 text-sm text-slate-600">Create your member account.</p>

        <form className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2" onSubmit={handleSubmit}>
          {[
            ["name", "Name", "text"],
            ["email", "Email", "email"],
            ["password", "Password", "password"],
            ["dateOfBirth", "Date of Birth", "date"],
            ["phone", "Phone", "text"],
            ["addressLine", "Address", "text"],
            ["city", "City", "text"],
            ["state", "State", "text"],
            ["pincode", "Pincode", "text"],
            ["height", "Height", "number"],
            ["weight", "Weight", "number"],
          ].map(([name, label, type]) => (
            <div key={name} className={name === "addressLine" ? "sm:col-span-2" : ""}>
              <label className="mb-1 block text-sm font-semibold text-slate-700" htmlFor={name}>
                {label}
              </label>
              <input
                id={name}
                name={name}
                type={type}
                value={form[name]}
                onChange={handleChange}
                required={!["height", "weight"].includes(name)}
                className="w-full rounded-lg border border-slate-300 px-3 py-2 outline-none focus:border-orange-500"
              />
            </div>
          ))}

          <div>
            <label className="mb-1 block text-sm font-semibold text-slate-700" htmlFor="gender">
              Gender
            </label>
            <select
              id="gender"
              name="gender"
              value={form.gender}
              onChange={handleChange}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 outline-none focus:border-orange-500"
            >
              <option value="Male">Male</option>
              <option value="Female">Female</option>
              <option value="Other">Other</option>
            </select>
          </div>

          {error && <p className="sm:col-span-2 text-sm font-medium text-red-600">{error}</p>}

          <div className="sm:col-span-2 flex items-center justify-between gap-4">
            <Link to="/login" className="text-sm font-semibold text-orange-600 hover:text-orange-700">
              Back to login
            </Link>
            <button
              type="submit"
              disabled={loading || !canSubmit}
              className="rounded-lg bg-orange-500 px-4 py-2 font-semibold text-white hover:bg-orange-600 disabled:opacity-60"
            >
              {loading ? "Creating account..." : "Register"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default RegisterPage;
