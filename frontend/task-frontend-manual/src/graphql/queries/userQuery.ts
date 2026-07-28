import { gql } from "@apollo/client";

export const GET_USERS_QUERY = gql`
query Getusers{
    users{
    id
    name
    email
    isAdmin
    isActive
    createdAt
    }
}
`;

export const GET_PENDING_INVITES_QUERY = gql`
query GetPendingInvites {
    pendingInviteUsers {
        id 
        email
        isAdmin
        inviteToken
        inviteExpiresAt
        createdAt
    }
}
`