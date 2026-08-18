import { gql } from "@apollo/client";

export const CREATE_TASK_COMMENT = gql`
  mutation CreateTaskComment($input: CreateTaskCommentInput!) {
    createTaskComment(input: $input) {
      id
      taskId
      authorId
      parentCommentId
      body
      createdAt
      updatedAt
      isEdited
      isDeleted
    }
  }
`;

export const UPDATE_TASK_COMMENT = gql`
  mutation UpdateTaskComment($input: UpdateTaskCommentInput!) {
    updateTaskComment(input: $input) {
      id
      body
      updatedAt
      isEdited
    }
  }
`;

export const DELETE_TASK_COMMENT = gql`
  mutation DeleteTaskComment($input: DeleteTaskCommentInput!) {
    deleteTaskComment(input: $input)
  }
`;