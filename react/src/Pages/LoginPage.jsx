import { useState } from "react";
import { Navigate, useLocation, useNavigate } from "react-router";
import FormField from "../Components/Forms/FormField";
import { useAuth } from "../Hooks/useAuth";

export default function LoginPage() {
  const { login, status, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const destination = useLocation().state?.from?.pathname ?? "/dashboard";

  const [formData, setFormData] = useState({ email: "", password: "" });
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  const handleChange = ({ target: { name, value } }) => {
    setFormData((prev) => ({ ...prev, [name]: value }));
    setErrorMessage("");
  };

  async function handleSubmit(e) {
    e.preventDefault();
    if (isLoading) return;
    if (!formData.email.trim() || !formData.password) {
      return setErrorMessage("Email dan password wajib diisi.");
    }

    setErrorMessage("");
    setIsLoading(true);

    try {
      await login(formData); //login()
      setFormData((prev) => ({ ...prev, password: "" }));
      navigate(destination, { replace: true });
    } catch (err) {
      const msg =
        err?.status === 401
          ? "Email atau password salah."
          : err?.status === 429
            ? "Terlalu banyak percobaan Login. Coba kembali nanti."
            : err instanceof Error
              ? err.message
              : "Login gagal. Silakan coba kembali.";
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }

  if (status === "checking") {
    return (
      <main className="flex min-h-screen items-center justify-center">
        Memeriksa sesi Login...
      </main>
    );
  }
  if (isAuthenticated) return <Navigate to="/dashboard" replace />;

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-100 p-4">
      <div className="w-full max-w-sm rounded-lg border bg-white p-6 shadow-sm">
        <h1 className="text-xl font-bold text-slate-900">Login KodaStore</h1>

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          <FormField
            id="email"
            label="Email"
            type="email"
            value={formData.email}
            onChange={handleChange}
            autoComplete="email"
            placeholder="nama@example.com"
            disabled={isLoading}
          />
          <FormField
            id="password"
            label="Password"
            type="password"
            value={formData.password}
            onChange={handleChange}
            autoComplete="current-password"
            placeholder="Masukkan password"
            disabled={isLoading}
          />

          <button
            type="submit"
            disabled={isLoading || !formData.email.trim() || !formData.password}
            className="w-full rounded-md bg-orange-600 py-2 font-medium text-white hover:bg-orange-700 disabled:opacity-50"
          >
            {isLoading ? "Memproses Login..." : "Login"}
          </button>
        </form>

        {errorMessage && (
          <p
            role="alert"
            className="mt-4 rounded-md bg-red-50 p-3 text-sm text-red-700"
          >
            {errorMessage}
          </p>
        )}
      </div>
    </main>
  );
}
