import { useQuery } from "@apollo/client/react";
import { gql } from "@apollo/client";
import { useNavigate } from "react-router-dom";
import { MyProjectDashboard } from "../../types/Dashboard";
import { BudgetProgress } from "../ui/BudgetProgress";

const GET_MY_PROJECTS_DASHBOARD = gql`
  query GetMyProjectsDashboard {
    myProjectsDashboard {
      id
      title
      description
      budgetHours
      status
      myRole
      usedHours
    }
  }
`;

export function MyProjectsSection() {
    const navigate = useNavigate();
    const { data, loading, error } = useQuery<{ myProjectsDashboard: MyProjectDashboard[] }>(GET_MY_PROJECTS_DASHBOARD, {
        fetchPolicy: "network-only",
    });

    if (loading) {
        return (
            <div className="bg-bg-card p-6 rounded-xl shadow-sm border border-gray-100">
                <div className="text-text-muted">Завантаження моїх проєктів...</div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="bg-red-50 text-red-700 p-4 rounded-xl border border-red-100">
                Не вдалося завантажити мої проєкти.
            </div>
        );
    }

    const projects = data?.myProjectsDashboard ?? [];

    if (projects.length === 0) {
        return (
            <div className="bg-bg-card p-6 rounded-xl shadow-sm border border-gray-100">
                <div className="text-text-muted">У тебе ще немає проєктів.</div>
            </div>
        );
    }

    return (
        <div className="space-y-4">
            <h2 className="text-xl font-bold text-text-main">Мої проєкти</h2>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {projects.map((project) => (
                    <div
                        key={project.id}
                        onClick={() => navigate(`/projects/${project.id}`)}
                        className="bg-bg-card p-4 rounded-xl shadow-sm border border-gray-100 flex flex-col gap-3"
                    >
                        <div className="flex justify-between items-start">
                            <div>
                                <h3 className="text-lg font-semibold text-text-main">{project.title}</h3>
                                <p className="text-sm text-text-muted">Роль: {project.myRole}</p>
                            </div>

                            <span className="px-2 py-1 text-xs rounded-full bg-blue-100 text-blue-700">
                {project.status}
              </span>
                        </div>

                        <p className="text-sm text-text-muted line-clamp-2">
                            {project.description?.trim() ? project.description : "Опис відсутній."}
                        </p>

                        <BudgetProgress usedHours={project.usedHours} budgetHours={project.budgetHours} />
                    </div>
                ))}
            </div>
        </div>
    );
}