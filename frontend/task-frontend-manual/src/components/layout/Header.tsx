import { Link, useNavigate } from 'react-router-dom';

export default function Header() {
  const navigate = useNavigate();

  const handleLogout = () => {
    localStorage.removeItem('token');
    navigate('/login');
  };

  return (
    <header className="bg-bg-card shadow-sm border-b border-gray-200 sticky top-0 z-10">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
        {/* Логотип / Назва */}
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 bg-primary rounded-lg flex items-center justify-center text-white font-bold">
            ✓
          </div>
          <span className="text-xl font-bold text-text-main">TaskTracker</span>
        </div>

        {/* Навігація */}
        <nav className="hidden md:flex gap-6">
          <Link to="/dashboard" className="text-text-main hover:text-primary font-medium transition">
            Дашборд
          </Link>
          <Link to="/adminPanel" className="text-text-main hover:text-primary font-medium transition">
            Адмін-панель
          </Link>
        </nav>

        {/* Кнопка виходу */}
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