import { gql } from "@apollo/client";

export const VERIFY_TOKEN_QUERY = gql`
    query VerifyInviteToken($token: String!){
        verifyInviteToken(token: $token)
    }
`;

export const GET_ME_QUERY = gql`
  query GetMe {
    me {
      id
      name
      isAdmin
    }
  }
`;