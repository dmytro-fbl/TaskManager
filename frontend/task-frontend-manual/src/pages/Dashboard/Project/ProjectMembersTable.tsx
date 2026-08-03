import React, { forwardRef, useImperativeHandle } from "react";
import { useQuery, useMutation } from "@apollo/client/react";
import { gql } from "@apollo/client";
import { getFriendlyErrorMessage } from "../../../utils/errorHandler";
import ErrorMessage from "../../../components/ui/ErrorMessage";
import { UPDATE_PROJECT_MEMBER_ROLE } from "../../../graphql/mutations/projectRoleMutations";

interface ProjectMembership {
    id: string;
    projectId: string;
    userId: string;
    projectRole: string;
    roleLabelId?: string | null;
    joinedAt: string;
    user: {
        id: string;
        name: string;
        email: string;
    };
}

interface GetMeData {
    me: {
        id: string;
        name: string;
        isAdmin: boolean;
    } | null;
}

const GET_ME_QUERY = gql`
  query GetMe {
    me {
      id
      name
      isAdmin
    }
  }
`;

const GET_PROJECT_MEMBERSHIPS = gql`
  query GetProjectMemberships($projectId: UUID!) {
    projectMemberships(projectId: $projectId) {
      id
      projectId
      userId
      projectRole
      roleLabelId
      joinedAt
      user {
        id
        name
        email
      }
    }
  }
`;

interface ProjectMembersTableProps {
    projectId: string;
}

export const ProjectMembersTable = forwardRef<{ refetchMembers: () => void }, ProjectMembersTableProps>(
    ({ projectId }, ref) => {
        const {
            data,
            loading,
            error,
            refetch,
        } = useQuery<{ projectMemberships: ProjectMembership[] }>(GET_PROJECT_MEMBERSHIPS, {
            variables: { projectId },
            fetchPolicy: "network-only",
        });

        useImperativeHandle(ref, () => ({
            refetchMembers: () => {
                refetch();
            }
        }));

        const { data: meData } = useQuery<GetMeData>(GET_ME_QUERY, {
            fetchPolicy: "network-only",
        });

        const [updateRole, { loading: updating }] = useMutation(UPDATE_PROJECT_MEMBER_ROLE);

        const memberships = data?.projectMemberships ?? [];
        const currentUserId = meData?.me?.id ?? null;

        const currentUserMembership = memberships.find((m) => m.userId === currentUserId);
        const canManageRoles = currentUserMembership?.projectRole === "manager";

        const handleChangeRole = async (membership: ProjectMembership, newRole: string) => {
            if (newRole === membership.projectRole) return;

            if (!canManageRoles) {
                alert("Лише менеджер може змінювати ролі учасників.");
                return;
            }

            if (membership.userId === currentUserId) {
                alert("Ви не можете змінювати власну роль у проєкті.");
                return;
            }

            if (!window.confirm(`Змінити роль користувача ${membership.user.email} на '${newRole}'?`)) {
                return;
            }

            try {
                await updateRole({
                    variables: {
                        projectId,
                        userId: membership.userId,
                        projectRole: newRole,
                        roleLabelId: membership.roleLabelId ?? null,
                    },
                });

                await refetch();
            } catch (err: any) {
                alert(getFriendlyErrorMessage(err));
            }
        };

        if (loading) {
            return (
                <div className="bg-bg-card p-6 rounded-xl shadow-sm border border-gray-100">
                    <div className="animate-spin rounded-full h-10 w-10 border-4 border-gray-200 border-t-primary mx-auto"></div>
                </div>
            );
        }

        if (error) {
            return (
                <div className="bg-bg-card p-6 rounded-xl shadow-sm border border-gray-100">
                    <ErrorMessage message={getFriendlyErrorMessage(error as any) ?? "Помилка завантаження учасників"} />
                </div>
            );
        }

        return (
            <div className="bg-bg-card rounded-xl shadow-sm border border-gray-100 mt-8">
                <div className="p-6 border-b border-gray-100 flex justify-between items-center bg-white">
                    <h2 className="text-xl font-bold text-text-main">Учасники проєкту</h2>
                    <span className="bg-gray-100 text-text-muted px-3 py-1 rounded-full text-sm font-medium">
                        Всього: {memberships.length}
                    </span>
                </div>

                {!canManageRoles && memberships.length > 0 && (
                    <div className="px-6 py-3 bg-yellow-50 border-b border-yellow-100 text-sm text-yellow-800">
                        Лише менеджер може змінювати ролі учасників.
                    </div>
                )}

                <div className="overflow-x-auto">
                    <table className="w-full text-left border-collapse">
                        <thead>
                            <tr className="bg-gray-50 text-text-muted text-sm uppercase tracking-wider">
                                <th className="p-4 font-medium">Імʼя</th>
                                <th className="p-4 font-medium">Email</th>
                                <th className="p-4 font-medium">Роль</th>
                                <th className="p-4 font-medium">Приєднаний</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-100">
                            {memberships.map((m) => {
                                const isSelf = m.userId === currentUserId;
                                const isDisabled = updating || !canManageRoles || isSelf;

                                return (
                                    <tr key={m.id} className="hover:bg-gray-50/50 transition-colors">
                                        <td className="p-4 font-medium text-text-main">
                                            <div className="flex items-center gap-2">
                                                <span>{m.user.name}</span>
                                                {isSelf && (
                                                    <span className="px-2 py-0.5 text-xs rounded-full bg-blue-100 text-blue-700">
                                                        Ви
                                                    </span>
                                                )}
                                            </div>
                                        </td>

                                        <td className="p-4 text-text-muted">{m.user.email}</td>

                                        <td className="p-4">
                                            <div className="flex flex-col gap-1">
                                                <select
                                                    value={m.projectRole}
                                                    onChange={(e) => handleChangeRole(m, e.target.value)}
                                                    disabled={isDisabled}
                                                    className={`px-3 py-1 border rounded-lg text-sm ${
                                                        isDisabled
                                                            ? "bg-gray-100 text-gray-400 border-gray-200 cursor-not-allowed"
                                                            : "bg-white text-text-main border-gray-300"
                                                    }`}
                                                >
                                                    <option value="manager">Менеджер</option>
                                                    <option value="contributor">Виконавець</option>
                                                </select>

                                                {isSelf && (
                                                    <span className="text-xs text-gray-500">
                                                        Ви не можете змінювати власну роль
                                                    </span>
                                                )}
                                            </div>
                                        </td>

                                        <td className="p-4 text-sm text-text-muted">
                                            {new Date(m.joinedAt).toLocaleString("uk-UA")}
                                        </td>
                                    </tr>
                                );
                            })}

                            {memberships.length === 0 && (
                                <tr>
                                    <td colSpan={4} className="p-8 text-center text-text-muted">
                                        Учасників ще немає
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>
        );
    }
);