import { gql } from '@apollo/client';

export const LOGOUT_MUTATION = gql`
  mutation Logout {
    logout
  }
`;

export const LOGIN_MUTATION = gql`
  mutation Login($email: String!, $password: String!) {
    login(request: { email: $email, password: $password }) {
      accessToken,
      refreshToken

    }
  }
`;