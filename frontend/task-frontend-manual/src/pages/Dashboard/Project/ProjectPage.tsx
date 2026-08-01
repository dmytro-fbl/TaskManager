import React, {useState} from "react";
import { CreateProjectForm } from "./CreateProjectForm";

export const ProjectsPage: React.FC = () => {
    const [showCreateForm, setShowCreateForm] = useState(false);

    return (
        <div className="container px-4 py-8 mx-auto max-w-7xl">
            {/* Хедер сторінки */}
            <div className="flex items-center justify-between mb-8">
                <h1 className="text-3xl font-bold text-gray-900">Мої проєкти</h1>
                <button
                    onClick={() => setShowCreateForm(!showCreateForm)}
                    className={`px-4 py-2 text-white transition-colors rounded-lg ${
                        showCreateForm 
                            ? 'bg-gray-500 hover:bg-gray-600' 
                            : 'bg-blue-600 hover:bg-blue-700'
                    }`}
                >
                    {showCreateForm ? 'Скасувати' : '+ Створити проєкт'}
                </button>
            </div>

            {showCreateForm && (
                <div className="mb-10">
                    <CreateProjectForm />
                </div>
            )}

            {/* Сітка з проєктами */}
            <h2 className="mb-4 text-xl font-semibold text-gray-700">Активні проєкти</h2>
            
            <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
                {/* Тут пізніше буде map() по даних з GraphQL, 
                  а поки що - плейсхолдер карточки проєкту 
                */}
                <div className="p-6 transition-shadow bg-white border border-gray-100 shadow-sm rounded-xl hover:shadow-md cursor-pointer">
                    <div className="flex items-start justify-between mb-4">
                        <h3 className="text-xl font-semibold text-gray-800">Приклад проєкту</h3>
                        <span className="px-2 py-1 text-xs font-medium text-green-700 bg-green-100 rounded-full">
                            active
                        </span>
                    </div>
                    <p className="mb-4 text-sm text-gray-600 line-clamp-2">
                        Опис проєкту буде виводитися тут. Якщо він занадто довгий, він обріжеться завдяки класу line-clamp-2.
                    </p>
                    <div className="flex items-center justify-between text-sm text-gray-500 border-t pt-4 border-gray-50">
                        <span>Бюджет: $5000</span>
                        <span className="font-medium text-blue-600 hover:text-blue-800">
                            Відкрити &rarr;
                        </span>
                    </div>
                </div>
            </div>
        </div>
    );
};