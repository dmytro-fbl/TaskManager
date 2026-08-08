import React, {
    FormEvent,
    useMemo,
    useState,
} from "react";
import { gql } from "@apollo/client";
import {
    useMutation,
    useQuery,
} from "@apollo/client/react";
import { getFriendlyErrorMessage } from "../../../utils/errorHandler";

const GET_PROJECT_TASKS = gql`
    query GetProjectTasks($projectId: UUID!) {
        projectTasks(projectId: $projectId) {
            id
            projectId
            authorId
            assigneeId
            statusId
            title
            notes
            priority
            startDate
            dueDate
            estimatedBudget
            estimatedUnit
            createdAt
            updatedAt
        }
    }
`;

const GET_PROJECT_STATUSES = gql`
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

const GET_PROJECT_MEMBERSHIPS = gql`
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

const CREATE_TASK = gql`
    mutation CreateTask($input: CreateTaskInput!) {
        createTask(input: $input) {
            id
            projectId
            assigneeId
            statusId
            title
            notes
            priority
            startDate
            dueDate
            estimatedBudget
            estimatedUnit
            createdAt
            updatedAt
        }
    }
`;

const ASSIGN_USER_TO_TASK = gql`
    mutation AssignUserToTask(
        $taskId: UUID!
        $userId: UUID!
        $estimatedHours: Decimal!
    ) {
        assignUserToTask(
            taskId: $taskId
            userId: $userId
            estimatedHours: $estimatedHours
        )
    }
`;

const UPDATE_TASK_STATUS = gql`
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

type Task = {
    id: string;
    projectId: string;
    authorId: string;
    assigneeId?: string | null;
    statusId: string;
    title: string;
    notes?: string | null;
    priority: string;
    startDate?: string | null;
    dueDate?: string | null;
    estimatedBudget?: number | null;
    estimatedUnit?: string | null;
    createdAt: string;
    updatedAt: string;
};

type ProjectStatus = {
    id: string;
    name: string;
    category: string;
    color?: string | null;
    sortOrder: number;
    isFinal: boolean;
};

type Membership = {
    id: string;
    userId: string;
    user: {
        id: string;
        name: string;
        email: string;
        isAdmin: boolean;
    };
};

type CreateTaskInput = {
    projectId: string;
    statusId: string;
    title: string;
    notes: string | null;
    priority: string;
    startDate: string | null;
    dueDate: string | null;
    estimatedBudget: number | null;
    estimatedUnit: string | null;
};

type CreateTaskResponse = {
    createTask: {
        id: string;
        projectId: string;
        assigneeId?: string | null;
        statusId: string;
        title: string;
        notes?: string | null;
        priority: string;
        startDate?: string | null;
        dueDate?: string | null;
        estimatedBudget?: number | null;
        estimatedUnit?: string | null;
        createdAt: string;
        updatedAt: string;
    } | null;
};

type Props = {
    projectId: string;
};

function toGraphQLDate(value: string): string | null {
    if (!value) {
        return null;
    }

    return `${value}T00:00:00.000Z`;
}

function formatDate(value?: string | null): string {
    if (!value) {
        return "—";
    }

    return new Date(value).toLocaleDateString("uk-UA");
}

export const ProjectTasks: React.FC<Props> = ({
                                                  projectId,
                                              }) => {
    const [title, setTitle] = useState("");
    const [notes, setNotes] = useState("");
    const [statusId, setStatusId] = useState("");
    const [priority, setPriority] = useState("medium");
    const [dueDate, setDueDate] = useState("");
    const [assigneeId, setAssigneeId] = useState("");
    const [estimatedHours, setEstimatedHours] = useState("1");
    const [formError, setFormError] = useState("");

    const tasksQuery = useQuery<{
        projectTasks: Task[];
    }>(GET_PROJECT_TASKS, {
        variables: {
            projectId,
        },
        fetchPolicy: "network-only",
    });

    const statusesQuery = useQuery<{
        projectStatuses: ProjectStatus[];
    }>(GET_PROJECT_STATUSES, {
        variables: {
            projectId,
        },
        fetchPolicy: "network-only",
    });

    const membershipsQuery = useQuery<{
        projectMemberships: Membership[];
    }>(GET_PROJECT_MEMBERSHIPS, {
        variables: {
            projectId,
        },
        fetchPolicy: "network-only",
    });

    const [createTask, { loading: creating }] = useMutation<
        CreateTaskResponse,
        { input: CreateTaskInput }
    >(CREATE_TASK);

    const [assignUserToTask] = useMutation(
        ASSIGN_USER_TO_TASK
    );

    const [updateTaskStatus] = useMutation(
        UPDATE_TASK_STATUS
    );

    const tasks =
        tasksQuery.data?.projectTasks ?? [];

    const statuses =
        statusesQuery.data?.projectStatuses ?? [];

    const memberships =
        membershipsQuery.data?.projectMemberships ?? [];

    const statusMap = useMemo(() => {
        return new Map(
            statuses.map((status) => [
                status.id,
                status,
            ])
        );
    }, [statuses]);

    const memberMap = useMemo(() => {
        return new Map(
            memberships.map((membership) => [
                membership.userId,
                membership.user,
            ])
        );
    }, [memberships]);

    const handleCreateTask = async (
        event: FormEvent<HTMLFormElement>
    ) => {
        event.preventDefault();
        setFormError("");

        const selectedStatusId =
            statusId || statuses[0]?.id || "";

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
                        estimatedBudget: null,
                        estimatedUnit: null,
                    },
                },
            });

            const createdTaskId =
                result.data?.createTask?.id;

            if (createdTaskId && assigneeId) {
                await assignUserToTask({
                    variables: {
                        taskId: createdTaskId,
                        userId: assigneeId,
                        estimatedHours:
                            Number(estimatedHours) > 0
                                ? Number(estimatedHours)
                                : 1,
                    },
                });
            }

            setTitle("");
            setNotes("");
            setDueDate("");
            setAssigneeId("");
            setEstimatedHours("1");
            setStatusId("");

            await tasksQuery.refetch();
        } catch (error: any) {
            setFormError(
                getFriendlyErrorMessage(error) ??
                "Не вдалося створити таску."
            );
        }
    };

    const handleStatusChange = async (
        taskId: string,
        nextStatusId: string
    ) => {
        try {
            await updateTaskStatus({
                variables: {
                    taskId,
                    statusId: nextStatusId,
                },
            });

            await tasksQuery.refetch();
        } catch (error: any) {
            alert(
                getFriendlyErrorMessage(error) ??
                "Не вдалося змінити статус таски."
            );
        }
    };

    if (
        tasksQuery.loading ||
        statusesQuery.loading ||
        membershipsQuery.loading
    ) {
        return (
            <div className="rounded-xl border border-gray-100 bg-bg-card p-6 shadow-sm">
                Завантаження тасків...
            </div>
        );
    }

    if (tasksQuery.error) {
        return (
            <div className="rounded-xl border border-red-100 bg-red-50 p-6 text-red-700">
                {getFriendlyErrorMessage(tasksQuery.error) ??
                    "Не вдалося завантажити таски."}
            </div>
        );
    }

    if (statusesQuery.error) {
        return (
            <div className="rounded-xl border border-red-100 bg-red-50 p-6 text-red-700">
                {getFriendlyErrorMessage(statusesQuery.error) ??
                    "Не вдалося завантажити статуси."}
            </div>
        );
    }

    if (membershipsQuery.error) {
        return (
            <div className="rounded-xl border border-red-100 bg-red-50 p-6 text-red-700">
                {getFriendlyErrorMessage(
                        membershipsQuery.error
                    ) ??
                    "Не вдалося завантажити учасників проєкту."}
            </div>
        );
    }

    return (
        <section className="space-y-6">
            <div className="rounded-xl border border-gray-100 bg-bg-card p-6 shadow-sm">
                <h2 className="mb-5 text-xl font-bold text-text-main">
                    Створити таску
                </h2>

                {formError && (
                    <div className="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-700">
                        {formError}
                    </div>
                )}

                <form
                    onSubmit={handleCreateTask}
                    className="grid gap-4 md:grid-cols-2"
                >
                    <div className="md:col-span-2">
                        <label className="mb-1 block text-sm font-medium text-text-main">
                            Назва
                        </label>

                        <input
                            value={title}
                            onChange={(event) =>
                                setTitle(event.target.value)
                            }
                            className="w-full rounded-lg border border-gray-300 px-3 py-2 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
                            placeholder="Наприклад: Додати авторизацію"
                        />
                    </div>

                    <div className="md:col-span-2">
                        <label className="mb-1 block text-sm font-medium text-text-main">
                            Опис
                        </label>

                        <textarea
                            value={notes}
                            onChange={(event) =>
                                setNotes(event.target.value)
                            }
                            rows={3}
                            className="w-full rounded-lg border border-gray-300 px-3 py-2 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
                            placeholder="Опис таски"
                        />
                    </div>

                    <div>
                        <label className="mb-1 block text-sm font-medium text-text-main">
                            Статус
                        </label>

                        <select
                            value={
                                statusId ||
                                statuses[0]?.id ||
                                ""
                            }
                            onChange={(event) =>
                                setStatusId(event.target.value)
                            }
                            className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2"
                        >
                            {statuses.map((status) => (
                                <option
                                    key={status.id}
                                    value={status.id}
                                >
                                    {status.name}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div>
                        <label className="mb-1 block text-sm font-medium text-text-main">
                            Пріоритет
                        </label>

                        <select
                            value={priority}
                            onChange={(event) =>
                                setPriority(event.target.value)
                            }
                            className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2"
                        >
                            <option value="low">
                                Низький
                            </option>
                            <option value="medium">
                                Середній
                            </option>
                            <option value="high">
                                Високий
                            </option>
                            <option value="critical">
                                Критичний
                            </option>
                        </select>
                    </div>

                    <div>
                        <label className="mb-1 block text-sm font-medium text-text-main">
                            Дедлайн
                        </label>

                        <input
                            type="date"
                            value={dueDate}
                            onChange={(event) =>
                                setDueDate(event.target.value)
                            }
                            className="w-full rounded-lg border border-gray-300 px-3 py-2"
                        />
                    </div>

                    <div>
                        <label className="mb-1 block text-sm font-medium text-text-main">
                            Виконавець
                        </label>

                        <select
                            value={assigneeId}
                            onChange={(event) =>
                                setAssigneeId(event.target.value)
                            }
                            className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2"
                        >
                            <option value="">
                                Не призначати
                            </option>

                            {memberships.map((membership) => (
                                <option
                                    key={membership.userId}
                                    value={membership.userId}
                                >
                                    {membership.user.name} —{" "}
                                    {membership.user.email}
                                </option>
                            ))}
                        </select>
                    </div>

                    {assigneeId && (
                        <div>
                            <label className="mb-1 block text-sm font-medium text-text-main">
                                Орієнтовні години
                            </label>

                            <input
                                type="number"
                                min="0.1"
                                step="0.1"
                                value={estimatedHours}
                                onChange={(event) =>
                                    setEstimatedHours(
                                        event.target.value
                                    )
                                }
                                className="w-full rounded-lg border border-gray-300 px-3 py-2"
                            />
                        </div>
                    )}

                    <div className="md:col-span-2">
                        <button
                            type="submit"
                            disabled={creating}
                            className="rounded-lg bg-blue-600 px-5 py-2.5 text-sm font-medium text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                            {creating
                                ? "Створення..."
                                : "Створити таску"}
                        </button>
                    </div>
                </form>
            </div>

            <div className="rounded-xl border border-gray-100 bg-bg-card shadow-sm">
                <div className="border-b border-gray-100 p-6">
                    <h2 className="text-xl font-bold text-text-main">
                        Таски проєкту
                    </h2>
                </div>

                {tasks.length === 0 ? (
                    <div className="p-6 text-text-muted">
                        Тасків ще немає.
                    </div>
                ) : (
                    <div className="divide-y divide-gray-100">
                        {tasks.map((task) => {
                            const status = statusMap.get(
                                task.statusId
                            );

                            const assignee = task.assigneeId
                                ? memberMap.get(task.assigneeId)
                                : null;

                            return (
                                <article
                                    key={task.id}
                                    className="space-y-3 p-6"
                                >
                                    <div className="flex flex-wrap items-start justify-between gap-4">
                                        <div>
                                            <h3 className="text-lg font-semibold text-text-main">
                                                {task.title}
                                            </h3>

                                            <p className="mt-1 text-sm text-text-muted">
                                                {task.notes?.trim()
                                                    ? task.notes
                                                    : "Опис відсутній."}
                                            </p>
                                        </div>

                                        <select
                                            value={task.statusId}
                                            onChange={(event) =>
                                                handleStatusChange(
                                                    task.id,
                                                    event.target.value
                                                )
                                            }
                                            className="rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm"
                                            style={{
                                                borderColor:
                                                    status?.color ??
                                                    undefined,
                                            }}
                                        >
                                            {statuses.map(
                                                (item) => (
                                                    <option
                                                        key={item.id}
                                                        value={item.id}
                                                    >
                                                        {item.name}
                                                    </option>
                                                )
                                            )}
                                        </select>
                                    </div>

                                    <div className="grid gap-3 text-sm text-text-muted md:grid-cols-3">
                                        <div>
                                            <span className="font-medium text-text-main">
                                                Статус:
                                            </span>{" "}
                                            {status?.name ?? "—"}
                                        </div>

                                        <div>
                                            <span className="font-medium text-text-main">
                                                Виконавець:
                                            </span>{" "}
                                            {assignee?.name ??
                                                "Не призначено"}
                                        </div>

                                        <div>
                                            <span className="font-medium text-text-main">
                                                Дедлайн:
                                            </span>{" "}
                                            {formatDate(
                                                task.dueDate
                                            )}
                                        </div>
                                    </div>

                                    <div className="text-xs text-text-muted">
                                        Пріоритет: {task.priority}
                                    </div>
                                </article>
                            );
                        })}
                    </div>
                )}
            </div>
        </section>
    );
};