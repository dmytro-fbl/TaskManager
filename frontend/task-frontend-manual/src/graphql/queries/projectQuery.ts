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
      ownerId
      ownerName
      ownerEmail
    }
  }
`;