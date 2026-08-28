import React, { useState, useEffect } from 'react';
import { useMutation } from '@apollo/client/react';
import { UPDATE_PROJECT } from '../../../graphql/mutations/project/projectMutation';
import { getFriendlyErrorMessage } from '../../../utils/errorHandler';
import ErrorMessage from '../../../components/ui/ErrorMessage';

interface EditProjectModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  project: {
    id: string;
    title: string;
    description?: string | null;
    budgetCap?: number | null;
    deadline?: string | null;
  };
}

export const EditProjectModal: React.FC<EditProjectModalProps> = ({
  isOpen,
  onClose,
  onSuccess,
  project,
}) => {
  const [title, setTitle] = useState(project.title);
  const [description, setDescription] = useState(project.description || '');
  const [budgetCap, setBudgetCap] = useState(project.budgetCap ? project.budgetCap.toString() : '');

  const [deadline, setDeadline] = useState(project.deadline ? new Date(project.deadline).toISOString().split('T')[0] : '');

  useEffect(() => {
    setTitle(project.title);
    setDescription(project.description || '');
    setBudgetCap(project.budgetCap ? project.budgetCap.toString() : '');
    setDeadline(project.deadline ? new Date(project.deadline).toISOString().split('T')[0] : '');
  }, [project]);

  const [updateProject, { loading, error }] = useMutation(UPDATE_PROJECT);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    let parsedDeadline = null;
    if (deadline.trim()) {
      parsedDeadline = new Date(deadline).toISOString();
    }

    try {
      await updateProject({
        variables: {
          projectId: project.id,
          title,
          description: description.trim() === '' ? null : description,
          budgetCap: budgetCap ? parseFloat(budgetCap) : null,
          deadline: parsedDeadline,
        },
      });

      onSuccess();
      onClose();
    } catch (err) {
      console.error('Помилка оновлення проєкту:', err);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div className="w-full max-w-md p-6 bg-white rounded-xl shadow-lg">
        <h2 className="mb-4 text-xl font-bold text-gray-800">Редагувати проєкт</h2>

        {error && <ErrorMessage message={getFriendlyErrorMessage(error as any) ?? 'Помилка'} />}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block mb-1 text-sm font-medium text-gray-700">Назва</label>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
              required
            />
          </div>

          <div>
            <label className="block mb-1 text-sm font-medium text-gray-700">Опис</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
              rows={3}
            />
          </div>

          <div className="flex gap-4">
            <div className="flex-1">
              <label className="block mb-1 text-sm font-medium text-gray-700">Бюджет (години)</label>
              <input
                type="number"
                step="0.01"
                value={budgetCap}
                onChange={(e) => setBudgetCap(e.target.value)}
                className="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
              />
            </div>

            <div className="flex-1">
              <label className="block mb-1 text-sm font-medium text-gray-700">Дедлайн</label>
              <input
                type="date"
                value={deadline}
                onChange={(e) => setDeadline(e.target.value)}
                className="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
              />
            </div>
          </div>

          <div className="flex justify-end gap-3 mt-6">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-gray-600 bg-gray-100 rounded-lg hover:bg-gray-200"
              disabled={loading}
            >
              Скасувати
            </button>
            <button
              type="submit"
              disabled={loading || !title.trim()}
              className="px-4 py-2 text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-50"
            >
              {loading ? 'Збереження...' : 'Зберегти'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};