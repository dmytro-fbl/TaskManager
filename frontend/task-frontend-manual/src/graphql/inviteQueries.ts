import { gql } from "@apollo/client";

export const VERIFY_TOKEN_QUERY = gql`
    query VerifyInviteToken($token: String!){
        verifyInviteToken(token: $token)
    }
`;

export const COMPLETE_REGISTRATION_MUTATION = gql`
  mutation CompleteRegistration($token: String!, $name: String!, $password: String!) {
    completeRegistration(token: $token, name: $name, password: $password)
  }
`;