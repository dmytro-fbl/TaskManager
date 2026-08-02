import { gql } from "@apollo/client";
import { useMutation, useQuery } from "@apollo/client/react";
import { FiLock, FiUnlock, FiEye } from 'react-icons/fi';
import { useNavigate } from 'react-router-dom';
import { getFriendlyErrorMessage } from "../../../../utils/errorHandler";
import { GET_ADMIN_PROJECTS } from "../../../../graphql/queries/projectQuery";
import { TOGGLE_PROJECT_ARCHIVE } from "../../../../graphql/mutations/projectMutation";

// Типізація для нашого DTO
interface AdminProject {
    id: string;
    title: string;
    description: string | null;
    budgetCap: number | null;
    status: string;
    isArchived: boolean;
    createdAt: string;
    ownerName: string;
    ownerEmail: string;
}

interface GetAdminProjectsData {
    adminProjects: AdminProject[];
}

export default function ProjectsTab() {

    const navigate = useNavigate();

    // 1. Отримуємо список
    const { data, loading, error, refetch } = useQuery<GetAdminProjectsData>(GET_ADMIN_PROJECTS, {
        fetchPolicy: 'network-only'
    });

    // 2. Мутація для зміни статусу
    const [toggleArchive] = useMutation(TOGGLE_PROJECT_ARCHIVE);

    const handleToggleArchive = async (projectId: string, currentStatus: boolean) => {
        const actionText = currentStatus ? 'розблокувати' : 'заблокувати (архівувати)';
        if (!window.confirm(`Ви впевнені, що хочете ${actionText} цей проєкт?`)) return;

        try {
            await toggleArchive({
                variables: {
                    projectId,
                    isArchived: !currentStatus // Відправляємо протилежний статус
                }
            });
            refetch(); // Оновлюємо дані після мутації
        } catch (err: any) {
            alert(getFriendlyErrorMessage(err));
        }
    };

    const handleViewDetails = (projectId: string) => {
        navigate(`/projects/${projectId}`);
    }

    // Форматування дати
    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('uk-UA', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    if (loading) {
        return (
            <div className="bg-bg-card p-10 rounded-xl shadow-sm border border-gray-100 flex justify-center items-center">
                <div className="animate-spin rounded-full h-10 w-10 border-4 border-gray-200 border-t-primary"></div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="bg-red-50 p-6 rounded-xl border border-red-200 text-red-600">
                <h3 className="font-bold text-lg mb-2">Помилка завантаження</h3>
                <p>{getFriendlyErrorMessage(error)}</p>
            </div>
        );
    }

    return (
        <div className="bg-bg-card rounded-xl shadow-sm border border-gray-100 overflow-hidden">
            {/* Шапка таблиці */}
            <div className="p-6 border-b border-gray-100 flex justify-between items-center bg-white">
                <h2 className="text-xl font-bold text-text-main">Список проєктів</h2>
                <span className="bg-gray-100 text-text-muted px-3 py-1 rounded-full text-sm font-medium">
                    Всього: {data?.adminProjects?.length || 0}
                </span>
            </div>

            <div className="overflow-x-auto">
                <table className="w-full text-left border-collapse">
                    <thead>
                        <tr className="bg-gray-50 text-text-muted text-sm uppercase tracking-wider">
                            <th className="p-4 font-medium">Проєкт</th>
                            <th className="p-4 font-medium">Власник</th>
                            <th className="p-4 font-medium">Бюджет</th>
                            <th className="p-4 font-medium">Статус</th>
                            <th className="p-4 font-medium">Створено</th>
                            <th className="p-4 font-medium text-right">Дії</th>
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                        {data?.adminProjects?.map((project) => (
                            <tr key={project.id} className="hover:bg-gray-50/50 transition-colors">
                                <td className="p-4">
                                    <p className="font-medium text-text-main">{project.title}</p>
                                    <p className="text-xs text-text-muted truncate max-w-[200px]">
                                        {project.description || "Без опису"}
                                    </p>
                                </td>
                                <td className="p-4">
                                    <p className="font-medium text-text-main">{project.ownerName}</p>
                                    <p className="text-xs text-text-muted">{project.ownerEmail}</p>
                                </td>
                                <td className="p-4 text-sm text-text-main font-medium">
                                    {project.budgetCap ? `$${project.budgetCap}` : '—'}
                                </td>
                                <td className="p-4">
                                    {/* Плашка для статусу */}
                                    <span className={`px-3 py-1 rounded-full text-xs font-semibold ${
                                        project.isArchived 
                                            ? 'bg-red-100 text-red-700' 
                                            : 'bg-green-100 text-green-700'
                                    }`}>
                                        {project.isArchived ? 'Заблокований' : 'Активний'}
                                    </span>
                                </td>
                                <td className="p-4 text-sm text-text-muted">
                                    {formatDate(project.createdAt)}
                                </td>
                                <td className="p-4 text-right">
                                    {/* Кнопка деталей проєкту */}
                                    <button
                                        onClick={() => handleViewDetails(project.id)}
                                        title="Детальніше про проєкт"
                                        className="p-2 rounded-md text-gray-400 hover:text-primary hover:bg-blue-50 transition-colors"
                                    >
                                        <FiEye size={18} />
                                    </button>
                                    {/* Кнопка статусу (архівації) */}
                                    <button 
                                        onClick={() => handleToggleArchive(project.id, project.isArchived)}
                                        title={project.isArchived ? "Розблокувати" : "Заблокувати"}
                                        className={`p-2 rounded-md transition-colors ${
                                            project.isArchived 
                                                ? 'text-gray-400 hover:text-green-600 hover:bg-green-50' 
                                                : 'text-gray-400 hover:text-danger hover:bg-red-50'
                                        }`}
                                    >
                                        {project.isArchived ? <FiUnlock size={18} /> : <FiLock size={18} />}
                                    </button>
                                </td>
                            </tr>
                        ))}
                        
                        {(!data?.adminProjects || data.adminProjects.length === 0) && (
                            <tr>
                                <td colSpan={6} className="p-8 text-center text-text-muted">
                                    Проєктів не знайдено
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </div>
        </div>
    );
}