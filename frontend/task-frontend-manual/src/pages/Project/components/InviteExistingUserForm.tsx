import React, { useState } from "react";
import { useMutation } from "@apollo/client/react";
import { ADD_USER_TO_PROJECT } from "../../../graphql/mutations/project/projectInviteMutations"; 
import ErrorMessage from "../../../components/ui/ErrorMessage";
import { getFriendlyErrorMessage } from "../../../utils/errorHandler";
import { FiUserPlus } from "react-icons/fi";

interface AddUserToProjectFormProps {
    projectId: string;
    onUserAdded: () => void; 
}

export const AddUserToProjectForm: React.FC<AddUserToProjectFormProps> = ({ projectId, onUserAdded }) => {
    const [email, setEmail] = useState("");
    const [projectRole, setProjectRole] = useState<"manager" | "contributor">("contributor");
    const [localError, setLocalError] = useState<string | null>(null);

    const [addUserDirectly, { loading, error }] = useMutation(ADD_USER_TO_PROJECT);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setLocalError(null);

        if (email.trim().length < 5 || !email.includes('@')) {
            setLocalError("Вкажіть коректний email.");
            return;
        }

        try {
            await addUserDirectly({
                variables: {
                    projectId,
                    email: email.trim(),
                    projectRole,
                },
            });

            setEmail("");
            setProjectRole("contributor");
            onUserAdded(); 
            
        } catch (err) {
            console.error("Помилка додавання користувача:", err);
        }
    };

    const displayError = localError || (error ? getFriendlyErrorMessage(error as any) : null);

    return (
        <form onSubmit={handleSubmit} className="p-6 mb-6 border border-gray-100 shadow-sm bg-bg-card rounded-xl">
            <h3 className="mb-4 text-lg font-bold text-text-main">Додати учасника до команди</h3>

            {displayError && <ErrorMessage message={displayError} />}

            <div className="flex flex-col gap-4 md:flex-row md:items-end">
                <div className="flex-1">
                    <label className="block mb-1 text-sm font-medium text-gray-700">Email користувача</label>
                    <input
                        type="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        required
                        placeholder="user@example.com"
                        className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                    />
                </div>

                <div className="w-full md:w-48">
                    <label className="block mb-1 text-sm font-medium text-gray-700">Роль</label>
                    <select
                        value={projectRole}
                        onChange={(e) => setProjectRole(e.target.value as "manager" | "contributor")}
                        className="w-full px-4 py-2 bg-gray-50 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                    >
                        <option value="contributor">Виконавець</option>
                        <option value="manager">Менеджер</option>
                    </select>
                </div>

                <button
                    type="submit"
                    disabled={loading || !email}
                    className="flex items-center justify-center px-6 py-2 text-white transition-colors rounded-lg h-[42px] bg-primary hover:bg-blue-700 disabled:opacity-50"
                >
                    <FiUserPlus className="mr-2" />
                    {loading ? "Додаємо..." : "Додати"}
                </button>
            </div>
        </form>
    );
};