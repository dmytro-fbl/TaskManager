import React, { useState } from "react";
import { useQuery } from "@apollo/client/react";
import { gql } from "@apollo/client";
import { useNavigate } from "react-router-dom";
import { CreateProjectForm } from "./components/CreateProjectForm";

interface Project {
    id: string;
    title: string;
    description?: string | null;
    budgetCap?: number | null;
    status: string;
    ownerId: string;
    isArchived: boolean;
    createdAt: string;
    updatedAt?: string | null;
}

const GET_PROJECTS = gql`
  query GetProjects {
    myProjects {
      id
      title
      description
      budgetCap
      status
      ownerId
      isArchived
      createdAt
      updatedAt
    }
  }
`;

export const ProjectsPage: React.FC = () => {
    const [showCreateForm, setShowCreateForm] = useState(false);
    const navigate = useNavigate();

    const { data, loading, error, refetch } = useQuery<{ myProjects: Project[] }>(GET_PROJECTS, {
        fetchPolicy: "network-only",
    });

    const getStatusBadge = (status: string) => {
        switch (status.toLowerCase()) {
            case "active":
                return { color: "bg-green-100 text-green-700", label: "Активний" };
            case "completed":
                return { color: "bg-blue-100 text-blue-700", label: "Виконано" };
            case "archived":
            case "on_hold":
                return { color: "bg-gray-100 text-gray-700", label: "В архіві" };
            default:
                return { color: "bg-gray-100 text-gray-700", label: status };
        }
    };

    const projects = data?.myProjects ?? [];

    return (
        <div className="container px-4 py-8 mx-auto max-w-7xl">
            <div className="flex items-center justify-between mb-8">
                <h1 className="text-3xl font-bold text-gray-900">Мої проєкти</h1>
                <button
                    onClick={() => setShowCreateForm(!showCreateForm)}
                    className={`px-4 py-2 text-white transition-colors rounded-lg ${
                        showCreateForm
                            ? "bg-gray-500 hover:bg-gray-600"
                            : "bg-blue-600 hover:bg-blue-700"
                    }`}
                >
                    {showCreateForm ? "Скасувати" : "+ Створити проєкт"}
                </button>
            </div>

            {showCreateForm && (
                <div className="mb-10">
                    <CreateProjectForm onCreated={() => {
                        refetch();
                        setShowCreateForm(false);
                    }} />
                </div>
            )}

            <h2 className="mb-4 text-xl font-semibold text-gray-700">Активні проєкти</h2>

            {loading && (
                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100 text-gray-500">
                    Завантаження проєктів...
                </div>
            )}

            {error && (
                <div className="mb-6 bg-red-50 border border-red-100 text-red-700 p-4 rounded-xl">
                    Помилка завантаження проєктів.
                </div>
            )}

            {!loading && !error && projects.length === 0 && (
                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100 text-gray-500">
                    У вас поки немає жодного проєкту.
                </div>
            )}

            {!loading && !error && projects.length > 0 && (
                <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
                    {projects.map((project) => {
                        const badge = getStatusBadge(project.status);

                        return (
                            <div
                                key={project.id}
                                className="p-6 transition-shadow bg-white border border-gray-100 shadow-sm rounded-xl hover:shadow-md"
                            >
                                <div
                                    onClick={() => navigate(`/projects/${project.id}`)}
                                    className="cursor-pointer"
                                >
                                    <div className="flex items-start justify-between mb-4">
                                        <h3 className="text-xl font-semibold text-gray-800">{project.title}</h3>
                                        <span className={`px-2 py-1 text-xs font-medium rounded-full ${badge.color}`}>
                                            {badge.label}
                                        </span>
                                    </div>

                                    <p className="mb-4 text-sm text-gray-600 line-clamp-2">
                                        {project.description?.trim()
                                            ? project.description
                                            : "Опис проєкту відсутній."}
                                    </p>

                                    <div className="flex items-center justify-between text-sm text-gray-500 border-t pt-4 border-gray-50">
                                        <span>
                                            Бюджет:{" "}
                                            {project.budgetCap != null
                                                ? `${project.budgetCap} год`
                                                : "Не вказано"}
                                        </span>
                                        <span className="font-medium text-blue-600 hover:text-blue-800">
                                            Відкрити →
                                        </span>
                                    </div>
                                </div>
                            </div>
                        );
                    })}
                </div>
            )}
        </div>
    );
};