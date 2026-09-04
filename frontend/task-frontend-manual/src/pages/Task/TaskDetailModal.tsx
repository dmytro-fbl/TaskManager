import React, {useState} from "react";
import {useParams, useNavigate} from "react-router-dom";
import {gql} from "@apollo/client";
import {useQuery, useMutation} from "@apollo/client/react";
import {
    FiArrowLeft,
    FiClock,
    FiAlignLeft,
    FiCalendar,
    FiUser,
    FiActivity,
    FiFlag,
    FiSave,
    FiList,
    FiPlus,
    FiTrash2,
    FiEdit2,
    FiCheck,
    FiX
} from "react-icons/fi";
import {getFriendlyErrorMessage} from "../../utils/errorHandler";

import {GET_PROJECT_DETAILS_FOR_TASK} from "../../graphql/queries/project/projectQuery";
import {GET_TASK_WORKLOGS, LOG_WORK, GET_TASK_ASSIGNMENTS} from "../../graphql/queries/task/taskQueries";
import {ASSIGN_USER_TO_TASK} from "../../graphql/mutations/taskmut/taskMutation";

import {TaskCommentsSection} from "./TaskComment/TaskCommentsSection";

interface MeResponse {
    me: {
        id: string;
        isAdmin: boolean;
    } | null;
}

const GET_ME_QUERY = gql`
  query GetMeForTaskDetails {
    me {
      id
      isAdmin
    }
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

const UPDATE_PROJECT_ROLE_MUTATION = gql`
    mutation UpdateProjectRole($projectId: UUID!, $roleId: UUID!, $newName: String!) {
        updateProjectrole(projectId: $projectId, roleId: $roleId, newName: $newName) {
            id
            name
        }
    }
`;

const DELETE_PROJECT_ROLE_MUTATION = gql`
    mutation DeleteProjectRole($projectId: UUID!, $roleId: UUID!) {
        deleteProjectRole(projectId: $projectId, roleId: $roleId)
    }
`;

type CreateProjectRoleResponse = {
    createProjectRole: { id: string; name: string; } | null;
};

type CreateProjectRoleVariables = {
    projectId: string;
    name: string;
};

type UpdateProjectRoleResponse = {
    updateProjectrole: { id: string; name: string; } | null;
};

type UpdateProjectRoleVariables = {
    projectId: string;
    roleId: string;
    newName: string;
};

type DeleteProjectRoleResponse = {
    deleteProjectRole: boolean;
};

type DeleteProjectRoleVariables = {
    projectId: string;
    roleId: string;
};

type Task = {
    id: string;
    projectId: string;
    authorId: string;
    statusId: string;
    title: string;
    notes?: string | null;
    priority: string;
    dueDate?: string | null;
};

type ProjectStatus = { id: string; name: string; };
type ProjectRole = { id: string; name: string; };
type Membership = { userId: string; user: { name: string; }; };

type TaskAssignment = {
    id: string;
    userId: string;
    roleId?: string | null;
    estimatedHours: number;
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
    projectRoles: ProjectRole[];
};

type WorklogsResponse = {
    taskWorklogs: Worklog[];
};

type AssignmentsResponse = {
    taskAssignments: TaskAssignment[];
};

export const TaskDetailsPage: React.FC = () => {
    const {projectId, taskId} = useParams();
    const navigate = useNavigate();

    const [hoursSpent, setHoursSpent] = useState("");
    const [comment, setComment] = useState("");
    const [formError, setFormError] = useState("");

    const [isAddMemberOpen, setIsAddMemberOpen] = useState(false);
    const [selectedUserId, setSelectedUserId] = useState("");
    const [selectedRoleId, setSelectedRoleId] = useState("");
    const [selectedHours, setSelectedHours] = useState("");

    const [isAddingRole, setIsAddingRole] = useState(false);
    const [newRoleName, setNewRoleName] = useState("");
    const [editingRoleId, setEditingRoleId] = useState<string | null>(null);
    const [editingRoleName, setEditingRoleName] = useState("");
    const [roleError, setRoleError] = useState("");

    const projectQuery = useQuery<ProjectDetailsResponse>(GET_PROJECT_DETAILS_FOR_TASK, {
        variables: {projectId},
        skip: !projectId,
    });

    const worklogsQuery = useQuery<WorklogsResponse>(GET_TASK_WORKLOGS, {
        variables: {taskId},
        skip: !taskId,
        fetchPolicy: "network-only",
    });

    const assignmentsQuery = useQuery<AssignmentsResponse>(GET_TASK_ASSIGNMENTS, {
        variables: {taskId},
        skip: !taskId,
        fetchPolicy: "network-only",
    });

    const {data: meData} = useQuery<MeResponse>(GET_ME_QUERY, {
        fetchPolicy: "network-only"
    });

    const [logWork, {loading: logging}] = useMutation(LOG_WORK);

    const [assignUserToTask, {loading: assigning}] = useMutation(ASSIGN_USER_TO_TASK, {
        onCompleted: () => {
            setIsAddMemberOpen(false);
            setSelectedUserId("");
            setSelectedRoleId("");
            setSelectedHours("");
            assignmentsQuery.refetch();
        },
        onError: (err) => {
            alert(getFriendlyErrorMessage(err) ?? "Не вдалося додати учасника");
        }
    });

    const [createRole, { loading: creatingRole }] = useMutation<CreateProjectRoleResponse, CreateProjectRoleVariables>(CREATE_PROJECT_ROLE_MUTATION, {
        refetchQueries: [{ query: GET_PROJECT_DETAILS_FOR_TASK, variables: { projectId } }]
    });
    const [updateRole, { loading: updatingRole }] = useMutation<UpdateProjectRoleResponse, UpdateProjectRoleVariables>(UPDATE_PROJECT_ROLE_MUTATION, {
        refetchQueries: [{ query: GET_PROJECT_DETAILS_FOR_TASK, variables: { projectId } }]
    });
    const [deleteRole, { loading: deletingRole }] = useMutation<DeleteProjectRoleResponse, DeleteProjectRoleVariables>(DELETE_PROJECT_ROLE_MUTATION, {
        refetchQueries: [{ query: GET_PROJECT_DETAILS_FOR_TASK, variables: { projectId } }]
    });

    if (projectQuery.loading || worklogsQuery.loading || assignmentsQuery.loading) {
        return <div className="p-8 text-center text-gray-500">Завантаження деталей таски...</div>;
    }

    if (projectQuery.error || assignmentsQuery.error) {
        return <div className="p-8 text-center text-red-500">Помилка завантаження даних.</div>;
    }

    const task = projectQuery.data?.projectTasks?.find((t) => t.id === taskId);
    if (!task) {
        return <div className="p-8 text-center text-gray-500">Таску не знайдено.</div>;
    }

    const status = projectQuery.data?.projectStatuses?.find((s) => s.id === task.statusId);

    const assignments = assignmentsQuery.data?.taskAssignments ?? [];
    const memberships = projectQuery.data?.projectMemberships ?? [];
    const roles = projectQuery.data?.projectRoles ?? [];

    const worklogs = worklogsQuery.data?.taskWorklogs ?? [];
    const totalHoursLogged = worklogs.reduce((sum, item) => sum + Number(item.hoursSpent || 0), 0);

    const currentUserId = meData?.me?.id;
    const isAdmin = meData?.me?.isAdmin ?? false;

    const isMember = projectQuery.data?.projectMemberships?.some(
        (m) => m.userId === currentUserId
    );

    const canLogWork = isAdmin || isMember;

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

    const handleAddMember = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!selectedUserId || !selectedHours) {
            alert("Оберіть користувача і вкажіть години");
            return;
        }

        await assignUserToTask({
            variables: {
                taskId: task.id,
                userId: selectedUserId,
                estimatedHours: Number(selectedHours),
                roleId: selectedRoleId || null,
            },
        });
    };

    const handleCreateCustomRole = async (e: React.MouseEvent) => {
        e.preventDefault();
        setRoleError("");
        if (!projectId || !newRoleName.trim()) return;

        try {
            const res = await createRole({ variables: { projectId, name: newRoleName.trim() } });
            const createdId = res.data?.createProjectRole?.id;
            if (createdId) {
                setSelectedRoleId(createdId);
            }
            setNewRoleName("");
            setIsAddingRole(false);
        } catch (err: any) {
            setRoleError(getFriendlyErrorMessage(err) ?? err.message ?? "Не вдалося створити роль.");
        }
    };

    const handleStartEditRole = (role: ProjectRole) => {
        setIsAddingRole(false);
        setEditingRoleId(role.id);
        setEditingRoleName(role.name);
        setRoleError("");
    };

    const handleCancelEditRole = () => {
        setEditingRoleId(null);
        setEditingRoleName("");
        setRoleError("");
    };

    const handleUpdateCustomRole = async (roleId: string) => {
        setRoleError("");
        if (!projectId) return;
        const trimmed = editingRoleName.trim();
        if (!trimmed) {
            setRoleError("Назва ролі не може бути порожньою.");
            return;
        }
        if (trimmed.length < 2) {
            setRoleError("Назва ролі повинна містити щонайменше 2 символи.");
            return;
        }

        try {
            await updateRole({
                variables: {
                    projectId,
                    roleId,
                    newName: trimmed,
                }
            });
            setEditingRoleId(null);
            setEditingRoleName("");
        } catch (err: any) {
            setRoleError(getFriendlyErrorMessage(err) ?? err.message ?? "Не вдалося оновити роль.");
        }
    };

    const handleDeleteCustomRole = async (role: ProjectRole) => {
        setRoleError("");
        if (!projectId) return;
        if (!window.confirm(`Ви дійсно бажаєте видалити роль "${role.name}"?`)) {
            return;
        }

        try {
            await deleteRole({
                variables: {
                    projectId,
                    roleId: role.id,
                }
            });
            if (selectedRoleId === role.id) {
                setSelectedRoleId("");
            }
            if (editingRoleId === role.id) {
                setEditingRoleId(null);
                setEditingRoleName("");
            }
        } catch (err: any) {
            setRoleError(getFriendlyErrorMessage(err) ?? err.message ?? "Не вдалося видалити роль.");
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
                <FiArrowLeft size={16}/> Назад до проєкту
            </button>

            <div
                className="flex items-center justify-between rounded-2xl border border-gray-100 bg-white p-6 shadow-sm">
                <div className="space-y-2">
                    <span
                        className="flex w-fit items-center gap-1.5 rounded-full bg-blue-100 px-3 py-1 text-xs font-semibold text-blue-700 uppercase">
                        <FiFlag size={12}/> {task.priority}
                    </span>
                    <h1 className="text-2xl font-bold text-[#1f2937]">{task.title}</h1>
                </div>
            </div>

            <div className="grid gap-6 md:grid-cols-3">
                {/* ЛІВА ОСНОВНА КОЛОНКА */}
                <div className="space-y-6 md:col-span-2">
                    <div className="rounded-2xl border border-gray-100 bg-white p-6 shadow-sm">
                        <h3 className="flex items-center gap-2 mb-4 text-sm font-semibold text-gray-500 uppercase tracking-wider">
                            <FiAlignLeft size={16}/> Опис завдання
                        </h3>
                        <div
                            className="rounded-xl bg-gray-50/50 p-4 text-sm text-[#374151] leading-relaxed min-h-[100px] border border-gray-100">
                            {task.notes?.trim() ? task.notes : "Опис відсутній."}
                        </div>
                    </div>

                    <div className="rounded-2xl border border-gray-100 bg-white p-6 shadow-sm">
                        <div className="flex items-center justify-between mb-6">
                            <h3 className="flex items-center gap-2 text-lg font-bold text-[#1f2937]">
                                <FiClock className="text-blue-600" size={20}/> Трекінг часу
                            </h3>
                            <span
                                className="text-sm font-medium text-blue-600 bg-blue-50 px-3 py-1 rounded-full border border-blue-100">
                                Всього: {totalHoursLogged.toFixed(1)} год
                            </span>
                        </div>

                        {canLogWork && (
                            <form onSubmit={handleLogWork}
                                  className="mb-8 rounded-xl border border-blue-100 bg-blue-50/30 p-5 space-y-4">
                                <h4 className="text-xs font-bold text-blue-900 uppercase">Записати витрачений час</h4>
                                {formError &&
                                    <div className="text-xs text-red-600 bg-red-50 p-2 rounded-md">{formError}</div>}

                                <div className="flex gap-4 items-start">
                                    <div className="w-1/3">
                                        <label className="block text-xs font-medium text-gray-700 mb-1">Години</label>
                                        <input type="number" step="0.1" min="0.1" value={hoursSpent}
                                               onChange={(e) => setHoursSpent(e.target.value)} placeholder="Напр: 2.5"
                                               className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500"/>
                                    </div>
                                    <div className="w-2/3">
                                        <label className="block text-xs font-medium text-gray-700 mb-1">Що було
                                            зроблено</label>
                                        <input type="text" value={comment} onChange={(e) => setComment(e.target.value)}
                                               placeholder="Короткий коментар..."
                                               className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500"/>
                                    </div>
                                </div>

                                <button type="submit" disabled={logging}
                                        className="flex w-full items-center justify-center gap-2 rounded-lg bg-blue-600 py-2.5 text-sm font-semibold text-white transition hover:bg-blue-700 disabled:opacity-50">
                                    <FiSave size={16}/> {logging ? "Збереження..." : "Записати час"}
                                </button>
                            </form>
                        )}

                        <div className="space-y-3">
                            <h4 className="flex items-center gap-1.5 text-xs font-bold text-gray-500 uppercase mb-3">
                                <FiList size={14}/> Історія роботи
                            </h4>
                            {worklogsQuery.loading ? (
                                <div className="text-sm text-gray-400">Завантаження...</div>
                            ) : worklogs.length === 0 ? (
                                <div className="text-sm text-gray-400 italic">Час ще не залоговано.</div>
                            ) : (
                                <div className="divide-y divide-gray-100 rounded-xl border border-gray-100">
                                    {worklogs.map((log) => (
                                        <div key={log.id}
                                             className="p-4 flex justify-between items-start hover:bg-gray-50/50 transition">
                                            <div className="space-y-1">
                                                <div className="font-semibold text-[#1f2937]">{log.userName}</div>
                                                <p className="text-sm text-gray-600">{log.comment || "Без коментаря."}</p>
                                                <div
                                                    className="text-xs text-gray-400">{formatDateTime(log.logDate)}</div>
                                            </div>
                                            <span
                                                className="font-bold text-blue-700 bg-blue-50 px-2 py-1 rounded text-sm">
                                                +{Number(log.hoursSpent).toFixed(1)} год
                                            </span>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </div>
                    </div>

                    <TaskCommentsSection
                        taskId={task.id}
                        memberships={memberships}
                        currentUserId={currentUserId}
                        canComment={canLogWork}
                    />
                </div>

                {/* ПРАВА БІЧНА КОЛОНКА */}
                <div className="space-y-4 rounded-2xl bg-white p-6 border border-gray-100 shadow-sm h-fit">
                    <div>
                        <span
                            className="flex items-center gap-1.5 text-[11px] font-semibold text-gray-400 uppercase mb-1">
                            <FiActivity size={12}/> Статус
                        </span>
                        <span className="text-sm font-semibold text-[#1f2937] pl-5">{status?.name ?? "—"}</span>
                    </div>

                    <div>
                        <span
                            className="flex items-center gap-1.5 text-[11px] font-semibold text-gray-400 uppercase mb-1">
                            <FiCalendar size={12}/> Дедлайн
                        </span>
                        <span className="text-sm font-semibold text-[#1f2937] pl-5">{formatDate(task.dueDate)}</span>
                    </div>

                    {/* БЛОК ЗІ СПИСКОМ ВСІХ ВИКОНАВЦІВ ТА РОЛЕЙ */}
                    <div className="pt-2 border-t border-gray-100">
                        <span
                            className="flex items-center gap-1.5 text-[11px] font-semibold text-gray-400 uppercase mb-3">
                            <FiUser size={12}/> Команда завдання
                        </span>

                        {canLogWork && (
                            <button
                                onClick={() => setIsAddMemberOpen(true)}
                                className="w-full mb-3 flex items-center justify-center gap-2 px-3 py-2 text-xs font-semibold text-white bg-blue-600 rounded-lg hover:bg-blue-700 transition"
                            >
                                <FiPlus size={14}/> Додати учасника
                            </button>
                        )}

                        <div className="space-y-2">
                            {assignments.length > 0 ? (
                                assignments.map((assignment) => {
                                    const assignee = memberships.find((m) => m.userId === assignment.userId)?.user;
                                    const role = roles.find((r) => r.id === assignment.roleId);

                                    return (
                                        <div key={assignment.id}
                                             className="p-3 bg-gray-50 rounded-xl border border-gray-100 flex flex-col gap-1.5">
                                            <span className="text-sm font-bold text-[#1f2937]">
                                                {assignee?.name ?? "Невідомий користувач"}
                                            </span>
                                            <div className="flex items-center justify-between">
                                                {role ? (
                                                    <span
                                                        className="px-2 py-0.5 text-[10px] font-bold rounded-full bg-blue-50 text-blue-700 border border-blue-100">
                                                        {role.name}
                                                    </span>
                                                ) : (
                                                    <span
                                                        className="px-2 py-0.5 text-[10px] font-bold rounded-full bg-gray-100 text-gray-500 border border-gray-200">
                                                        Без ролі
                                                    </span>
                                                )}
                                                <span className="text-xs font-medium text-gray-500">
                                                    {assignment.estimatedHours} год
                                                </span>
                                            </div>
                                        </div>
                                    );
                                })
                            ) : (
                                <span className="text-sm font-semibold text-gray-400 pl-5">Не призначено</span>
                            )}
                        </div>

                        {isAddMemberOpen && (
                            <form onSubmit={handleAddMember}
                                  className="mt-4 p-4 bg-blue-50 rounded-xl border border-blue-100 space-y-3">
                                <h4 className="text-xs font-bold text-blue-900 uppercase">Додати учасника</h4>

                                <select
                                    value={selectedUserId}
                                    onChange={(e) => setSelectedUserId(e.target.value)}
                                    className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500"
                                    required
                                >
                                    <option value="">Оберіть користувача</option>
                                    {memberships.map((m) => (
                                        <option key={m.userId} value={m.userId}>
                                            {m.user.name}
                                        </option>
                                    ))}
                                </select>

                                <select
                                    value={selectedRoleId}
                                    onChange={(e) => setSelectedRoleId(e.target.value)}
                                    className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500"
                                >
                                    <option value="">Без ролі</option>
                                    {roles.map((r) => (
                                        <option key={r.id} value={r.id}>
                                            {r.name}
                                        </option>
                                    ))}
                                </select>

                                {/* Керування кастомними ролями */}
                                <div className="rounded-xl border border-blue-200/80 bg-white p-3 space-y-3 shadow-xs">
                                    <div className="flex items-center justify-between">
                                        <span className="text-[11px] font-bold uppercase tracking-wider text-gray-500">
                                            Ролі проєкту ({roles.length})
                                        </span>
                                        {!isAddingRole && !editingRoleId && (
                                            <button
                                                type="button"
                                                onClick={() => { setIsAddingRole(true); setEditingRoleId(null); setRoleError(""); }}
                                                className="flex items-center gap-1 text-xs font-semibold text-blue-600 hover:text-blue-700 transition"
                                            >
                                                <FiPlus size={13} /> Нова роль
                                            </button>
                                        )}
                                    </div>

                                    {/* Форма додавання ролі */}
                                    {isAddingRole && (
                                        <div className="flex flex-col gap-2 p-2.5 bg-blue-50/80 rounded-xl border border-blue-200">
                                            <span className="text-[10px] font-bold text-blue-900 uppercase tracking-wider">
                                                Створення ролі
                                            </span>
                                            <input
                                                type="text"
                                                value={newRoleName}
                                                onChange={(e) => setNewRoleName(e.target.value)}
                                                placeholder="Назва ролі (напр. QA)"
                                                className="w-full rounded-lg border border-gray-300 bg-white px-2.5 py-1.5 text-xs outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-400"
                                                maxLength={50}
                                                autoFocus
                                                onKeyDown={(e) => {
                                                    if (e.key === "Enter") {
                                                        e.preventDefault();
                                                        handleCreateCustomRole(e as any);
                                                    } else if (e.key === "Escape") {
                                                        setIsAddingRole(false);
                                                        setRoleError("");
                                                        setNewRoleName("");
                                                    }
                                                }}
                                            />
                                            <div className="flex items-center gap-1.5">
                                                <button
                                                    type="button"
                                                    onClick={handleCreateCustomRole}
                                                    disabled={creatingRole || !newRoleName.trim()}
                                                    className="flex-1 rounded-lg bg-blue-600 py-1.5 px-2 text-xs font-semibold text-white hover:bg-blue-700 disabled:opacity-50 transition"
                                                >
                                                    {creatingRole ? "..." : "Зберегти"}
                                                </button>
                                                <button
                                                    type="button"
                                                    onClick={() => { setIsAddingRole(false); setRoleError(""); setNewRoleName(""); }}
                                                    className="rounded-lg border border-gray-300 bg-white py-1.5 px-3 text-xs font-medium text-gray-600 hover:bg-gray-50 transition"
                                                >
                                                    Скасувати
                                                </button>
                                            </div>
                                        </div>
                                    )}

                                    {/* Форма редагування ролі */}
                                    {editingRoleId && (
                                        <div className="flex flex-col gap-2 p-2.5 bg-amber-50/80 rounded-xl border border-amber-200">
                                            <span className="text-[10px] font-bold text-amber-900 uppercase tracking-wider">
                                                Редагувати роль
                                            </span>
                                            <input
                                                type="text"
                                                value={editingRoleName}
                                                onChange={(e) => setEditingRoleName(e.target.value)}
                                                placeholder="Нова назва ролі"
                                                className="w-full rounded-lg border border-gray-300 bg-white px-2.5 py-1.5 text-xs outline-none focus:border-amber-500 focus:ring-1 focus:ring-amber-400"
                                                maxLength={50}
                                                autoFocus
                                                onKeyDown={(e) => {
                                                    if (e.key === "Enter") {
                                                        e.preventDefault();
                                                        handleUpdateCustomRole(editingRoleId);
                                                    } else if (e.key === "Escape") {
                                                        handleCancelEditRole();
                                                    }
                                                }}
                                            />
                                            <div className="flex items-center gap-1.5">
                                                <button
                                                    type="button"
                                                    onClick={() => handleUpdateCustomRole(editingRoleId)}
                                                    disabled={updatingRole || !editingRoleName.trim()}
                                                    className="flex-1 rounded-lg bg-amber-600 py-1.5 px-2 text-xs font-semibold text-white hover:bg-amber-700 disabled:opacity-50 transition"
                                                >
                                                    {updatingRole ? "..." : "Оновити"}
                                                </button>
                                                <button
                                                    type="button"
                                                    onClick={handleCancelEditRole}
                                                    className="rounded-lg border border-gray-300 bg-white py-1.5 px-3 text-xs font-medium text-gray-600 hover:bg-gray-50 transition"
                                                >
                                                    Скасувати
                                                </button>
                                            </div>
                                        </div>
                                    )}

                                    {roleError && (
                                        <div className="text-xs text-red-600 bg-red-50 p-2 rounded-lg border border-red-200">
                                            {roleError}
                                        </div>
                                    )}

                                    {roles.length === 0 ? (
                                        <p className="text-[11px] text-gray-400 italic">Кастомних ролей ще немає.</p>
                                    ) : (
                                        <div className="flex flex-wrap gap-1.5">
                                            {roles.map((r) => {
                                                const isSelected = selectedRoleId === r.id;
                                                const isEditing = editingRoleId === r.id;
                                                return (
                                                    <div
                                                        key={r.id}
                                                        className={`flex items-center gap-1 rounded-lg border px-2 py-1 text-xs font-medium transition ${
                                                            isEditing
                                                                ? "border-amber-400 bg-amber-50 text-amber-900 ring-1 ring-amber-300"
                                                                : isSelected
                                                                ? "border-blue-500 bg-blue-50 text-blue-800 ring-1 ring-blue-300"
                                                                : "border-gray-200 bg-gray-50/80 text-gray-700 hover:bg-gray-100"
                                                        }`}
                                                    >
                                                        <button
                                                            type="button"
                                                            onClick={() => setSelectedRoleId(isSelected ? "" : r.id)}
                                                            className="text-left font-medium hover:underline"
                                                            title={isSelected ? "Зняти вибір" : "Обрати цю роль"}
                                                        >
                                                            {r.name}
                                                        </button>
                                                        <div className="flex items-center gap-0.5 border-l border-gray-300/70 pl-1 ml-0.5">
                                                            <button
                                                                type="button"
                                                                onClick={() => handleStartEditRole(r)}
                                                                className="text-gray-400 hover:text-blue-600 p-0.5 transition rounded hover:bg-white"
                                                                title="Редагувати назву"
                                                            >
                                                                <FiEdit2 size={11} />
                                                            </button>
                                                            <button
                                                                type="button"
                                                                onClick={() => handleDeleteCustomRole(r)}
                                                                disabled={deletingRole}
                                                                className="text-gray-400 hover:text-red-600 p-0.5 transition rounded hover:bg-white"
                                                                title="Видалити роль"
                                                            >
                                                                <FiTrash2 size={11} />
                                                            </button>
                                                        </div>
                                                    </div>
                                                );
                                            })}
                                        </div>
                                    )}
                                </div>

                                <input
                                    type="number"
                                    step="0.1"
                                    min="0.1"
                                    value={selectedHours}
                                    onChange={(e) => setSelectedHours(e.target.value)}
                                    placeholder="Години (напр: 2.5)"
                                    className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500"
                                    required
                                />

                                <div className="flex gap-2">
                                    <button
                                        type="submit"
                                        disabled={assigning}
                                        className="flex-1 px-3 py-2 text-xs font-semibold text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-50"
                                    >
                                        {assigning ? "Збереження..." : "Додати"}
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => {
                                            setIsAddMemberOpen(false);
                                            setIsAddingRole(false);
                                            setEditingRoleId(null);
                                            setRoleError("");
                                        }}
                                        className="flex-1 px-3 py-2 text-xs font-semibold text-gray-600 bg-gray-100 rounded-lg hover:bg-gray-200"
                                    >
                                        Скасувати
                                    </button>
                                </div>
                            </form>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
};