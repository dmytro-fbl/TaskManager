import { Navigate, Outlet } from 'react-router-dom';

export default function ProtectedRoute() {
  const accessToken = localStorage.getItem('accessToken');
  const refreshToken = localStorage.getItem('refreshToken');

  if (!accessToken || !refreshToken) {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}