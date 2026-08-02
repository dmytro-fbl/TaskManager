import React, { useState } from "react";
import { useMutation } from "@apollo/client/react";
import { INVITE_EXISTING_USER_TO_PROJECT } from "../../../graphql/mutations/projectInviteMutations";
import ErrorMessage from "../../../components/ui/ErrorMessage";
import { getFriendlyErrorMessage } from "../../../utils/errorHandler";

interface InviteExistingUserFormProps {
    projectId: string;
}

export const InviteExistingUserForm: React.FC<InviteExistingUserFormProps> = ({ projectId }) => {
    const [email, setEmail] = useState("");
    const [projectRole, setProjectRole] = useState<"manager" | "contributor">("contributor");
    const [roleLabelId, setRoleLabelId] = useState<string | null>(null);
    const [localError, setLocalError] = useState<string | null>(null);

    const [inviteExistingUser, { loading, error }] = useMutation(INVITE_EXISTING_USER_TO_PROJECT);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setLocalError(null);

        if (email.trim().length < 5) {
            setLocalError("Вкажіть коректний email.");
            return;
        }

        try {
            await inviteExistingUser({
                variables: {
                    projectId,
                    email,
                    projectRole,
                    roleLabelId: roleLabelId || null,
                },
            });

            alert("Запрошення успішно створено! Лінк можна буде відправити користувачу.");
            setEmail("");
            setProjectRole("contributor");
            setRoleLabelId(null);
        } catch (err) {
            console.error("Помилка запрошення користувача в проєкт: ", err);
        }
    };

    const displayError = localError || (error ? getFriendlyErrorMessage(error as any) : null);

    return (
        <form
            onSubmit={handleSubmit}
            className="space-y-4 bg-bg-card p-4 rounded-xl border border-gray-100 mt-8"
        >
            <h3 className="text-lg font-semibold text-text-main">Запросити існуючого користувача в проєкт</h3>

            {displayError && <ErrorMessage message={displayError} />}

            <div>
                <label className="block mb-1 text-sm font-medium text-gray-700">Email користувача</label>
                <input
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                />
            </div>

            <div>
                <label className="block mb-1 text-sm font-medium text-gray-700">Роль у проєкті</label>
                <select
                    value={projectRole}
                    onChange={(e) => setProjectRole(e.target.value as "manager" | "contributor")}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                >
                    <option value="contributor">Учасник (contributor)</option>
                    <option value="manager">Менеджер (manager)</option>
                </select>
            </div>

            <button
                type="submit"
                disabled={loading}
                className="px-4 py-2 text-white bg-primary rounded-lg hover:bg-primary-hover disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
                {loading ? "Надсилаємо..." : "Запросити користувача"}
            </button>
        </form>
    );
};