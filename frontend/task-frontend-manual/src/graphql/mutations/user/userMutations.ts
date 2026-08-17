import { gql } from "@apollo/client";

export const TOGGLE_USER_ROLE = gql`
    mutation ToggleUserRole($userId: UUID!, $isAdmin: Boolean!){
        toggleUserRole(userId: $userId, isAdmin: $isAdmin)
    }
`;

export const TOGGLE_USER_STATUS = gql`
    mutation ToggleUserSTATUS($userId: UUID!, $isActive: Boolean!){
        toggleUserStatus(userId: $userId, isActive: $isActive)
    }
`;