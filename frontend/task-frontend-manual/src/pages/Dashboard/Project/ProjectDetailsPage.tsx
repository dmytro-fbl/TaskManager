import React, { useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { gql } from "@apollo/client";
import { useQuery } from "@apollo/client/react";

import { ProjectTasks } from "./Dashboard/Project/ProjectTasks";
import { ProjectMembersTable } from "./Dashboard/Project/ProjectMembersTable";
import { AddUserToProjectForm } from "./Dashboard/Project/InviteExistingUserForm";
import { ProjectStatuses } from "./Dashboard/Project/ProjectStatuses";

import ErrorMessage from "../components/ui/ErrorMessage";
import { getFriendlyErrorMessage } from "../utils/errorHandler";
import { EditProjectModal } from "../components/projects/EditProjectModel";

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

            <div className="space-y-4 rounded-xl border border-gray-100 bg-bg-card p-6 shadow-sm">
                <div className="flex flex-wrap items-center justify-between gap-4">
                    <div className="flex flex-wrap items-center gap-4">
                        <h1 className="text-2xl font-bold text-text-main">
                            {project.title}
                        </h1>

                        <span className="rounded-full bg-green-100 px-3 py-1 text-xs font-semibold text-green-700">
                            {project.status}
                        </span>
                    </div>

                    <button
                        onClick={() => setIsEditModalOpen(true)}
                        className="rounded-lg bg-blue-600 px-4 py-2 text-sm text-white transition-colors hover:bg-blue-700"
                    >
                        Редагувати
                    </button>
                </div>

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

                <p className="text-text-muted">
                    {project.description?.trim()
                        ? project.description
                        : "Опис проєкту відсутній."}
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

            <section className="space-y-6">
                <h2 className="text-xl font-bold text-text-main">
                    Команда проєкту
                </h2>

                <AddUserToProjectForm
                    projectId={id}
                    onUserAdded={() => {
                        tableRef.current?.refetchMembers();
                    }}
                />

                <ProjectMembersTable
                    projectId={id}
                    ref={tableRef}
                />
            </section>

            <ProjectTasks projectId={id} />

            <EditProjectModal
                isOpen={isEditModalOpen}
                onClose={() => setIsEditModalOpen(false)}
                onSuccess={() => refetch()}
                project={project}
            />
        </div>
    );
};