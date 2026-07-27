import { gql } from "@apollo/client";
import { useMutation, useQuery } from "@apollo/client/react";
import { FiShield, FiLock, FiUnlock } from 'react-icons/fi';
import { getFriendlyErrorMessage } from "../../../../utils/errorHandler";
import User from "../../../../types/User";
import { GET_USERS_QUERY } from "../../../../graphql/queries/userQuery";
import { TOGGLE_USER_ROLE, TOGGLE_USER_STATUS } from "../../../../graphql/mutations/userMutations";

interface GetUserData {
    users: User[];
}


export default function UsersTab() {

    const { data, loading, error, refetch } = useQuery<GetUserData>(GET_USERS_QUERY, {
        fetchPolicy: 'network-only'
    });

    const [toggleRole] = useMutation(TOGGLE_USER_ROLE);
    const [toggleStatus] = useMutation(TOGGLE_USER_STATUS);

    const handleToggleRole = async (userId: string, currentIsAdmin: boolean) => {
      const actionText = currentIsAdmin ? 'забрати права адміністатора у' : 'надати права адміністратора';
      if(!window.confirm(`Ви впевнені, що хочте ${actionText} цього користувача?`)) return;

      try{
          await toggleRole({
            variables: {userId, isAdmin: !currentIsAdmin}
          });
          refetch();
      }catch (err: any){
        alert(getFriendlyErrorMessage(err));
      }
    }

    const handleToggleStatus = async (userId: string, currentIsActive: boolean) => {
    const actionText = currentIsActive ? 'заблокувати' : 'розблокувати';
    if (!window.confirm(`Ви впевнені, що хочете ${actionText} цього користувача?`)) return;

    try {
      await toggleStatus({ 
        variables: { userId, isActive: !currentIsActive } 
      });
      refetch(); 
    } catch (err: any) {
      alert(getFriendlyErrorMessage(err));
    }
  };

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
        <h2 className="text-xl font-bold text-text-main">Список користувачів</h2>
        <span className="bg-gray-100 text-text-muted px-3 py-1 rounded-full text-sm font-medium">
          Всього: {data?.users.length || 0}
        </span>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="bg-gray-50 text-text-muted text-sm uppercase tracking-wider">
              <th className="p-4 font-medium">Користувач</th>
              <th className="p-4 font-medium">Email</th>
              <th className="p-4 font-medium">Роль</th>
              <th className="p-4 font-medium">Статус</th>
              <th className="p-4 font-medium">Дата реєстрації</th>
              <th className="p-4 font-medium text-right">Дії</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {data?.users.map((user) => (
              <tr key={user.id} className="hover:bg-gray-50/50 transition-colors">
                <td className="p-4 font-medium text-text-main">
                  {user.name}
                </td>
                <td className="p-4 text-text-muted">
                  {user.email}
                </td>
                <td className="p-4">
                  {/* Плашка для ролі */}
                  <span className={`px-3 py-1 rounded-full text-xs font-semibold ${
                    user.isAdmin 
                      ? 'bg-purple-100 text-purple-700' 
                      : 'bg-blue-100 text-blue-700'
                  }`}>
                    {user.isAdmin ? 'Admin' : 'User'}
                  </span>
                </td>
                <td className="p-4">
                  {/* Плашка для статусу */}
                  <span className={`px-3 py-1 rounded-full text-xs font-semibold ${
                    user.isActive 
                      ? 'bg-green-100 text-green-700' 
                      : 'bg-red-100 text-red-700'
                  }`}>
                    {user.isActive ? 'Активний' : 'Заблокований'}
                  </span>
                </td>
                <td className="p-4 text-sm text-text-muted">
                  {formatDate(user.createdAt)}
                </td>
                <td className="p-4 text-right">

                  {/* Кнопки дій */}
                 <button 
                    onClick={() => handleToggleRole(user.id, user.isAdmin)}
                    title={user.isAdmin ? "Забрати права адміна" : "Зробити адміном"}
                    className={`p-2 rounded-md transition-colors ${
                      user.isAdmin 
                        ? 'text-purple-600 bg-purple-50 hover:bg-purple-100' 
                        : 'text-gray-400 hover:text-purple-600 hover:bg-purple-50'
                    }`}
                  >
                    <FiShield size={18} />
                  </button>
                  
                  {/* Кнопка статусу */}
                  <button 
                    onClick={() => handleToggleStatus(user.id, user.isActive)}
                    title={user.isActive ? "Заблокувати" : "Розблокувати"}
                    className={`p-2 rounded-md transition-colors ${
                      user.isActive 
                        ? 'text-gray-400 hover:text-danger hover:bg-red-50' 
                        : 'text-danger bg-red-50 hover:bg-red-100'
                    }`}
                  >
                    {user.isActive ? <FiLock size={18} /> : <FiUnlock size={18} />}
                  </button>
                </td>
              </tr>
            ))}
            
            {data?.users.length === 0 && (
              <tr>
                <td colSpan={6} className="p-8 text-center text-text-muted">
                  Користувачів не знайдено
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}