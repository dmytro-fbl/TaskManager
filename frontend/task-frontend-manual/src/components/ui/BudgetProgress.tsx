type BudgetProgressProps = {
    usedHours: number;
    budgetHours?: number | null;
};

export function BudgetProgress({ usedHours, budgetHours }: BudgetProgressProps) {
    if (budgetHours == null || budgetHours <= 0) {
        return (
            <div className="text-sm text-text-muted">
                Бюджет не вказано
            </div>
        );
    }

    const percent = (usedHours / budgetHours) * 100;
    const clampedPercent = Math.min(100, Math.max(0, percent));

    let colorClass = "bg-green-500";

    if (percent >= 100) {
        colorClass = "bg-red-500";
    } else if (percent >= 80) {
        colorClass = "bg-yellow-500";
    }

    return (
        <div className="w-full">
            <div className="flex justify-between text-xs text-text-muted mb-1">
                <span>Використано: {usedHours.toFixed(1)} год</span>
                <span>Бюджет: {budgetHours.toFixed(1)} год ({percent.toFixed(1)}%)</span>
            </div>

            <div className="h-2 w-full bg-gray-200 rounded-full overflow-hidden">
                <div
                    className={`h-full ${colorClass} transition-all`}
                    style={{ width: `${clampedPercent}%` }}
                />
            </div>
        </div>
    );
}