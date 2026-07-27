import { useState } from "react";
import { disableExperimentalFragmentVariables, gql } from "@apollo/client";
import { useMutation } from '@apollo/client/react';
import { GENERATE_INVITE_MUTATIONS } from '../../graphql/mutations/invateMutations';
import ErrorMessage from "../../components/ui/ErrorMessage";
import { getFriendlyErrorMessage } from "../../utils/errorHandler";

import AdminSidebar from './components/AdminSidebar';
import UsersTab from "./components/tabs/UsersTab";
import InvitesTab from "./components/tabs/InvatesTab";

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
          {activeTab === 'invites' && <InvitesTab/>}
          {activeTab === 'projects' && <div>Вкладка проєктів (в розробці...)</div>}

        </div>
      </main>
    </div>
  );

  const [email, setEmail] = useState('');
  const [isAdmin, setIsAdmin] = useState(false);
  const [generatedLink, setGeneratelink] = useState<string | null>(null);

  const [generateInvite, { loading, error }] = useMutation<GenerateInviteData>(GENERATE_INVITE_MUTATIONS);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setGeneratelink(null);

    try {
      const response = await generateInvite({
        variables: { email, isAdmin }
      });


      if (response.data?.generateInvite) {
        const token = response.data.generateInvite;

        const link = `${window.location.origin}/register?token=${token}`;
        setGeneratelink(link);
        setIsAdmin(false);
        setEmail('');
      }
    } catch (err) {
      console.error("Помилка генерації запрошення: ", err);
    }
  };

  const handleCopy = () => {
    if (generatedLink) {
      navigator.clipboard.writeText(generatedLink);
      alert('Посилання скопійовано в буфер обміну');
    }
  };

}

