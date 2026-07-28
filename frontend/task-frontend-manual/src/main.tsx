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

const errorLink = onError((error: any) => {
  const { graphQLErrors, operation, forward } = error;

  if (graphQLErrors) {
    const isUnautorized = graphQLErrors.some(
      (err: any) => err.extensions?.code === 'AUTH_NOT_AUTHENTICATED' || err.message.includes('401')
    );

    if (isUnautorized) {
      return new Observable((observer) => {
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
          body: 
          JSON.stringify({
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
                headers: {
                  ...oldHeaders,
                  authorization: `Bearer ${newTokens.accessToken}`,
                },
              });

              const subscriber = {
                next: observer.next.bind(observer),
                error: observer.error.bind(observer),
                complete: observer.complete.bind(observer),
              };
              forward(operation).subscribe(subscriber);
            } else {
              localStorage.clear();
              window.location.href = '/login';
            }
          })
          .catch(err => {
            console.error("Помилка оновлення токена", err);
            localStorage.clear();
            window.location.href = '/login';
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
)