import { useQuery } from "@apollo/client/react";
import { gql } from "@apollo/client";
import { useNavigate } from "react-router-dom";
import { ManagerProjectDashboard } from "../../types/Dashboard";
import { BudgetProgress } from "../ui/BudgetProgress";

const GET_MANAGER_PROJECTS_DASHBOARD = gql`
  query GetManagerProjectsDashboard {
    managerProjectsDashboard {
      id
      title
      status
      budgetHours
      usedHours
      rolesHours {
        roleName
        usedHours
      }
    }
  }
`;

export function ManagerProjectsSection() {
    const navigate = useNavigate();
    const { data, loading, error } = useQuery<{ managerProjectsDashboard: ManagerProjectDashboard[] }>(GET_MANAGER_PROJECTS_DASHBOARD, {
        fetchPolicy: "network-only",
    });

    if (loading) {
        return (
            <div className="bg-bg-card p-6 rounded-xl shadow-sm border border-gray-100">
                <div className="text-text-muted">Завантаження проєктів менеджера...</div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="bg-red-50 text-red-700 p-4 rounded-xl border border-red-100">
                Не вдалося завантажити проєкти менеджера.
            </div>
        );
    }

    const projects = data?.managerProjectsDashboard ?? [];

    if (projects.length === 0) {
        return (
            <div className="bg-bg-card p-6 rounded-xl shadow-sm border border-gray-100">
                <div className="text-text-muted">Ти не є менеджером жодного проєкту.</div>
            </div>
        );
    }

    return (
        <div className="space-y-4">
            <h2 className="text-xl font-bold text-text-main">Проєкти, де ти менеджер</h2>

            <div className="grid grid-cols-1 gap-4">
                {projects.map((project) => (
                    <div
                        key={project.id}
                        onClick={() => navigate(`/projects/${project.id}`)}
                        className="bg-bg-card p-4 rounded-xl shadow-sm border border-gray-100 flex flex-col gap-3"
                    >
                        <div className="flex justify-between items-start">
                            <div>
                                <h3 className="text-lg font-semibold text-text-main">{project.title}</h3>
                                <p className="text-sm text-text-muted">Статус: {project.status}</p>
                            </div>

                            <span className="px-2 py-1 text-xs rounded-full bg-purple-100 text-purple-700">
                                Менеджер
                            </span>
                        </div>

                        <BudgetProgress usedHours={project.usedHours} budgetHours={project.budgetHours} />

                        {project.rolesHours.length > 0 && (
                            <div className="space-y-2">
                                <div className="text-sm font-medium text-text-main">Години по ролях:</div>

                                <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
                                    {project.rolesHours.map((role, idx) => (
                                        <div
                                            key={idx}
                                            className="text-sm text-text-muted bg-gray-50 p-2 rounded border border-gray-100"
                                        >
                                            <span className="font-medium text-text-main">{role.roleName}:</span>{" "}
                                            {role.usedHours.toFixed(1)} год
                                        </div>
                                    ))}
                                </div>
                            </div>
                        )}
                    </div>
                ))}
            </div>
        </div>
    );
}