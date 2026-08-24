import { useState } from "react";
import { useMutation } from "@apollo/client/react";
import { gql } from "@apollo/client";
import { ADD_PROJECT_HOURS } from "../../graphql/mutations/projectMutation";

type AddProjectHoursFormProps = {
    projectId: string;
    onAdded?: () => void;
};

const ADD_PROJECT_HOURS_MUTATION = gql`
  mutation AddProjectHours($input: AddProjectHoursInput!) {
    addProjectHours(input: $input)
  }
`;

export function AddProjectHoursForm({ projectId, onAdded }: AddProjectHoursFormProps) {
    const [hours, setHours] = useState("");
    const [description, setDescription] = useState("");
    const [error, setError] = useState("");

    const [addProjectHours, { loading }] = useMutation(ADD_PROJECT_HOURS_MUTATION);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError("");

        const hoursNum = parseFloat(hours);
        if (isNaN(hoursNum) || hoursNum <= 0) {
            setError("Вкажи коректну кількість годин (> 0).");
            return;
        }

        try {
            await addProjectHours({
                variables: {
                    input: {
                        projectId,
                        hours: hoursNum,
                        description: description.trim() || null,
                    },
                },
            });

            setHours("");
            setDescription("");
            onAdded?.();
        } catch (err: any) {
            setError(err.message || "Не вдалося додати години.");
        }
    };

    return (
        <form onSubmit={handleSubmit} className="bg-bg-card p-4 rounded-xl border border-gray-100 space-y-3">
            <h3 className="text-lg font-semibold text-text-main">Додати години на проєкт</h3>

            {error && (
                <div className="text-sm text-red-600 bg-red-50 p-2 rounded border border-red-100">
                    {error}
                </div>
            )}

            <div>
                <label className="block text-sm font-medium text-text-main mb-1">
                    Години
                </label>
                <input
                    type="number"
                    step="0.1"
                    min="0.1"
                    value={hours}
                    onChange={(e) => setHours(e.target.value)}
                    className="w-full rounded-lg border border-gray-300 px-3 py-2 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
                    placeholder="Наприклад: 5.5"
                    required
                />
            </div>

            <div>
                <label className="block text-sm font-medium text-text-main mb-1">
                    Опис (опціонально)
                </label>
                <input
                    type="text"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    className="w-full rounded-lg border border-gray-300 px-3 py-2 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
                    placeholder="Наприклад: Робота над дизайном"
                />
            </div>

            <button
                type="submit"
                disabled={loading}
                className="rounded-lg bg-blue-600 px-5 py-2.5 text-sm font-medium text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-50"
            >
                {loading ? "Додавання..." : "Додати години"}
            </button>
        </form>
    );
}