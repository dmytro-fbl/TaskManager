import { gql } from "@apollo/client";

export const VERIFY_TOKEN_QUERY = gql`
    query VerifyInviteToken($token: String!){
        verifyInviteToken(token: $token)
    }
`;

