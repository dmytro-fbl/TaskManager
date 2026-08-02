import { Link, useNavigate } from 'react-router-dom';
import { gql } from '@apollo/client';
import { useQuery, useMutation } from '@apollo/client/react';
import { Check } from "lucide-react";
import { LOGOUT_MUTATION } from '../../graphql/mutations/logoutMutations';

export interface GetMeData {
  me: {
    id: string;
    name: string;
    isAdmin: boolean;
  } | null;
}

export const GET_ME_QUERY = gql`
  query GetMe {
    me{
      id
      name
      isAdmin    
    }
  }
`;

export default function Header() {
  const navigate = useNavigate();

  const { data } = useQuery<GetMeData>(GET_ME_QUERY,{
    fetchPolicy: 'network-only'
    });

  const [logoutMutation] = useMutation(LOGOUT_MUTATION);
  const handleLogout = async () => {
    try {
      await logoutMutation();
    } catch (err) {
      console.error("Помилка під час виходу на сервері:", err);
    } finally {
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('userEmail');

      window.location.href = '/login';
    }
  };

  return (
    <header className="bg-bg-card shadow-sm border-b border-gray-200 sticky top-0 z-10">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
        {/* Логотип / Назва */}
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 bg-primary rounded-lg flex items-center justify-center text-white">
            <Check className="w-5 h-5" />
          </div>
          <span className="text-xl font-bold text-text-main">TaskTracker</span>
        </div>

        {/* Навігація */}
        <nav className="hidden md:flex gap-6">
          <Link to="/dashboard" className="text-text-main hover:text-primary font-medium transition">
            Дашборд
          </Link>

          <Link to="/projects" className="text-text-main hover:text-primary font-medium transition">
            Проєкти
          </Link>

          {data?.me?.isAdmin && (
            <Link to="/adminPanel" className="text-text-main hover:text-primary font-medium transition">
              Адмін-панель
            </Link>
          )}

        </nav>

        <button
          onClick={handleLogout}
          className="px-4 py-2 text-sm font-medium text-danger hover:bg-danger-bg rounded-md transition"
        >
          Вийти
        </button>
      </div>
    </header>
  );
}