import { Navigate } from "react-router";
import { useAuth } from "../Hooks/useAuth";

export const AdminRoute = ({ children }) => {
  const { user, isAuthenticated } = useAuth();
  console.log("ADMIN BUKAN : ", { isAuthenticated, user });

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }
  const isAdmin = user?.roles?.includes("ADMIN") ?? false;

  if (!isAdmin) {
    return <Navigate to="/dashboard" replace />;
  }
  return children;
};
