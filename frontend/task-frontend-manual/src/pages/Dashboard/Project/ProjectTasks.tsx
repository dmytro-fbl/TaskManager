import React, { FormEvent, useMemo, useState } from "react";
import { gql } from "@apollo/client";
import { useMutation, useQuery } from "@apollo/client/react";
import { getFriendlyErrorMessage } from "../../../utils/errorHandler";
import { useNavigate } from "react-router-dom";
import { FiTrash2, FiPlus, FiX } from "react-icons/fi";

const GET_PROJECT_TASKS_MULTIPLE = gql`
    query GetProjectTasks($projectId: UUID!) {
        projectTasks(projectId: $projectId) {
            id
            projectId
            authorId
            assigneeIds
            statusId
            title
            notes
            priority
            startDate
            dueDate
            createdAt
            updatedAt
        }
    }
`;

const GET_PROJECT_STATUSES_QUERY = gql`
    query GetProjectTaskStatuses($projectId: UUID!) {
        projectStatuses(projectId: $projectId) {
            id
            name
            category
            color
            sortOrder
            isFinal
        }
    }
`;

const GET_PROJECT_MEMBERSHIPS_QUERY = gql`
    query GetProjectTaskMembers($projectId: UUID!) {
        projectMemberships(projectId: $projectId) {
            id
            userId
            user {
                id
                name
                email
                isAdmin
            }
        }
    }
`;

const GET_PROJECT_ROLES_QUERY = gql`
    query GetProjectRoles($projectId: UUID!) {
        projectRoles(projectId: $projectId) {
            id
            name
        }
    }
`;

const CREATE_TASK_MUTATION = gql`
    mutation CreateTask($input: CreateTaskInput!) {
        createTask(input: $input) {
            id
            projectId
            assigneeIds
            statusId
            title
            notes
            priority
            startDate
            dueDate
            createdAt
            updatedAt
        }
    }
`;

const ASSIGN_USER_TO_TASK_MUTATION = gql`
    mutation AssignUserToTask(
        $taskId: UUID!
        $userId: UUID!
        $roleId: UUID
        $estimatedHours: Decimal!
    ) {
        assignUserToTask(
            taskId: $taskId
            userId: $userId
            roleId: $roleId
            estimatedHours: $estimatedHours
        )
    }
`;

const UPDATE_TASK_STATUS_MUTATION = gql`
    mutation UpdateTaskStatus(
        $taskId: UUID!
        $statusId: UUID!
    ) {
        updateTaskStatus(
            taskId: $taskId
            statusId: $statusId
        ) {
            id
            statusId
            updatedAt
        }
    }
`;

const DELETE_TASK = gql`
    mutation DeleteTask($taskId: UUID!) {
        deleteTask(taskId: $taskId)
    }
`;

const CREATE_PROJECT_ROLE_MUTATION = gql`
    mutation CreateProjectRole($projectId: UUID!, $name: String!) {
        createProjectRole(projectId: $projectId, name: $name) {
            id
            name
        }
    }
`;

type CreateProjectRoleResponse = {
    createProjectRole: { id: string; name: string; } | null;
};

type CreateProjectRoleVariables = {
    projectId: string;
    name: string;
};

type Task = {
    id: string;
    projectId: string;
    authorId: string;
    assigneeIds: string[];
    statusId: string;
    title: string;
    notes?: string | null;
    priority: string;
    startDate?: string | null;
    dueDate?: string | null;
    createdAt: string;
    updatedAt: string;
};

type ProjectStatus = { id: string; name: string; category: string; color?: string | null; sortOrder: number; isFinal: boolean; };
type Membership = { id: string; userId: string; user: { id: string; name: string; email: string; isAdmin: boolean; }; };
type ProjectRole = { id: string; name: string; };

type CreateTaskInput = {
    projectId: string;
    statusId: string;
    title: string;
    notes: string | null;
    priority: string;
    startDate: string | null;
    dueDate: string | null;
};

type CreateTaskResponse = { createTask: Task | null; };
type Props = { projectId: string; isArchived?: boolean; };

function toGraphQLDate(value: string): string | null {
    if (!value) return null;
    return `${value}T00:00:00.000Z`;
}

function formatDate(value?: string | null): string {
    if (!value) return "—";
    return new Date(value).toLocaleDateString("uk-UA");
}

export const ProjectTasks: React.FC<Props> = ({ projectId, isArchived = false }) => {
    const [title, setTitle] = useState("");
    const [notes, setNotes] = useState("");
    const [statusId, setStatusId] = useState("");
    const [priority, setPriority] = useState("medium");
    const [dueDate, setDueDate] = useState("");
    
    const [assignees, setAssignees] = useState<{ userId: string; roleId: string; hours: string }[]>([]);

    const [formError, setFormError] = useState("");
    const navigate = useNavigate();

    const tasksQuery = useQuery<{ projectTasks: Task[] }>(GET_PROJECT_TASKS_MULTIPLE, { variables: { projectId }, fetchPolicy: "network-only" });
    const statusesQuery = useQuery<{ projectStatuses: ProjectStatus[] }>(GET_PROJECT_STATUSES_QUERY, { variables: { projectId }, fetchPolicy: "network-only" });
    const membershipsQuery = useQuery<{ projectMemberships: Membership[] }>(GET_PROJECT_MEMBERSHIPS_QUERY, { variables: { projectId }, fetchPolicy: "network-only" });
    const rolesQuery = useQuery<{ projectRoles: ProjectRole[] }>(GET_PROJECT_ROLES_QUERY, { variables: { projectId }, fetchPolicy: "network-only" });

    const [createTask, { loading: creating }] = useMutation<CreateTaskResponse, { input: CreateTaskInput }>(CREATE_TASK_MUTATION);
    const [assignUserToTask] = useMutation(ASSIGN_USER_TO_TASK_MUTATION);
    const [updateTaskStatus] = useMutation(UPDATE_TASK_STATUS_MUTATION);
    const [deleteTaskMutation] = useMutation(DELETE_TASK);
    const [createRole, { loading: creatingRole }] = useMutation<CreateProjectRoleResponse, CreateProjectRoleVariables>(CREATE_PROJECT_ROLE_MUTATION, {
        refetchQueries: [{ query: GET_PROJECT_ROLES_QUERY, variables: { projectId } }]
    });

    const tasks = tasksQuery.data?.projectTasks ?? [];
    const statuses = statusesQuery.data?.projectStatuses ?? [];
    const memberships = membershipsQuery.data?.projectMemberships ?? [];
    const roles = rolesQuery.data?.projectRoles ?? [];

    const [isAddingRole, setIsAddingRole] = useState(false);
    const [newRoleName, setNewRoleName] = useState("");
    const [roleError, setRoleError] = useState("");

    const statusMap = useMemo(() => new Map(statuses.map((status) => [status.id, status])), [statuses]);
    const memberMap = useMemo(() => new Map(memberships.map((membership) => [membership.userId, membership.user])), [memberships]);

    const handleAddAssignee = () => {
        setAssignees([...assignees, { userId: "", roleId: "", hours: "" }]);
    };

    const handleRemoveAssignee = (index: number) => {
        setAssignees(assignees.filter((_, i) => i !== index));
    };

    const handleAssigneeChange = (index: number, field: "userId" | "roleId" | "hours", value: string) => {
        const updated = [...assignees];
        updated[index][field] = value;
        setAssignees(updated);
    };

    const handleCreateTask = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setFormError("");

        const selectedStatusId = statusId || statuses[0]?.id || "";

        if (!title.trim()) {
            setFormError("Вкажи назву таски.");
            return;
        }
        if (!selectedStatusId) {
            setFormError("У проєкту немає статусів.");
            return;
        }

        try {
            const result = await createTask({
                variables: {
                    input: {
                        projectId,
                        statusId: selectedStatusId,
                        title: title.trim(),
                        notes: notes.trim() || null,
                        priority,
                        startDate: null,
                        dueDate: toGraphQLDate(dueDate),
                    },
                },
            });

            const createdTaskId = result.data?.createTask?.id;

            const validAssignees = assignees.filter(a => a.userId);

            if (createdTaskId && validAssignees.length > 0) {
                await Promise.all(
                    validAssignees.map((assignee) =>
                        assignUserToTask({
                            variables: {
                                taskId: createdTaskId,
                                userId: assignee.userId,
                                roleId: assignee.roleId || null,
                                estimatedHours: Number(assignee.hours) > 0 ? Number(assignee.hours) : 1,
                            },
                        })
                    )
                );
            }

            setTitle("");
            setNotes("");
            setDueDate("");
            setAssignees([]);
            setStatusId("");

            await tasksQuery.refetch();
        } catch (error: any) {
            setFormError(getFriendlyErrorMessage(error) ?? "Не вдалося створити таску.");
        }
    };

    const handleStatusChange = async (taskId: string, nextStatusId: string) => {
        try {
            await updateTaskStatus({ variables: { taskId, statusId: nextStatusId } });
            await tasksQuery.refetch();
        } catch (error: any) {
            alert(getFriendlyErrorMessage(error) ?? "Не вдалося змінити статус таски.");
        }
    };

    const handleCreateCustomRole = async (e: React.MouseEvent) => {
        e.preventDefault();
        setRoleError("");
        if (!newRoleName.trim()) return;

        try {
            await createRole({ variables: { projectId, name: newRoleName.trim() } });
            setNewRoleName("");
            setIsAddingRole(false);
        } catch (err: any) {
            setRoleError(err.message || "Не вдалося створити роль.");
        }
    };

    const handleDeleteTask = async (taskId: string) => {
        if (!window.confirm("Ви впевнені, що хочете видалити цю таску? Дія незворотня.")) return;
        try {
            await deleteTaskMutation({
                variables: { taskId },
                update: (cache) => {
                    cache.evict({ id: cache.identify({ __typename: 'TaskItem', id: taskId }) });
                    cache.gc();
                }
            });
            await tasksQuery.refetch();
        } catch (error: any) {
            alert(error.message || "Сталася помилка при видаленні таски.");
        }
    };

    if (tasksQuery.loading || statusesQuery.loading || membershipsQuery.loading || rolesQuery.loading) {
        return <div className="rounded-xl border border-gray-100 bg-bg-card p-6 shadow-sm">Завантаження даних...</div>;
    }

    if (tasksQuery.error || statusesQuery.error || membershipsQuery.error || rolesQuery.error) {
        const err = tasksQuery.error || statusesQuery.error || membershipsQuery.error || rolesQuery.error;
        return <div className="rounded-xl border border-red-100 bg-red-50 p-6 text-red-700">{getFriendlyErrorMessage(err as any) ?? "Не вдалося завантажити дані."}</div>;
    }

    return (
        <section className="space-y-6">
            {!isArchived && (
                <div className="rounded-xl border border-gray-100 bg-bg-card p-6 shadow-sm">
                    <h2 className="mb-5 text-xl font-bold text-text-main">Створити таску</h2>

                    {formError && (
                        <div className="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-700">{formError}</div>
                    )}

                    <form onSubmit={handleCreateTask} className="grid gap-4 md:grid-cols-2">
                        <div className="md:col-span-2">
                            <label className="mb-1 block text-sm font-medium text-text-main">Назва</label>
                            <input value={title} onChange={(e) => setTitle(e.target.value)} className="w-full rounded-lg border border-gray-300 px-3 py-2 outline-none focus:border-blue-500" placeholder="Наприклад: Додати авторизацію" />
                        </div>

                        <div className="md:col-span-2">
                            <label className="mb-1 block text-sm font-medium text-text-main">Опис</label>
                            <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={3} className="w-full rounded-lg border border-gray-300 px-3 py-2 outline-none focus:border-blue-500" placeholder="Опис таски" />
                        </div>

                        <div>
                            <label className="mb-1 block text-sm font-medium text-text-main">Статус</label>
                            <select value={statusId || statuses[0]?.id || ""} onChange={(e) => setStatusId(e.target.value)} className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 outline-none focus:border-blue-500">
                                {statuses.map((status) => <option key={status.id} value={status.id}>{status.name}</option>)}
                            </select>
                        </div>

                        <div>
                            <label className="mb-1 block text-sm font-medium text-text-main">Пріоритет</label>
                            <select value={priority} onChange={(e) => setPriority(e.target.value)} className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 outline-none focus:border-blue-500">
                                <option value="low">Низький</option>
                                <option value="medium">Середній</option>
                                <option value="high">Високий</option>
                                <option value="critical">Критичний</option>
                            </select>
                        </div>

                        <div>
                            <label className="mb-1 block text-sm font-medium text-text-main">Дедлайн</label>
                            <input type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} className="w-full rounded-lg border border-gray-300 px-3 py-2 outline-none focus:border-blue-500" />
                        </div>

                        <div className="md:col-span-2 mt-4 border-t border-gray-100 pt-4">
                            <div className="flex items-center justify-between mb-3">
                                <label className="block text-sm font-bold text-text-main">Виконавці та ролі</label>
                                <button type="button" onClick={handleAddAssignee} className="flex items-center gap-1 text-sm font-medium text-blue-600 hover:text-blue-700">
                                    <FiPlus /> Додати виконавця
                                </button>
                            </div>

                            <div className="space-y-3">
                                {assignees.map((assignee, index) => (
                                    <div key={index} className="flex flex-col sm:flex-row items-start sm:items-center gap-2 bg-gray-50 p-3 rounded-xl border border-gray-200">
                                        <select
                                            value={assignee.userId}
                                            onChange={(e) => handleAssigneeChange(index, "userId", e.target.value)}
                                            className="flex-1 rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm outline-none w-full sm:w-auto"
                                        >
                                            <option value="">Оберіть людину...</option>
                                            {memberships.map((m) => <option key={m.userId} value={m.userId}>{m.user.name}</option>)}
                                        </select>

                                        <select
                                            value={assignee.roleId}
                                            onChange={(e) => handleAssigneeChange(index, "roleId", e.target.value)}
                                            className="flex-1 rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm outline-none w-full sm:w-auto"
                                        >
                                            <option value="">Без ролі</option>
                                            {roles.map((role) => <option key={role.id} value={role.id}>{role.name}</option>)}
                                        </select>

                                        <input
                                            type="number"
                                            min="0.1"
                                            step="0.1"
                                            value={assignee.hours}
                                            onChange={(e) => handleAssigneeChange(index, "hours", e.target.value)}
                                            placeholder="Години"
                                            className="w-full sm:w-24 rounded-lg border border-gray-300 px-3 py-2 text-sm outline-none"
                                        />

                                        <button type="button" onClick={() => handleRemoveAssignee(index)} className="p-2 text-red-500 hover:bg-red-100 rounded-lg">
                                            <FiX size={18} />
                                        </button>
                                    </div>
                                ))}
                                {assignees.length === 0 && (
                                    <div className="text-sm text-gray-400 italic text-center py-4">Ще нікого не призначено</div>
                                )}
                            </div>
                        </div>

                        <div className="md:col-span-2">
                            {!isAddingRole ? (
                                <button type="button" onClick={() => setIsAddingRole(true)} className="flex items-center gap-1 text-xs font-medium text-blue-600 transition hover:text-blue-700 mt-2">
                                    <FiPlus /> Створити кастомну роль
                                </button>
                            ) : (
                                <div className="mt-2 flex flex-col gap-2 p-3 bg-blue-50 rounded-lg border border-blue-100 w-fit">
                                    <div className="flex gap-2">
                                        <input type="text" value={newRoleName} onChange={(e) => setNewRoleName(e.target.value)} placeholder="Назва нової ролі" className="rounded-lg border border-gray-300 px-3 py-1.5 text-sm outline-none" />
                                        <button type="button" onClick={handleCreateCustomRole} disabled={creatingRole || !newRoleName.trim()} className="rounded-lg bg-blue-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50">
                                            {creatingRole ? "..." : "Зберегти"}
                                        </button>
                                        <button type="button" onClick={() => { setIsAddingRole(false); setRoleError(""); setNewRoleName(""); }} className="rounded-lg border border-gray-300 bg-white px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-50">
                                            Скасувати
                                        </button>
                                    </div>
                                    {roleError && <span className="text-xs text-red-600">{roleError}</span>}
                                </div>
                            )}
                        </div>

                        <div className="md:col-span-2 mt-2 border-t border-gray-100 pt-4">
                            <button type="submit" disabled={creating} className="w-full rounded-lg bg-blue-600 px-5 py-3 text-sm font-bold text-white transition hover:bg-blue-700 disabled:opacity-50">
                                {creating ? "Створення..." : "Створити таску"}
                            </button>
                        </div>
                    </form>
                </div>
            )}

            <div className="rounded-xl border border-gray-100 bg-bg-card shadow-sm">
                <div className="border-b border-gray-100 p-6">
                    <h2 className="text-xl font-bold text-text-main">Таски проєкту</h2>
                </div>

                {tasks.length === 0 ? (
                    <div className="p-6 text-text-muted">Тасків ще немає.</div>
                ) : (
                    <div className="divide-y divide-gray-100">
                        {tasks.map((task) => {
                            const status = statusMap.get(task.statusId);
                            const assigneesNames = task.assigneeIds.map((id) => memberMap.get(id)?.name).filter(Boolean);

                            return (
                                <article key={task.id} onClick={() => navigate(`/projects/${projectId}/tasks/${task.id}`)} className="space-y-3 p-6 hover:bg-gray-50/50 transition-colors relative group cursor-pointer">
                                    <div className="flex flex-wrap items-start justify-between gap-4">
                                        <div className="flex-1">
                                            <h3 className="text-lg font-semibold text-text-main">{task.title}</h3>
                                            <p className="mt-1 text-sm text-text-muted line-clamp-2">{task.notes?.trim() ? task.notes : "Опис відсутній."}</p>
                                        </div>

                                        <div className="flex flex-col items-end gap-3 w-full sm:w-auto">
                                            <select
                                                value={task.statusId}
                                                onChange={(e) => handleStatusChange(task.id, e.target.value)}
                                                onClick={(e) => e.stopPropagation()}
                                                disabled={isArchived}
                                                className={`min-w-[160px] rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500 ${isArchived ? 'bg-gray-100 text-gray-500 cursor-not-allowed' : ''}`}
                                                style={{ borderColor: status?.color ?? undefined }}
                                            >
                                                {statuses.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
                                            </select>

                                            {!isArchived && (
                                                <button onClick={async (e) => { e.stopPropagation(); await handleDeleteTask(task.id); }} className="flex w-full items-center justify-center gap-2 rounded-lg border border-red-200 bg-white px-4 py-2 text-sm font-medium text-red-600 transition-colors hover:bg-red-50 hover:border-red-300">
                                                    <FiTrash2 className="h-4 w-4" /> Видалити
                                                </button>
                                            )}
                                        </div>
                                    </div>

                                    <div className="grid gap-3 text-sm text-text-muted md:grid-cols-4">
                                        <div><span className="font-medium text-text-main">Статус:</span> {status?.name ?? "—"}</div>
                                        <div className="col-span-2"><span className="font-medium text-text-main">Виконавці:</span> {assigneesNames.length > 0 ? assigneesNames.join(", ") : "Не призначено"}</div>
                                        <div><span className="font-medium text-text-main">Дедлайн:</span> {formatDate(task.dueDate)}</div>
                                    </div>

                                    <div className="text-xs text-text-muted">Пріоритет: <span className="uppercase font-medium">{task.priority}</span></div>
                                </article>
                            );
                        })}
                    </div>
                )}
            </div>
        </section>
    );
};