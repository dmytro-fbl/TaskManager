import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { ApolloClient, InMemoryCache, HttpLink } from '@apollo/client'
import { setContext } from '@apollo/client/link/context';
import { ApolloProvider } from '@apollo/client/react'
import App from './App.tsx'
import './index.css'

const httpLink = new HttpLink({
  uri: 'https://localhost:7190/graphql'
});

const authLink = setContext((_, { headers }) => {
  const token = localStorage.getItem('token');
  return {
    headers: {
      ...headers,
      authorization: token ? `Bearer ${token}` : "",
    }
  }
});

// 👇 ВИПРАВЛЕНО ТУТ 👇
const client = new ApolloClient({
  link: authLink.concat(httpLink), // Спочатку додаємо токен, потім робимо запит
  cache: new InMemoryCache(),
});

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ApolloProvider client={client}>
      <App />
    </ApolloProvider>
  </StrictMode>,
)