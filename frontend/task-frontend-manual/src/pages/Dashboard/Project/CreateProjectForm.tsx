import React, { useState } from "react";
import { useMutation } from "@apollo/client/react";
import { CREATE_PROJECT } from "../../../graphql/mutations/projectMutation";
import { getFriendlyErrorMessage } from "../../../utils/errorHandler";
import ErrorMessage from "../../../components/ui/ErrorMessage";

export const CreateProjectForm: React.FC = () => {

    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');
    const [budgetCap, setBudgetCap] = useState<string>('');

    const [localError, setLocalError] = useState<string | null>(null);

    const [createProject, { loading, error }] = useMutation(CREATE_PROJECT, {
        refetchQueries: ['GetProjects'],
    });


    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setLocalError(null);

        if(title.trim().length < 3){
            setLocalError("Назва проекту має містити щонайменше 3 символи");
            return;
        }

        const parsedBudget = budgetCap ? parseFloat(budgetCap) : null;
        if(parsedBudget !== null && parsedBudget < 0){
            setLocalError("Бюджет не може бути від'ємним.");
        }
        
        try {
            await createProject({
                variables: {
                    input: {
                        title,
                        description,
                        budgetCap: budgetCap ? parseFloat(budgetCap) : null,
                    },
                },
            });

            setTitle('');
            setDescription('');
            setBudgetCap('');
            alert('Проект успішно створено!');

        } catch (err){
            console.error("Помилка створення проекту: ", err);
        }
    };

    const displayError = localError || (error ? getFriendlyErrorMessage(error) : null);

    return (
        <div className="max-w-md p-6 mx-auto mt-10 bg-white rounded-xl shadow-md">
            <h2 className="mb-6 text-2xl font-bold text-gray-800">Створити новий проєкт</h2>

            <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                    <label className="block mb-1 text-sm font-medium text-gray-700">Назва проєкту *</label>
                    <input
                        type="text"
                        required
                        value={title}
                        onChange={(e) => setTitle(e.target.value)}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                        placeholder="Наприклад: Редизайн сайту"
                    />
                </div>

                <div>
                    <label className="block mb-1 text-sm font-medium text-gray-700">Опис</label>
                    <textarea
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                        placeholder="Коротко про цілі проєкту..."
                        rows={3}
                    />
                </div>

                <div>
                    <label className="block mb-1 text-sm font-medium text-gray-700">Бюджет ($)</label>
                    <input
                        type="number"
                        step="0.01"
                        value={budgetCap}
                        onChange={(e) => setBudgetCap(e.target.value)}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                        placeholder="5000.00"
                    />
                </div>

                {displayError && <ErrorMessage message={displayError}/>}

                <button
                    type="submit"
                    disabled={loading || !title.trim()}
                    className="w-full px-4 py-2 text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                    {loading ? 'Створення...' : 'Створити проєкт'}
                </button>
            </form>
        </div>
    );
};