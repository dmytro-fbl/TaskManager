import { gql } from "@apollo/client";

export const GET_TASK_COMMENTS = gql`
  query GetTaskComments($taskId: UUID!) {
    taskComments(taskId: $taskId) {
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

export const GET_TASK_COMMENT_VERSIONS = gql`
  query GetTaskCommentVersions($commentId: UUID!) {
    taskCommentVersions(commentId: $commentId) {
      id
      commentId
      previousBody
      changedAt
    }
  }
`;
