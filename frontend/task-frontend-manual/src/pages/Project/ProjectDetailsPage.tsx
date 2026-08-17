import React, { useRef, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { gql } from "@apollo/client";
import { useQuery } from "@apollo/client/react"
import { ProjectMembersTable } from "./components/ProjectMembersTable";
import { AddUserToProjectForm } from "./components/InviteExistingUserForm";
import ErrorMessage from "../../components/ui/ErrorMessage";
import { getFriendlyErrorMessage } from "../../utils/errorHandler";
import { EditProjectModal } from "./components/EditProjectModel";

import { GET_PROJECT_DETAILS } from "../../graphql/queries/project/projectQuery";

interface Project {
    id: string;
    title: string;
    description?: string | null;
    budgetCap?: number | null;
    deadline?: string | null;
    status: string;
    isArchived: boolean;
    createdAt: string;
}


export const ProjectDetailsPage: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();

    const tableRef = useRef<{ refetchMembers: () => void } | null>(null);

    const [isEditModalOpen, setIsEditModalOpen] = useState(false);

    const { data, loading, error, refetch } = useQuery<{ project: Project | null }>(GET_PROJECT_DETAILS, {
        variables: { id },
        skip: !id,
        fetchPolicy: "network-only",
    });

    if (!id) {
        return (
            <div className="container mx-auto max-w-7xl px-4 py-8">
                <ErrorMessage message="Ідентифікатор проєкту не вказано." />
            </div>
        );
    }

    if (loading) {
        return (
            <div className="container mx-auto max-w-7xl px-4 py-8">
                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100 text-gray-500">
                    Завантаження даних проєкту...
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="container mx-auto max-w-7xl px-4 py-8">
                <ErrorMessage message={getFriendlyErrorMessage(error as any) ?? "Помилка завантаження проєкту"} />
            </div>
        );
    }

    const project = data?.project;

    if (!project) {
        return (
            <div className="container mx-auto max-w-7xl px-4 py-8">
                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100 text-gray-500">
                    Проєкт не знайдено або у вас немає доступу.
                </div>
            </div>
        );
    }

    return (
        <div className="container mx-auto max-w-7xl px-4 py-8 space-y-6">

            {/* Кнопка назад */}
            <button
                onClick={() => navigate("/projects")}
                className="text-sm font-medium text-blue-600 hover:text-blue-800 transition-colors"
            >
                &rarr; Назад до списку проєктів
            </button>

            {project.isArchived && (
                <div className="bg-yellow-50 border-l-4 border-yellow-500 p-4 rounded-r-lg shadow-sm">
                    <div className="flex items-center">
                        <div className="flex-shrink-0">
                            <svg className="h-5 w-5 text-yellow-500" viewBox="0 0 20 20" fill="currentColor">
                                <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                            </svg>
                        </div>
                        <div className="ml-3">
                            <p className="text-sm text-yellow-700 font-medium">
                                Цей проєкт знаходиться в архіві. Він доступний лише для перегляду.
                            </p>
                        </div>
                    </div>
                </div>
            )}

            {/* Карточка з деталями проєкту */}
            <div className="bg-bg-card p-6 rounded-xl shadow-sm border border-gray-100 space-y-4">

                <div className="flex items-center justify-between">
                    <div className="flex items-center gap-4">
                        <h1 className="text-2xl font-bold text-text-main">{project.title}</h1>
                        <span className="px-3 py-1 text-xs font-semibold rounded-full bg-green-100 text-green-700">
                            {project.status}
                        </span>
                    </div>

                    {!project.isArchived && (
                        <button
                            onClick={() => setIsEditModalOpen(true)}
                            className="px-4 py-2 text-sm text-white bg-blue-600 rounded-lg hover:bg-blue-700 transition-colors"
                        >
                            Редагувати
                        </button>
                    )}
                </div>

                <p className="text-text-muted">
                    {project.description?.trim() ? project.description : "Опис проєкту відсутній."}
                </p>

                <div className="flex flex-col sm:flex-row sm:gap-8 text-sm text-text-main font-medium border-t pt-4 border-gray-100">
                    <div>
                        <span className="text-text-muted mr-1">Бюджетний ліміт:</span>
                        {project.budgetCap != null ? `$${project.budgetCap}` : "Не обмежено"}
                    </div>
                    <div>
                        <span className="text-text-muted mr-1">Дедлайн:</span>
                        {project.deadline
                            ? new Date(project.deadline).toLocaleDateString('uk-UA')
                            : "Не вказано"}
                    </div>
                </div>
            </div>

            {/* Секція: Команда проєкту */}
            <div className="space-y-6 pt-4">
                <h2 className="text-xl font-bold text-text-main">Команда проєкту</h2>

                {/* Форма додавання учасника */}
                {!project.isArchived && (
                    <AddUserToProjectForm
                        projectId={id}
                        onUserAdded={() => {
                            tableRef.current?.refetchMembers();
                        }}
                    />
                )}

                <ProjectMembersTable
                    projectId={id}
                    ref={tableRef}
                    // isArchived={project.isArchived}
                />
            </div>

            {project && !project.isArchived && (
                <EditProjectModal
                    isOpen={isEditModalOpen}
                    onClose={() => setIsEditModalOpen(false)}
                    onSuccess={() => refetch()}
                    project={project}
                />
            )}

        </div>
    );
};