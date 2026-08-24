import { useQuery } from "@apollo/client/react";
import { gql } from "@apollo/client";
import { AvailableProjectDashboard } from "../../types/Dashboard";

const GET_AVAILABLE_PROJECTS_DASHBOARD = gql`
  query GetAvailableProjectsDashboard {
    availableProjectsDashboard {
      id
      title
      description
      status
      managerId
      managerName
      managerEmail
    }
  }
`;

export function AvailableProjectsSection() {
    const { data, loading, error } = useQuery<{ availableProjectsDashboard: AvailableProjectDashboard[] }>(GET_AVAILABLE_PROJECTS_DASHBOARD, {
        fetchPolicy: "network-only",
    });

    if (loading) {
        return (
            <div className="bg-bg-card p-6 rounded-xl shadow-sm border border-gray-100">
                <div className="text-text-muted">Завантаження доступних проєктів...</div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="bg-red-50 text-red-700 p-4 rounded-xl border border-red-100">
                Не вдалося завантажити доступні проєкти.
            </div>
        );
    }

    const projects = data?.availableProjectsDashboard ?? [];

    if (projects.length === 0) {
        return (
            <div className="bg-bg-card p-6 rounded-xl shadow-sm border border-gray-100">
                <div className="text-text-muted">Немає доступних проєктів.</div>
            </div>
        );
    }

    return (
        <div className="space-y-4">
            <h2 className="text-xl font-bold text-text-main">Доступні проєкти</h2>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {projects.map((project) => (
                    <div
                        key={project.id}
                        className="bg-bg-card p-4 rounded-xl shadow-sm border border-gray-100 flex flex-col gap-3"
                    >
                        <div className="flex justify-between items-start">
                            <div>
                                <h3 className="text-lg font-semibold text-text-main">{project.title}</h3>
                                <p className="text-sm text-text-muted">
                                    Менеджер: {project.managerName} ({project.managerEmail})
                                </p>
                            </div>

                            <span className="px-2 py-1 text-xs rounded-full bg-gray-100 text-gray-700">
                {project.status}
              </span>
                        </div>

                        <p className="text-sm text-text-muted line-clamp-2">
                            {project.description?.trim() ? project.description : "Опис відсутній."}
                        </p>
                    </div>
                ))}
            </div>
        </div>
    );
}