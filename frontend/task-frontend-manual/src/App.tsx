import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import LoginPage from './pages/Login/LoginPage';
import RegisterPage from './pages/Register/RegisterPage';
import DashboardPage from './pages/Dashboard/DashboardPage';
import AdminPanelPage from './pages/AdminPanel/AdminPanelPage'
import MainLayout from './components/layout/MainLayout';
import Adminguard from './components/layout/AdminGuard';
import ProtectedRoute from './components/ProtectedRoute';
import { ProjectsPage } from './pages/Project/ProjectPage';
import { ProjectDetailsPage } from './pages/Dashboard/Project/ProjectDetailsPage';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />

        <Route path="/register" element={<RegisterPage />} />

        <Route element={<ProtectedRoute />}>
          <Route element={<MainLayout />}>
            <Route path='/dashboard' element={<DashboardPage />} />
            <Route path='/projects' element={<ProjectsPage/>}/>
            <Route path="/projects/:id" element={<ProjectDetailsPage />} />
            <Route element={<Adminguard />}>
              <Route path='/adminPanel' element={<AdminPanelPage />} />
            </Route>
          </Route>
        </Route>

        <Route path="/" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  );
}