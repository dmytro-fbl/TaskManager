import { DashboardStatsCard } from "../../components/Dashboard/DashboardStats";
import { MyProjectsSection } from "../../components/Dashboard/MyProjectsSection";
import { AvailableProjectsSection } from "../../components/Dashboard/AvailableProjectsSection";
import { ManagerProjectsSection } from "../../components/Dashboard/ManagerProjectsSection";

export default function DashboardPage() {
    return (
        <div className="flex flex-col gap-6">
            <div className="bg-bg-card p-8 rounded-2xl shadow-sm border border-gray-100">
                <h2 className="text-2xl font-bold text-text-main mb-2">
                    Вітаємо в системі
                </h2>
                <p className="text-text-muted text-lg">
                    Ви успішно увійшли в систему. Тут твої проєкти та статистика.
                </p>
            </div>

            <DashboardStatsCard />

            <MyProjectsSection />

            <AvailableProjectsSection />

            <ManagerProjectsSection />
        </div>
    );
}