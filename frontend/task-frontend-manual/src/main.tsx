import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { ApolloClient, InMemoryCache, HttpLink, createHttpLink, from, Observable } from '@apollo/client'
import { setContext } from '@apollo/client/link/context';
import { ApolloProvider } from '@apollo/client/react'
import { onError } from '@apollo/client/link/error';
import App from './App.tsx'
import './index.css'

const httpLink = createHttpLink({
  uri: 'https://localhost:7190/graphql'
});

const authLink = setContext((_, { headers }) => {
  const token = localStorage.getItem('accessToken');
  return {
    headers: {
      ...headers,
      authorization: token ? `Bearer ${token}` : "",
    }
  }
});

let isRefreshing = false;
let pendingRequests: (() => void)[] = [];

const resolvePendingRequests = () => {
  pendingRequests.forEach((callback) => callback());
  pendingRequests = [];
};

const errorLink = onError((errorOptions: any) => {
  const { graphQLErrors, networkError, operation, forward, error, result } = errorOptions;

  const actualGraphQLErrors = graphQLErrors || result?.errors || error?.errors;
  const errorMessage = error?.message || networkError?.message || "";

  let isUnauthorized = false;

  if (actualGraphQLErrors) {
    isUnauthorized = actualGraphQLErrors.some((err: any) => {
      const code = err.extensions?.code;
      const msg = err.message?.toLowerCase() || '';
      return code === 'AUTH_NOT_AUTHENTICATED' || code === 'HC0014' || msg.includes('not authorized') || msg.includes('401');
    });

    if (!isUnauthorized && errorMessage.toLowerCase().includes('not authorized')) {
      isUnauthorized = true;
    }

    if (isUnauthorized) {
      return new Observable((observer) => {

        if (isRefreshing) {
          pendingRequests.push(() => {
            const oldHeaders = operation.getContext().headers;
            operation.setContext({
              headers: { ...oldHeaders, authorization: `Bearer ${localStorage.getItem('accessToken')}` },
            });
            forward(operation).subscribe({
              next: observer.next.bind(observer),
              error: observer.error.bind(observer),
              complete: observer.complete.bind(observer),
            });
          });
          return;
        }

        isRefreshing = true;

        const refreshToken = localStorage.getItem('refreshToken');
        const email = localStorage.getItem('userEmail');

        if (!refreshToken || !email) {
          localStorage.clear();
          window.location.href = '/login';
          return;
        }

        fetch('https://localhost:7190/graphql', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            query: `
              mutation Refresh($email: String!, $refreshToken: String!) {
                refreshToken(email: $email, refreshToken: $refreshToken) {
                  accessToken
                  refreshToken
                }
              }
            `,
            variables: { email, refreshToken }
          })
        })
          .then(res => res.json())
          .then(res => {
            const newTokens = res.data?.refreshToken;

            if (newTokens?.accessToken && newTokens?.refreshToken) {
              localStorage.setItem('accessToken', newTokens.accessToken);
              localStorage.setItem('refreshToken', newTokens.refreshToken);

              const oldHeaders = operation.getContext().headers;
              operation.setContext({
                headers: { ...oldHeaders, authorization: `Bearer ${newTokens.accessToken}` },
              });

              forward(operation).subscribe({
                next: observer.next.bind(observer),
                error: observer.error.bind(observer),
                complete: observer.complete.bind(observer),
              });

              resolvePendingRequests();
            } else {
              pendingRequests = [];
              localStorage.clear();
              window.location.href = '/login';
            }
          })
          .catch(err => {
            pendingRequests = [];
            localStorage.clear();
            window.location.href = '/login';
          })
          .finally(() => {
            isRefreshing = false;
          });
      });
    }
  }
});

const client = new ApolloClient({
  link: from([errorLink, authLink, httpLink]),
  cache: new InMemoryCache(),
});

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ApolloProvider client={client}>
      <App />
    </ApolloProvider>
  </StrictMode>,
);