import { gql } from "@apollo/client";

export const GENERATE_INVITE_MUTATIONS = gql`
    mutation GenerateInviteAsync($email: String!, $isAdmin: Boolean!){
        generateInvite(email: $email, isAdmin: $isAdmin)
    }
`;

export const COMPLETE_REGISTRATION_MUTATION = gql`
  mutation CompleteRegistration($token: String!, $name: String!, $password: String!) {
    completeRegistration(token: $token, name: $name, password: $password)
  }
`;