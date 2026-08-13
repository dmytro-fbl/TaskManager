import React, { useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { gql } from "@apollo/client";
import { useQuery, useMutation } from "@apollo/client/react";
import { 
    FiArrowLeft, 
    FiClock, 
    FiAlignLeft, 
    FiCalendar, 
    FiUser, 
    FiDollarSign, 
    FiActivity, 
    FiFlag, 
    FiSave,
    FiList
} from "react-icons/fi";
import { getFriendlyErrorMessage } from "../../utils/errorHandler";

type Task = {
    id: string;
    projectId: string;
    authorId: string;
    assigneeId?: string | null;
    statusId: string;
    title: string;
    notes?: string | null;
    priority: string;
    dueDate?: string | null;
    estimatedBudget?: number | null;
};

type ProjectStatus = {
    id: string;
    name: string;
};

type Membership = {
    userId: string;
    user: {
        name: string;
    };
};

type Worklog = {
    id: string;
    userName: string;
    hoursSpent: number;
    logDate: string;
    comment?: string | null;
};

type ProjectDetailsResponse = {
    projectTasks: Task[];
    projectStatuses: ProjectStatus[];
    projectMemberships: Membership[];
};

type WorklogsResponse = {
    taskWorklogs: Worklog[];
};

const GET_PROJECT_DETAILS_FOR_TASK = gql`
    query GetProjectDetailsForTask($projectId: UUID!) {
        projectTasks(projectId: $projectId) {
            id projectId authorId assigneeId statusId title notes priority startDate dueDate estimatedBudget estimatedUnit createdAt updatedAt
        }
        projectStatuses(projectId: $projectId) { id name category color }
        projectMemberships(projectId: $projectId) { userId user { id name email } }
    }
`;

const GET_TASK_WORKLOGS = gql`
    query GetTaskWorklogs($taskId: UUID!) {
        taskWorklogs(taskId: $taskId) {
            id taskId userId userName hoursSpent logDate comment
        }
    }
`;

const LOG_WORK = gql`
    mutation LogWork($input: WorkLogInput!) {
        logWork(input: $input)
    }
`;

export const TaskDetailsPage: React.FC = () => {
    const { projectId, taskId } = useParams();
    const navigate = useNavigate();

    const [hoursSpent, setHoursSpent] = useState("");
    const [comment, setComment] = useState("");
    const [formError, setFormError] = useState("");

    const projectQuery = useQuery<ProjectDetailsResponse>(GET_PROJECT_DETAILS_FOR_TASK, {
        variables: { projectId },
        skip: !projectId,
    });

    const worklogsQuery = useQuery<WorklogsResponse>(GET_TASK_WORKLOGS, {
        variables: { taskId },
        skip: !taskId,
        fetchPolicy: "network-only",
    });

    const [logWork, { loading: logging }] = useMutation(LOG_WORK);

    if (projectQuery.loading || worklogsQuery.loading) {
        return <div className="p-8 text-center text-gray-500">Завантаження деталей таски...</div>;
    }

    if (projectQuery.error) {
        return <div className="p-8 text-center text-red-500">Помилка завантаження даних.</div>;
    }

    const task = projectQuery.data?.projectTasks?.find((t) => t.id === taskId);
    if (!task) {
        return <div className="p-8 text-center text-gray-500">Таску не знайдено.</div>;
    }

    const status = projectQuery.data?.projectStatuses?.find((s) => s.id === task.statusId);
    const assignee = projectQuery.data?.projectMemberships?.find((m) => m.userId === task.assigneeId)?.user;
    
    const worklogs = worklogsQuery.data?.taskWorklogs ?? [];
    const totalHoursLogged = worklogs.reduce((sum, item) => sum + Number(item.hoursSpent || 0), 0);

    const handleLogWork = async (e: React.FormEvent) => {
        e.preventDefault();
        setFormError("");

        const hours = Number(hoursSpent);
        if (isNaN(hours) || hours <= 0) {
            setFormError("Вкажи коректну кількість годин (більше 0).");
            return;
        }

        try {
            await logWork({
                variables: {
                    input: {
                        taskId: task.id,
                        hoursSpent: hours, 
                        comment: comment.trim() || null,
                    },
                },
            });

            setHoursSpent("");
            setComment("");
            await worklogsQuery.refetch();
        } catch (err: any) {
            setFormError(getFriendlyErrorMessage(err) ?? "Не вдалося зберегти час.");
        }
    };

    function formatDate(value?: string | null): string {
        if (!value) return "—";
        return new Date(value).toLocaleDateString("uk-UA");
    }

    function formatDateTime(value?: string | null): string {
        if (!value) return "—";
        return new Date(value).toLocaleString("uk-UA", {
            day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit",
        });
    }

    return (
        <div className="mx-auto max-w-5xl space-y-6 pb-12">
            <button 
                onClick={() => navigate(`/projects/${projectId}`)}
                className="flex items-center gap-2 text-sm font-medium text-gray-500 hover:text-blue-600 transition"
            >
                <FiArrowLeft size={16} /> Назад до проєкту
            </button>

            <div className="flex items-center justify-between rounded-2xl border border-gray-100 bg-white p-6 shadow-sm">
                <div className="space-y-2">
                    <span className="flex w-fit items-center gap-1.5 rounded-full bg-blue-100 px-3 py-1 text-xs font-semibold text-blue-700 uppercase">
                        <FiFlag size={12} /> {task.priority}
                    </span>
                    <h1 className="text-2xl font-bold text-text-main">{task.title}</h1>
                </div>
            </div>

            <div className="grid gap-6 md:grid-cols-3">
                <div className="space-y-6 md:col-span-2">
                    <div className="rounded-2xl border border-gray-100 bg-white p-6 shadow-sm">
                        <h3 className="flex items-center gap-2 mb-4 text-sm font-semibold text-gray-500 uppercase tracking-wider">
                            <FiAlignLeft size={16} /> Опис завдання
                        </h3>
                        <div className="rounded-xl bg-gray-50/50 p-4 text-sm text-text-main leading-relaxed min-h-[100px] border border-gray-100">
                            {task.notes?.trim() ? task.notes : "Опис відсутній."}
                        </div>
                    </div>

                    <div className="rounded-2xl border border-gray-100 bg-white p-6 shadow-sm">
                        <div className="flex items-center justify-between mb-6">
                            <h3 className="flex items-center gap-2 text-lg font-bold text-text-main">
                                <FiClock className="text-blue-600" size={20} /> Трекінг часу
                            </h3>
                            <span className="text-sm font-medium text-blue-600 bg-blue-50 px-3 py-1 rounded-full border border-blue-100">
                                Всього: {totalHoursLogged.toFixed(1)} год
                            </span>
                        </div>

                        <form onSubmit={handleLogWork} className="mb-8 rounded-xl border border-blue-100 bg-blue-50/30 p-5 space-y-4">
                            <h4 className="text-xs font-bold text-blue-900 uppercase">Записати витрачений час</h4>
                            {formError && <div className="text-xs text-red-600 bg-red-50 p-2 rounded-md">{formError}</div>}
                            
                            <div className="flex gap-4 items-start">
                                <div className="w-1/3">
                                    <label className="block text-xs font-medium text-gray-700 mb-1">Години</label>
                                    <input type="number" step="0.1" min="0.1" value={hoursSpent} onChange={(e) => setHoursSpent(e.target.value)} placeholder="Напр: 2.5" className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500" />
                                </div>
                                <div className="w-2/3">
                                    <label className="block text-xs font-medium text-gray-700 mb-1">Що було зроблено</label>
                                    <input type="text" value={comment} onChange={(e) => setComment(e.target.value)} placeholder="Короткий коментар..." className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500" />
                                </div>
                            </div>

                            <button type="submit" disabled={logging} className="flex w-full items-center justify-center gap-2 rounded-lg bg-blue-600 py-2.5 text-sm font-semibold text-white transition hover:bg-blue-700 disabled:opacity-50">
                                <FiSave size={16} /> {logging ? "Збереження..." : "Записати час"}
                            </button>
                        </form>

                        <div className="space-y-3">
                            <h4 className="flex items-center gap-1.5 text-xs font-bold text-gray-500 uppercase mb-3">
                                <FiList size={14} /> Історія роботи
                            </h4>
                            {worklogsQuery.loading ? (
                                <div className="text-sm text-gray-400">Завантаження...</div>
                            ) : worklogs.length === 0 ? (
                                <div className="text-sm text-gray-400 italic">Час ще не залоговано.</div>
                            ) : (
                                <div className="divide-y divide-gray-100 rounded-xl border border-gray-100">
                                    {worklogs.map((log) => (
                                        <div key={log.id} className="p-4 flex justify-between items-start hover:bg-gray-50/50 transition">
                                            <div className="space-y-1">
                                                <div className="font-semibold text-text-main">{log.userName}</div>
                                                <p className="text-sm text-gray-600">{log.comment || "Без коментаря."}</p>
                                                <div className="text-xs text-gray-400">{formatDateTime(log.logDate)}</div>
                                            </div>
                                            <span className="font-bold text-blue-700 bg-blue-50 px-2 py-1 rounded text-sm">
                                                +{Number(log.hoursSpent).toFixed(1)} год
                                            </span>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </div>
                    </div>
                </div>

                <div className="space-y-4 rounded-2xl bg-white p-6 border border-gray-100 shadow-sm h-fit">
                    <div>
                        <span className="flex items-center gap-1.5 text-[11px] font-semibold text-gray-400 uppercase mb-1">
                            <FiActivity size={12} /> Статус
                        </span>
                        <span className="text-sm font-semibold text-text-main pl-5">{status?.name ?? "—"}</span>
                    </div>
                    <div>
                        <span className="flex items-center gap-1.5 text-[11px] font-semibold text-gray-400 uppercase mb-1">
                            <FiUser size={12} /> Виконавець
                        </span>
                        <span className="text-sm font-semibold text-text-main pl-5">{assignee?.name ?? "Не призначено"}</span>
                    </div>
                    <div>
                        <span className="flex items-center gap-1.5 text-[11px] font-semibold text-gray-400 uppercase mb-1">
                            <FiCalendar size={12} /> Дедлайн
                        </span>
                        <span className="text-sm font-semibold text-text-main pl-5">{formatDate(task.dueDate)}</span>
                    </div>
                    <div>
                        <span className="flex items-center gap-1.5 text-[11px] font-semibold text-gray-400 uppercase mb-1">
                            <FiDollarSign size={12} /> Бюджет
                        </span>
                        <span className="text-sm font-bold text-green-600 pl-5">
                            {task.estimatedBudget ? `$${Number(task.estimatedBudget).toFixed(2)}` : "—"}
                        </span>
                    </div>
                </div>
            </div>
        </div>
    );
};