import { gql } from "@apollo/client";

export const UPDATE_PROJECT_MEMBER_ROLE = gql`
  mutation UpdateProjectMemberRole(
    $projectId: UUID!
    $userId: UUID!
    $projectRole: String!
    $roleLabelId: UUID
  ) {
    updateProjectMemberRole(
      projectId: $projectId
      userId: $userId
      projectRole: $projectRole
      roleLabelId: $roleLabelId
    )
  }
`;