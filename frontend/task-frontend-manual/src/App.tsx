import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import LoginPage from './pages/Login/LoginPage';
import RegisterPage from './pages/Register/RegisterPage'; 
import DashboardPage from './pages/Dashboard/DashboardPage';
import AdminPanelPage from './pages/AdminPanel/AdminPanelPage'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        
        <Route path="/register" element={<RegisterPage />} />

        <Route path='/dashboard' element={<DashboardPage />} />
        <Route path='/adminPanel' element={<AdminPanelPage />} />

        <Route path="/" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  );
}