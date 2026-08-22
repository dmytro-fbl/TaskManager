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
      projectId
      userId
      projectRole
      roleLabelId
      joinedAt
      user {
        id
        name
        email
        isAdmin
      }
    }
  }
`;

export const GET_PROJECT_DETAILS = gql`
  query GetProjectDetails($id: UUID!) {
    project(id: $id) {
      id
      title
      description
      budgetCap
      deadline
      status
      isArchived
      createdAt
    }
  }
`;

export const GET_PROJECT_STATUSES = gql`
    query GetProjectStatuses($projectId: UUID!) {
        projectStatuses(projectId: $projectId) {
            id
            projectId
            name
            category
            color
            sortOrder
            isFinal
        }
    }
`;

export const GET_PROJECT_ROLES = gql`
    query GetProjectRoles($projectId: UUID!) {
        projectRoles(projectId: $projectId) {
            id
            projectId
            name
        }
    }
`;

export const GET_PROJECTS = gql`
  query GetProjects {
    myProjects {
      id
      title
      description
      budgetCap
      status
      ownerId
      isArchived
      createdAt
      updatedAt
    }
  }
`;

export const GET_PROJECT_DETAILS_FOR_TASK = gql`
    query GetProjectDetailsForTask($projectId: UUID!) {
        projectTasks(projectId: $projectId) {
            id
            projectId 
            authorId 
            assigneeIds 
            statusId 
            title 
            notes 
            priority 
            startDate 
            dueDate  
            createdAt 
            updatedAt
        }
        projectStatuses(projectId: $projectId) { 
            id 
            name 
            category 
            color 
        }
        projectMemberships(projectId: $projectId) { 
            userId
            user {
                id 
                name 
                email
            } 
        }
        projectRoles(projectId: $projectId){
            id
            name
        }
    }
`;
