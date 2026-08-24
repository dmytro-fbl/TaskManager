import { gql } from '@apollo/client';

export const INVITE_EXISTING_USER_TO_PROJECT = gql`
      mutation InviteExistingUserToProject(
        $projectId: UUID!
        $email: String!
        $projectRole: String!
        $roleLabelId: UUID
      ) {
        inviteExistingUserToProject(
          projectId: $projectId
          email: $email
          projectRole: $projectRole
          roleLabelId: $roleLabelId
        )
      }

`;

export const ADD_USER_TO_PROJECT = gql`
  mutation AddUserToProjectDirectly($projectId: UUID!, $email: String!, $projectRole: String!) {
    addUserToProjectDirectly(projectId: $projectId, email: $email, projectRole: $projectRole)
  }
`;

export const UPDATE_MEMBER_ROLE = gql`
  mutation UpdateProjectMemberRole($projectId: UUID!, $userId: UUID!, $projectRole: String!) {
    updateProjectMemberRole(projectId: $projectId, userId: $userId, projectRole: $projectRole)
  }
`;