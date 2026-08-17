import React, { useState, FormEvent } from 'react'; 
import { gql } from '@apollo/client';
import { useMutation, useQuery } from '@apollo/client/react';
import { FiPlus } from 'react-icons/fi';

import { GET_PROJECT_ROLES } from '../../graphql/queries/project/projectQuery';

type Role = {
    id: string;
    name: string;
};

type GetProjectRolesResponse = {
    projectRoles: Role[];
};

type ProjectRolesManagerProps = {
    projectId: string; 
};



const CREATE_PROJECT_ROLE = gql`
    mutation CreateProjectRole($projectId: UUID!, $name: String!) {
        createProjectRole(projectId: $projectId, name: $name) {
            id
            name
        }
    }
`;

export const ProjectRolesManager: React.FC<ProjectRolesManagerProps> = ({ projectId }) => {
    const [roleName, setRoleName] = useState("");
    const [error, setError] = useState("");

    const { data, loading } = useQuery<GetProjectRolesResponse>(GET_PROJECT_ROLES, {
        variables: { projectId }
    });

    const [createRole, { loading: creating }] = useMutation(CREATE_PROJECT_ROLE, {
        refetchQueries: [{ query: GET_PROJECT_ROLES, variables: { projectId } }]
    });

    const handleCreateRole = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setError("");

        if (!roleName.trim()) return;

        try {
            await createRole({ variables: { projectId, name: roleName.trim() } });
            setRoleName(""); 
        } catch (err: any) {
            setError(err.message || "Не вдалося створити роль");
        }
    };

    const roles = data?.projectRoles ?? [];

    return (
        <div className="rounded-xl border border-gray-100 bg-bg-card p-6 shadow-sm">
            <h2 className="mb-4 text-xl font-bold text-text-main">Кастомні ролі</h2>
            
            {/* Список існуючих ролей */}
            <div className="mb-6 flex flex-wrap gap-2">
                {loading ? <p className="text-sm text-gray-400">Завантаження...</p> : roles.map((role) => (
                    <span key={role.id} className="rounded-full border border-blue-100 bg-blue-50 px-3 py-1 text-sm font-medium text-blue-700">
                        {role.name}
                    </span>
                ))}
                {roles.length === 0 && !loading && (
                    <span className="text-sm text-gray-400">Ще немає кастомних ролей</span>
                )}
            </div>

            {/* Форма створення нової ролі */}
            <form onSubmit={handleCreateRole} className="flex flex-col gap-3 sm:flex-row">
                <input
                    type="text"
                    value={roleName}
                    onChange={(e) => setRoleName(e.target.value)}
                    placeholder="Наприклад: Frontend Розробник"
                    className="flex-1 rounded-lg border border-gray-300 px-3 py-2 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
                    maxLength={50}
                />
                <button
                    type="submit"
                    disabled={creating || !roleName.trim()}
                    className="flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-50"
                >
                    <FiPlus />
                    {creating ? "Створення..." : "Додати роль"}
                </button>
            </form>

            {error && <p className="mt-2 text-sm text-red-600">{error}</p>}
        </div>
    );
};