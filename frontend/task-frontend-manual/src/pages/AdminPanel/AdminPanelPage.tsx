import { useState } from "react";
import { disableExperimentalFragmentVariables, gql } from "@apollo/client";
import { useMutation } from '@apollo/client/react';
import { GENERATE_INVITE_MUTATIONS } from '../../graphql/mutations/invateMutations';
import ErrorMessage from "../../components/ui/ErrorMessage";

interface GenerateInviteData {
    generateInvite: string;
}

export default function InviteUserForm() {
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

   return (
    <div className="max-w-[400px] p-6 mt-6 bg-bg-card rounded-xl shadow-md border border-gray-100">
      <h3 className="text-xl font-bold text-text-main mb-6">
        Запросити нового користувача
      </h3>
      
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        
        {error && <ErrorMessage message={`Помилка: ${error.message}`} />}

        <div>
          <label className="block text-sm font-medium text-text-main mb-1">
            Email користувача:
          </label>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            className="w-full p-2.5 border border-gray-300 rounded-md outline-none focus:border-transparent focus:ring-2 focus:ring-primary transition"
          />
        </div>

        <div className="flex items-center gap-3 my-2">
          <input
            type="checkbox"
            id="isAdmin"
            checked={isAdmin}
            onChange={(e) => setIsAdmin(e.target.checked)}
            className="w-4 h-4 text-primary bg-gray-100 border-gray-300 rounded focus:ring-primary cursor-pointer"
          />
          <label htmlFor="isAdmin" className="text-sm font-medium text-text-main cursor-pointer select-none">
            Надати права Адміністратора
          </label>
        </div>

        <button 
          type="submit" 
          disabled={loading} 
          className="w-full p-2.5 bg-primary text-white font-semibold rounded-md hover:bg-primary-hover transition duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading ? 'Генерація...' : 'Створити запрошення'}
        </button>
      </form>

      {generatedLink && (
        <div className="mt-6 p-4 bg-blue-50 border border-blue-200 rounded-lg">
          <p className="text-sm font-bold text-text-main mb-3">
            Одноразове посилання створено:
          </p>
          <input
            type="text"
            readOnly
            value={generatedLink}
            className="w-full p-2.5 mb-3 border border-blue-200 rounded-md bg-white text-gray-700 outline-none"
          />
          <button 
            onClick={handleCopy} 
            type="button"
            className="w-full p-2.5 bg-white border border-primary text-primary font-semibold rounded-md hover:bg-primary hover:text-white transition duration-200"
          >
            Скопіювати посилання
          </button>
        </div>
      )}
    </div>
  );
}

