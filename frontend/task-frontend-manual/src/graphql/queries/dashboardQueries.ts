import { gql } from 'graphql-tag';

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