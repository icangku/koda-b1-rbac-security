import { Routes, Route, Navigate } from "react-router";
import LoginPage from "./Pages/LoginPage";
import ProtectedRoute from "./Auth/ProtectedRoute";
import DashboardPage from "./Pages/DashboardPage";
import { AdminRoute } from "./Auth/AdminRoute";
import { AdminCategoryPage } from "./Pages/AdminCategoryPage";

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/dashboard"
        element={
          <ProtectedRoute>
            <DashboardPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/admin/category"
        element={
          <ProtectedRoute>
            <AdminRoute>
              <AdminCategoryPage />
            </AdminRoute>
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}
