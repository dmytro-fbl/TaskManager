import { gql } from "@apollo/client";

export const CREATE_PROJECT = gql `
    mutation CreateProject($input: CreateProjectInput!){
        createProject(input: $input){
            id
            title
            description
            budgetCap
            status
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
  ) {
    updateProject(
      projectId: $projectId
      title: $title
      description: $description
      budgetCap: $budgetCap
    )
  }
`;

export const REMOVE_PROJECT_MEMBER = gql`
  mutation RemoveProjectMember($projectId: UUID!, $userId: UUID!) {
    removeProjectMember(projectId: $projectId, userId: $userId)
  }
`;

export const ADD_PROJECT_HOURS = gql`
  mutation AddProjectHours($input: AddProjectHoursInput!) {
    addProjectHours(input: $input)
  }
`;