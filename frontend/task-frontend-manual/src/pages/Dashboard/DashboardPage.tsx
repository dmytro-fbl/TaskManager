import { ProjectsPage } from "../Project/ProjectPage";

export default function DashboardPage() {
 

  return (
    <div className="flex flex-col gap-6">
      <div className="bg-bg-card p-8 rounded-2xl shadow-sm border border-gray-100">
        <h2 className="text-2xl font-bold text-text-main mb-2">
          Вітаємо в системі
        </h2>
        <p className="text-text-muted text-lg">
          Ви успішно увійшли в систему. Тут незабаром з'являться ваші проєкти та завдання.
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="bg-bg-card p-6 rounded-2xl shadow-sm border border-gray-100 min-h-[150px] flex items-center justify-center border-dashed border-2 border-gray-200">
          <span className="text-gray-400 font-medium">Мої проєкти (скоро)</span>
          <button onClick={ProjectsPage}>осьо</button>
        </div>
        <div className="bg-bg-card p-6 rounded-2xl shadow-sm border border-gray-100 min-h-[150px] flex items-center justify-center border-dashed border-2 border-gray-200">
          <span className="text-gray-400 font-medium">Мої завдання (скоро)</span>
        </div>
      </div>
    </div>
  );
};