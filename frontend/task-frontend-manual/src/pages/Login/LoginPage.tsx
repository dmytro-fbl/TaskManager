import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { gql } from '@apollo/client';
import { useMutation } from '@apollo/client/react';

interface LoginData {
  login: {
    token: string;
  };
}

interface LoginVars {
  email: string;
  password: string;
}

const LOGIN_MUTATION = gql`
  mutation Login($email: String!, $password: String!) {
    login(request: { email: $email, password: $password }) {
      token
    }
  }
`;

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const navigate = useNavigate();

  const [loginFunc, { loading, error }] = useMutation<LoginData, LoginVars>(LOGIN_MUTATION);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      const response = await loginFunc({
        variables: { email, password }
      });

      if (response.data) {
        const token = response.data.login.token; 
        localStorage.setItem('token', token);
        console.log("Токен отримано:", token);
        navigate ('/dashboard');
        
      }
    } catch (err) {
      console.error("Помилка:", err);
    }
  };

  return (
    <div style={{ padding: '20px', maxWidth: '400px', margin: '0 auto', fontFamily: 'sans-serif' }}>
      <h2>Вхід у Task Tracker</h2>

      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: "column", gap: '15px' }}>
        <input
          type="email"
          placeholder="Email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          style={{ padding: '8px' }}
        />
        
        <input
          type="password"
          placeholder="Пароль"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          style={{ padding: '8px' }}
        />

        {error && <div style={{ color: 'red' }}>Помилка входу</div>}

        <button type="submit" disabled={loading} style={{ padding: '10px' }}>
          {loading ? 'Завантаження...' : 'Увійти'}
        </button>
      </form>
    </div>
  )
}