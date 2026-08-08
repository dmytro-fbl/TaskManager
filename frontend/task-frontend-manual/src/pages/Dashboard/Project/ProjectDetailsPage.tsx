import React, { useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { gql } from "@apollo/client";
import { useQuery } from "@apollo/client/react";

import { ProjectTasks } from "./ProjectTasks";
import { ProjectMembersTable } from "../../Project/components/ProjectMembersTable";
import { AddUserToProjectForm } from "../../Project/components/InviteExistingUserForm";
import { ProjectStatuses } from "./ProjectStatuses";
import ErrorMessage from "../../../components/ui/ErrorMessage";
import { getFriendlyErrorMessage } from "../../../utils/errorHandler";
import { EditProjectModal } from "../../Project/components/EditProjectModel";

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

const GET_PROJECT_DETAILS = gql`
  query GetProjectDetails($id: UUID!) {
    project(id: $id) {
      id
      title
      description
      budgetCap
      deadline
      status
      isArchived
      createdAt
    }
  }
`;

export const ProjectDetailsPage: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();

    const tableRef = useRef<{
        refetchMembers: () => void;
    } | null>(null);

    const [isEditModalOpen, setIsEditModalOpen] = useState(false);
    const [selectedStatusId, setSelectedStatusId] = useState("");

    const { data, loading, error, refetch } = useQuery<{
        project: Project | null;
    }>(GET_PROJECT_DETAILS, {
        variables: {
            id: id ?? "",
        },
        skip: !id,
        fetchPolicy: "network-only",
    });

    const renderDeadline = (deadlineString?: string | null) => {
        if (!deadlineString) return <span className="text-gray-500 font-normal">Не вказано</span>;

        const deadlineDate = new Date(deadlineString);
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        const diffTime = deadlineDate.getTime() - today.getTime();
        const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

        const formattedDate = deadlineDate.toLocaleDateString('uk-UA', {
            day: 'numeric',
            month: 'long',
            year: 'numeric'
        });

        if (diffDays < 0) {
            return (
                <span className="px-2 py-1 text-xs font-semibold rounded-md bg-red-100 text-red-700 border border-red-200">
                    Прострочено ({formattedDate})
                </span>
            );
        } else if (diffDays <= 3) {
            return (
                <span className="px-2 py-1 text-xs font-semibold rounded-md bg-orange-100 text-orange-800 border border-orange-200" title={`Залишилось днів: ${diffDays}`}>
                    Скоро ({formattedDate})
                </span>
            );
        } else {
            return <span className="text-text-main font-medium">{formattedDate}</span>;
        }
    };

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
                <div className="rounded-xl border border-gray-100 bg-white p-6 text-gray-500 shadow-sm">
                    Завантаження даних проєкту...
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="container mx-auto max-w-7xl px-4 py-8">
                <ErrorMessage
                    message={
                        getFriendlyErrorMessage(error as any) ??
                        "Помилка завантаження проєкту"
                    }
                />
            </div>
        );
    }

    const project = data?.project;

    if (!project) {
        return (
            <div className="container mx-auto max-w-7xl px-4 py-8">
                <div className="rounded-xl border border-gray-100 bg-white p-6 text-gray-500 shadow-sm">
                    Проєкт не знайдено або у вас немає доступу.
                </div>
            </div>
        );
    }

    return (
        <div className="container mx-auto max-w-7xl space-y-6 px-4 py-8">
            <button
                onClick={() => navigate("/projects")}
                className="text-sm font-medium text-blue-600 transition-colors hover:text-blue-800"
            >
                &rarr; Назад до списку проєктів
            </button>

            {/* БАНЕР АРХІВАЦІЇ */}
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
                                Цей проєкт знаходиться в архіві. Він доступний лише для перегляду. Будь-які зміни заборонені.
                            </p>
                        </div>
                    </div>
                </div>
            )}

            <div className="space-y-4 rounded-xl border border-gray-100 bg-bg-card p-6 shadow-sm">
                <div className="flex flex-wrap items-center justify-between gap-4">
                    <div className="flex flex-wrap items-center gap-4">
                        <h1 className="text-2xl font-bold text-text-main">
                            {project.title}
                        </h1>

                        <span className={`rounded-full px-3 py-1 text-xs font-semibold ${project.isArchived ? 'bg-gray-100 text-gray-600' : 'bg-green-100 text-green-700'}`}>
                            {project.isArchived ? 'Архівний' : project.status}
                        </span>
                    </div>

                    {!project.isArchived && (
                        <button
                            onClick={() => setIsEditModalOpen(true)}
                            className="rounded-lg bg-blue-600 px-4 py-2 text-sm text-white transition-colors hover:bg-blue-700"
                        >
                            Редагувати
                        </button>
                    )}
                </div>

                {!project.isArchived && (
                    <div className="flex flex-wrap items-center gap-3 border-t border-gray-100 pt-4">
                        <label
                            htmlFor="project-status"
                            className="text-sm font-medium text-text-muted"
                        >
                            Статус проєкту:
                        </label>

                        <ProjectStatuses
                            projectId={id}
                            selectedStatusId={selectedStatusId}
                            onChange={setSelectedStatusId}
                        />
                    </div>
                )}

                <p className="text-text-muted">
                    {project.description?.trim()
                        ? project.description
                        : "Опис проєкту відсутній."}
                </p>

                <div className="flex flex-col sm:flex-row sm:gap-8 text-sm text-text-main border-t pt-4 border-gray-100">
                    <div className="flex items-center">
                        <span className="text-text-muted mr-2 font-medium">Бюджетний ліміт:</span>
                        <span className="font-semibold">{project.budgetCap != null ? `$${project.budgetCap}` : "Не обмежено"}</span>
                    </div>
                    <div className="flex items-center mt-2 sm:mt-0">
                        <span className="text-text-muted mr-2 font-medium">Дедлайн:</span>
                        {renderDeadline(project.deadline)}
                    </div>
                </div>
            </div>

            <section className="space-y-6">
                <h2 className="text-xl font-bold text-text-main">
                    Команда проєкту
                </h2>

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
                    isArchived={project.isArchived}
                />
            </section>

            <ProjectTasks projectId={id} isArchived={project.isArchived} />

            {!project.isArchived && (
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