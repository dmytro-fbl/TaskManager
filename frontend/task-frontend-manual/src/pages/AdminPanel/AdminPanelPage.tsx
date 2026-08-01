import { useState } from "react";
import AdminSidebar from './components/AdminSidebar';
import UsersTab from "./components/tabs/UsersTab";
import PendingInviteTab from "./components/tabs/PendingInviteTab";
import { CreateProjectForm } from "../Dashboard/projects/CreateProjectForm";

interface GenerateInviteData {
  generateInvite: string;
}

export default function AdminPanelPage() {

const [activeTab, setActiveTab] = useState<'users' | 'invites' | 'projects'>('users');

  return (
    <div className="flex min-h-[calc(100vh-4rem)] bg-bg-main">
      <AdminSidebar activeTab={activeTab} setActiveTab={setActiveTab} />
      <main className="flex-1 p-6 md:p-8 overflow-y-auto">
        <div className="max-w-7xl mx-auto space-y-6">

          {/* Динамічне відображення активного блоку */}
          {activeTab === 'users' && <UsersTab />}
          {activeTab === 'invites' && <PendingInviteTab/>}
          {activeTab === 'projects' && <CreateProjectForm />}

        </div>
      </main>
    </div>
  );

}

