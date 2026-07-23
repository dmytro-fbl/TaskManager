import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { ApolloClient, InMemoryCache, HttpLink } from '@apollo/client'
import {ApolloProvider} from '@apollo/client/react'
import App from './App.tsx'
import './index.css'

const httpLink = new HttpLink({
  uri: 'https://localhost:7190/graphql'
});

const client = new ApolloClient({
  link: httpLink,
  cache: new InMemoryCache(),
});

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ApolloProvider client={client}>
      <App />
    </ApolloProvider>
  </StrictMode>,
)