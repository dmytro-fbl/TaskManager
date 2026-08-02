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