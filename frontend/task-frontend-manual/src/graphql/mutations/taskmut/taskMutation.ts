import { gql } from '@apollo/client';

export const CREATE_TASK = gql`
    mutation CreateTask($input: CreateTaskInput!) {
        createTask(input: $input) {
            id
            projectId
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

export const ASSIGN_USER_TO_TASK = gql`
    mutation AssignUserToTask(
        $taskId: UUID!
        $userId: UUID!
        $estimatedHours: Decimal!
    ) {
        assignUserToTask(
            taskId: $taskId
            userId: $userId
            estimatedHours: $estimatedHours
        )
    }
`;

export const UPDATE_TASK_STATUS = gql`
    mutation UpdateTaskStatus(
        $taskId: UUID!
        $statusId: UUID!
    ) {
        updateTaskStatus(
            taskId: $taskId
            statusId: $statusId
        ) {
            id
            statusId
            updatedAt
        }
    }
`;

export const DELETE_TASK_MUTATION = gql`
  mutation DeleteTask($taskId: UUID!) {
    deleteTask(taskId: $taskId)
  }
`;