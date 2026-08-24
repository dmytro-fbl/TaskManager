import { gql } from "@apollo/client";

export const CREATE_PROJECT = gql `
    mutation CreateProject($input: CreateProjectInput!){
        createProject(input: $input){
            id
            title
            description
            budgetCap
            status
            deadline

        }
    }
`;

export const TOGGLE_PROJECT_ARCHIVE = gql`
  mutation ToggleProjectArchive($projectId: UUID!, $isArchived: Boolean!) {
    toggleProjectIsArchived(projectId: $projectId, isArchived: $isArchived)
  }
`;

export const UPDATE_PROJECT = gql`
  mutation UpdateProject(
    $projectId: UUID!
    $title: String!
    $description: String
    $budgetCap: Decimal
    $deadline: DateTime!
  ) {
    updateProject(
      projectId: $projectId
      title: $title
      description: $description
      budgetCap: $budgetCap
      deadline: $deadline
    )
  }
`;

export const REMOVE_PROJECT_MEMBER = gql`
  mutation RemoveProjectMember($projectId: UUID!, $memberUserId: UUID!) {
    removeProjectMember(
      projectId: $projectId, 
      memberUserId: $memberUserId
    )
  }
`;

