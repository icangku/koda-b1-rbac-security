import { NavLink } from "react-router";
import { RoleGuard } from "../../Auth/RoleGuard";
export const RoleNavigation = () => {
  const getClassName = ({ isActive }) =>
    isActive ? "bg-orange-100" : "bg-slate-100";

  return (
    <nav>
      <NavLink to="/dashboard" className={getClassName}>
        Dashboard
      </NavLink>
      <RoleGuard allowRoles={["ADMIN"]}>
        <NavLink to="/admin/category" className={getClassName}>
          Manage Category
        </NavLink>
      </RoleGuard>
    </nav>
  );
};
