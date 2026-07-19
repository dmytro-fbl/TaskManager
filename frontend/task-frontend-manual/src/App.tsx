import { useState } from 'react';
import { gql } from '@apollo/client';
import { useMutation } from '@apollo/client/react';

// 1. Описуємо, що ми очікуємо отримати від сервера
interface LoginData {
  login: {
    token: string;
  };
}

// 2. Описуємо, які змінні ми відправляємо
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

function App() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  // 3. Передаємо наші типи в хук у кутових дужках: <Дані, Змінні>
  const [loginFunc, { loading, error }] = useMutation<LoginData, LoginVars>(LOGIN_MUTATION);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      const response = await loginFunc({
        variables: { email, password }
      });

      // 4. Додаємо перевірку if (response.data), щоб TS точно знав, що дані прийшли
      if (response.data) {
        const token = response.data.login.token; // Тепер тут працює автодоповнення і немає помилок!
        localStorage.setItem('token', token);
        console.log("Токен отримано:", token);
        alert('Вхід виконано!');
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

export default App;