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

export const GET_MY_PROJECTS_DASHBOARD = gql`
  query GetMyProjectsDashboard {
    myProjectsDashboard {
      id
      title
      description
      budgetHours
      status
      myRole
      usedHours
    }
  }
`;

export const GET_AVAILABLE_PROJECTS_DASHBOARD = gql`
  query GetAvailableProjectsDashboard {
    availableProjectsDashboard {
      id
      title
      description
      status
      managerId
      managerName
      managerEmail
    }
  }
`;

export const GET_MANAGER_PROJECTS_DASHBOARD = gql`
  query GetManagerProjectsDashboard {
    managerProjectsDashboard {
      id
      title
      status
      budgetHours
      usedHours
      rolesHours {
        roleName
        usedHours
      }
    }
  }
`;

export const GET_DASHBOARD_STATS = gql`
  query GetDashboardStats {
    dashboardStats {
      totalProjects
      totalBudgetHours
      totalUsedHours
      projectsOnTrack
      projectsAtRisk
      projectsOverBudget
    }
  }
`;