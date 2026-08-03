import React, { useRef, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { gql } from "@apollo/client";
import { useQuery } from "@apollo/client/react"
import { ProjectMembersTable } from "./Dashboard/Project/ProjectMembersTable";
import { AddUserToProjectForm } from "./Dashboard/Project/InviteExistingUserForm";
import ErrorMessage from "../components/ui/ErrorMessage";
import { getFriendlyErrorMessage } from "../utils/errorHandler";
import { EditProjectModal } from "../components/projects/EditProjectModel";

interface Project {
    id: string;
    title: string;
    description?: string | null;
    budgetCap?: number | null;
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
      status
      isArchived
      createdAt
    }
  }
`;

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

        {/* Карточка з деталями проєкту */}
        <div className="bg-bg-card p-6 rounded-xl shadow-sm border border-gray-100 space-y-4">
            
            {/* Шапка карточки: Заголовок + Статус + Кнопка */}
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-4">
                    <h1 className="text-2xl font-bold text-text-main">{project.title}</h1>
                    <span className="px-3 py-1 text-xs font-semibold rounded-full bg-green-100 text-green-700">
                        {project.status}
                    </span>
                </div>

                <button
                    onClick={() => setIsEditModalOpen(true)}
                    className="px-4 py-2 text-sm text-white bg-blue-600 rounded-lg hover:bg-blue-700 transition-colors"
                >
                    Редагувати
                </button>
            </div>

            {/* Тіло карточки: Опис */}
            <p className="text-text-muted">
                {project.description?.trim() ? project.description : "Опис проєкту відсутній."}
            </p>

            {/* Підвал карточки: Бюджет */}
            <div className="text-sm text-text-main font-medium border-t pt-4 border-gray-100">
                Бюджетний ліміт: {project.budgetCap != null ? `$${project.budgetCap}` : "Не обмежено"}
            </div>
        </div>

        {/* Секція: Команда проєкту */}
        <div className="space-y-6 pt-4">
            <h2 className="text-xl font-bold text-text-main">Команда проєкту</h2>
            
            {/* Форма додавання учасника */}
            <AddUserToProjectForm
                projectId={id}
                onUserAdded={() => {
                    tableRef.current?.refetchMembers();
                }}
            />

            {/* Таблиця учасників проєкту */}
            <ProjectMembersTable projectId={id} ref={tableRef} />
        </div>

        {/* Модалка редагування (рендериться поверх усього, якщо isEditModalOpen === true) */}
        {project && (
            <EditProjectModal
                isOpen={isEditModalOpen}
                onClose={() => setIsEditModalOpen(false)}
                onSuccess={() => refetch()} // Оновлюємо сторінку після редагування
                project={project}
            />
        )}
        
    </div> // Кінець головного контейнера
);
};