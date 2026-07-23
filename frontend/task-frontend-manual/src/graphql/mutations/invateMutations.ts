import { gql } from "@apollo/client";

export const GENERATE_INVITE_MUTATIONS = gql`
    mutation GenerateInviteAsync($email: String!, $isAdmin: Boolean!){
        generateInvite(email: $email, isAdmin: $isAdmin)
    }
`;