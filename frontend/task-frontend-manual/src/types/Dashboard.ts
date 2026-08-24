export interface MyProjectDashboard {
    id: string;
    title: string;
    description?: string | null;
    budgetHours?: number | null;
    status: string;
    myRole: string;
    usedHours: number;
}

export interface AvailableProjectDashboard {
    id: string;
    title: string;
    description?: string | null;
    status: string;
    managerId: string;
    managerName: string;
    managerEmail: string;
}

export interface RoleHours {
    roleName: string;
    usedHours: number;
}

export interface ManagerProjectDashboard {
    id: string;
    title: string;
    status: string;
    budgetHours: number;
    usedHours: number;
    rolesHours: RoleHours[];
}

export interface DashboardStats {
    totalProjects: number;
    totalBudgetHours: number;
    totalUsedHours: number;
    projectsOnTrack: number;
    projectsAtRisk: number;
    projectsOverBudget: number;
}