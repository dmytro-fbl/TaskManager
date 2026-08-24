export interface AddProjectHoursInput {
    projectId: string;
    hours: number;
    description?: string | null;
}