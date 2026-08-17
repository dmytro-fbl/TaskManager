import React, { useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { gql } from "@apollo/client";
import { useQuery } from "@apollo/client/react";
import { 
    FiArrowLeft, 
    FiAlertTriangle, 
    FiInfo, 
    FiCheckSquare, 
    FiEdit3 
} from "react-icons/fi";

import { ProjectTasks } from "./ProjectTasks";
import { ProjectMembersTable } from "../../Project/components/ProjectMembersTable";
import { AddUserToProjectForm } from "../../Project/components/InviteExistingUserForm";
import { ProjectStatuses } from "./ProjectStatuses";
import ErrorMessage from "../../../components/ui/ErrorMessage";
import { getFriendlyErrorMessage } from "../../../utils/errorHandler";
import { EditProjectModal } from "../../Project/components/EditProjectModel";
import { GET_PROJECT_DETAILS } from "../../../graphql/queries/project/projectQuery";
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

    const tableRef = useRef<{
        refetchMembers: () => void;
    } | null>(null);

    const [activeTab, setActiveTab] = useState<"project" | "tasks">("project");

    const [isEditModalOpen, setIsEditModalOpen] = useState(false);
    const [selectedStatusId, setSelectedStatusId] = useState("");

    const { data, loading, error, refetch } = useQuery<{
        project: Project | null;
    }>(GET_PROJECT_DETAILS, {
        variables: { id: id ?? "" },
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
            day: 'numeric', month: 'long', year: 'numeric'
        });

        if (diffDays < 0) {
            return (
                <span className="px-2.5 py-1 text-xs font-semibold rounded-md bg-red-100 text-red-700 border border-red-200">
                    Прострочено ({formattedDate})
                </span>
            );
        } else if (diffDays <= 3) {
            return (
                <span className="px-2.5 py-1 text-xs font-semibold rounded-md bg-orange-100 text-orange-800 border border-orange-200" title={`Залишилось днів: ${diffDays}`}>
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
                <ErrorMessage message={getFriendlyErrorMessage(error as any) ?? "Помилка завантаження проєкту"} />
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
                className="inline-flex items-center gap-2 text-sm font-medium text-blue-600 transition-colors hover:text-blue-800"
            >
                <FiArrowLeft className="w-4 h-4" />
                <span>Назад до списку проєктів</span>
            </button>

            {project.isArchived && (
                <div className="bg-yellow-50 border-l-4 border-yellow-500 p-4 rounded-r-lg shadow-sm">
                    <div className="flex items-center">
                        <div className="flex-shrink-0">
                            <FiAlertTriangle className="h-5 w-5 text-yellow-500" />
                        </div>
                        <div className="ml-3">
                            <p className="text-sm text-yellow-700 font-medium">
                                Цей проєкт знаходиться в архіві. Він доступний лише для перегляду. Будь-які зміни заборонені.
                            </p>
                        </div>
                    </div>
                </div>
            )}

            <div className="flex flex-wrap items-center justify-between gap-4">
                <div className="flex items-center gap-3">
                    <h1 className="text-3xl font-bold text-text-main">{project.title}</h1>
                    <span className={`rounded-full px-3 py-1 text-xs font-semibold ${project.isArchived ? 'bg-gray-100 text-gray-600' : 'bg-green-100 text-green-700'}`}>
                        {project.isArchived ? 'Архівний' : project.status}
                    </span>
                </div>

                {!project.isArchived && (
                    <button
                        onClick={() => setIsEditModalOpen(true)}
                        className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-blue-700"
                    >
                        <FiEdit3 className="w-4 h-4" />
                        <span>Редагувати проєкт</span>
                    </button>
                )}
            </div>

            <div className="border-b border-gray-200">
                <nav className="-mb-px flex gap-6 text-sm font-medium">
                    <button
                        onClick={() => setActiveTab("project")}
                        className={`flex items-center gap-2 pb-3 border-b-2 font-medium transition-colors ${
                            activeTab === "project"
                                ? "border-blue-600 text-blue-600 font-semibold"
                                : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
                        }`}
                    >
                        <FiInfo className="w-4 h-4" />
                        <span>Інформація та Команда</span>
                    </button>

                    <button
                        onClick={() => setActiveTab("tasks")}
                        className={`flex items-center gap-2 pb-3 border-b-2 font-medium transition-colors ${
                            activeTab === "tasks"
                                ? "border-blue-600 text-blue-600 font-semibold"
                                : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
                        }`}
                    >
                        <FiCheckSquare className="w-4 h-4" />
                        <span>Завдання</span>
                    </button>
                </nav>
            </div>

            <div className="pt-2">
                {activeTab === "project" && (
                    <div className="space-y-8">
                        {/* Картка деталей проєкту */}
                        <div className="space-y-4 rounded-xl border border-gray-100 bg-bg-card p-6 shadow-sm">
                            <h2 className="text-xl font-bold text-text-main border-b border-gray-100 pb-3">
                                Деталі проєкту
                            </h2>

                            {!project.isArchived && (
                                <div className="flex flex-wrap items-center gap-3 pt-1">
                                    <label className="text-sm font-medium text-text-muted">
                                        Статус проєкту:
                                    </label>
                                    <ProjectStatuses
                                        projectId={id}
                                        selectedStatusId={selectedStatusId}
                                        onChange={setSelectedStatusId}
                                    />
                                </div>
                            )}

                            <div>
                                <label className="block text-xs font-semibold text-text-muted uppercase tracking-wider mb-1">
                                    Опис
                                </label>
                                <p className="text-text-muted text-base leading-relaxed">
                                    {project.description?.trim() ? project.description : "Опис проєкту відсутній."}
                                </p>
                            </div>

                            <div className="grid grid-cols-1 sm:grid-cols-2 gap-6 border-t pt-4 border-gray-100">
                                <div>
                                    <label className="block text-xs font-semibold text-text-muted uppercase tracking-wider mb-1">
                                        Бюджетний ліміт
                                    </label>
                                    <div className="text-base font-semibold text-text-main">
                                        {project.budgetCap != null ? `$${project.budgetCap}` : "Не обмежено"}
                                    </div>
                                </div>

                                <div>
                                    <label className="block text-xs font-semibold text-text-muted uppercase tracking-wider mb-1">
                                        Дедлайн проєкту
                                    </label>
                                    <div className="text-base">
                                        {renderDeadline(project.deadline)}
                                    </div>
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
                    </div>
                )}

                {activeTab === "tasks" && (
                    <div>
                        <ProjectTasks projectId={id} isArchived={project.isArchived} />
                    </div>
                )}
            </div>

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