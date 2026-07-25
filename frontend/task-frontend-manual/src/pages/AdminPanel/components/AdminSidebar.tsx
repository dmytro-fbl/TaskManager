import { icons } from 'lucide-react';
import { FiUsers, FiMail, FiFolder } from 'react-icons/fi';

interface AdminSidebarProps {
  activeTab: string;
  setActiveTab: (tab: 'users' | 'invites' | 'projects') => void;
}

export default function AdminSidebar({ activeTab, setActiveTab }: AdminSidebarProps) {
  const navItems = [
    { id: 'users', label: 'Користувачі', icon: FiUsers},
    { id: 'invites', label: 'Запрошення', icon: FiMail},
    { id: 'projects', label: 'Проєкти', icon: FiFolder},
  ];

  return (
    <aside className="w-64 bg-bg-card border-r border-gray-200 p-5 hidden md:block">
      <h2 className="text-lg font-bold text-text-main mb-6 px-2">Панель керування</h2>
      
      <nav className="space-y-2">
        {navItems.map((item) => (
          <button
            key={item.id}
            onClick={() => setActiveTab(item.id as 'users' | 'invites' | 'projects')}
            className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg font-medium transition-all duration-200 cursor-pointer ${
              activeTab === item.id
                ? 'bg-primary text-white shadow-md'
                : 'text-text-muted hover:bg-gray-50 hover:text-text-main'
            }`}
          >
            <item.icon className="w-5 h-5" />
            {item.label}
          </button>
        ))}
      </nav>
    </aside>
  );
}