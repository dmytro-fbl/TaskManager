import { gql } from "@apollo/client";

export const GET_PROJECT_TASKS = gql`
    query GetProjectTasks($projectId: UUID!) {
        projectTasks(projectId: $projectId) {
            id
            projectId
            authorId
            assigneeId
            statusId
            title
            notes
            priority
            startDate
            dueDate
            createdAt
            updatedAt
        }
    }
`;

export const GET_TASK_WORKLOGS = gql`
    query GetTaskWorklogs($taskId: UUID!) {
        taskWorklogs(taskId: $taskId) {
            id taskId userId userName hoursSpent logDate comment
        }
    }
`;

export const LOG_WORK = gql`
    mutation LogWork($input: WorkLogInput!) {
        logWork(input: $input)
    }
`;

