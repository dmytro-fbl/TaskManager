import { useQuery } from "@apollo/client/react";
import { GET_PROJECT_STATUSES } from "../../../graphql/queries/project/projectQuery";


type ProjectStatus = {
    id: string;
    projectId: string;
    name: string;
    category: string;
    color?: string | null;
    sortOrder: number;
    isFinal: boolean;
};

type ProjectStatusesData = {
    projectStatuses: ProjectStatus[];
};

type ProjectStatusesProps = {
    projectId: string;
    selectedStatusId?: string;
    onChange?: (statusId: string) => void;
};

export function ProjectStatuses({
    projectId,
    selectedStatusId,
    onChange,
}: ProjectStatusesProps) {
    const { data, loading, error } = useQuery<ProjectStatusesData>(
        GET_PROJECT_STATUSES,
        {
            variables: { projectId },
            skip: !projectId,
            fetchPolicy: "network-only",
        }
    );

    if (loading) {
        return (
            <div className="text-sm text-text-muted">
                Завантаження статусів...
            </div>
        );
    }

    if (error) {
        return (
            <div className="text-sm text-red-600">
                Не вдалося завантажити статуси.
            </div>
        );
    }

    const statuses = data?.projectStatuses ?? [];

    if (statuses.length === 0) {
        return (
            <div className="text-sm text-text-muted">
                Статуси відсутні.
            </div>
        );
    }

    return (
        <select
            id="project-status"
            value={selectedStatusId || statuses[0].id}
            onChange={(event) => onChange?.(event.target.value)}
            className="rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-text-main shadow-sm outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
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
    );
}