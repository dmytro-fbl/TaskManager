import { gql } from '@apollo/client';

export const GET_ADMIN_PROJECTS = gql`
  query GetAdminProjects {
    adminProjects {
      id
      title
      description
      budgetCap
      status
      isArchived
      createdAt
      deadline
      ownerId
      ownerName
      ownerEmail
    }
  }
`;

export const GET_PROJECT_MEMBERSHIPS = gql`
  query GetProjectMemberships($projectId: UUID!) {
    projectMemberships(projectId: $projectId) {
      id
      userId
      projectRole
      joinedAt
      user {
        id
        name
        email
        isActive
      }
    }
  }
`;