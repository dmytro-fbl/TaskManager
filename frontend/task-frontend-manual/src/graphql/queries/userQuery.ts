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