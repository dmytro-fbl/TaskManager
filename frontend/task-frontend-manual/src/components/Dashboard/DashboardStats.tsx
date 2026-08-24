import { useQuery } from "@apollo/client/react";
import { gql } from "@apollo/client";
import { DashboardStats } from "../../types/Dashboard";

const GET_DASHBOARD_STATS = gql`
  query GetDashboardStats {
    dashboardStats {
      totalProjects
      totalBudgetHours
      totalUsedHours
      projectsOnTrack
      projectsAtRisk
      projectsOverBudget
    }
  }
`;

export function DashboardStatsCard() {
    const { data, loading, error } = useQuery<{ dashboardStats: DashboardStats }>(GET_DASHBOARD_STATS, {
        fetchPolicy: "network-only",
    });

    if (loading) {
        return (
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {[...Array(3)].map((_, i) => (
                    <div key={i} className="bg-bg-card p-4 rounded-xl shadow-sm border border-gray-100 animate-pulse h-24" />
                ))}
            </div>
        );
    }

    if (error) {
        return (
            <div className="bg-red-50 text-red-700 p-4 rounded-xl border border-red-100">
                Не вдалося завантажити статистику дашборду.
            </div>
        );
    }

    const stats = data?.dashboardStats;

    if (!stats) {
        return null;
    }

    return (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="bg-bg-card p-4 rounded-xl shadow-sm border border-gray-100">
                <div className="text-sm text-text-muted">Всього проєктів</div>
                <div className="text-2xl font-bold text-text-main">{stats.totalProjects}</div>
            </div>

            <div className="bg-bg-card p-4 rounded-xl shadow-sm border border-gray-100">
                <div className="text-sm text-text-muted">Всього бюджет (год)</div>
                <div className="text-2xl font-bold text-text-main">{stats.totalBudgetHours.toFixed(1)}</div>
            </div>

            <div className="bg-bg-card p-4 rounded-xl shadow-sm border border-gray-100">
                <div className="text-sm text-text-muted">Всього використано (год)</div>
                <div className="text-2xl font-bold text-text-main">{stats.totalUsedHours.toFixed(1)}</div>
            </div>

            <div className="bg-bg-card p-4 rounded-xl shadow-sm border border-gray-100">
                <div className="text-sm text-text-muted">On track (&lt;80%)</div>
                <div className="text-2xl font-bold text-green-600">{stats.projectsOnTrack}</div>
            </div>

            <div className="bg-bg-card p-4 rounded-xl shadow-sm border border-gray-100">
                <div className="text-sm text-text-muted">At risk (80–100%)</div>
                <div className="text-2xl font-bold text-yellow-600">{stats.projectsAtRisk}</div>
            </div>

            <div className="bg-bg-card p-4 rounded-xl shadow-sm border border-gray-100">
                <div className="text-sm text-text-muted">Over budget (100%+)</div>
                <div className="text-2xl font-bold text-red-600">{stats.projectsOverBudget}</div>
            </div>
        </div>
    );
}