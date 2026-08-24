import { useState } from 'react';
import { UNSAFE_createClientRoutesWithHMRRevalidationOptOut, useNavigate } from 'react-router-dom';
import { gql } from '@apollo/client';
import { useMutation } from '@apollo/client/react';
import ErrorMessage from '../../components/ui/ErrorMessage';
import { getFriendlyErrorMessage } from '../../utils/errorHandler';
import { LOGIN_MUTATION } from '../../graphql/mutations/autorization/autorizationMutations';

interface LoginData {
  login: {
    accessToken: string;
    refreshToken: string;
  };
}

interface LoginVars {
  email: string;
  password: string;
}



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

        const { accessToken, refreshToken} = response.data.login;

        localStorage.setItem('accessToken', accessToken);
        localStorage.setItem('refreshToken', refreshToken);
        localStorage.setItem('userEmail', email);
        console.log('Токени успішно отримано');
        window.location.href = '/dashboard';
      }
    } catch (err) {
      console.error("Помилка:", err);
    }
  };

 return (
    <div className="max-w-[400px] mx-auto p-5 font-sans mt-10 bg-bg-card rounded-xl shadow-md border border-gray-100">
      <h2 className="text-2xl font-bold text-center text-text-main mb-6">
        Вхід у Task Tracker
      </h2>

      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        
        {error && <ErrorMessage message={getFriendlyErrorMessage(error)} />}
        
        <input
          type="email"
          placeholder="Email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required

          className="p-2.5 border border-gray-300 rounded-md outline-none focus:border-transparent focus:ring-2 focus:ring-primary transition"
        />

        <input
          type="password"
          placeholder="Пароль"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          className="p-2.5 border border-gray-300 rounded-md outline-none focus:border-transparent focus:ring-2 focus:ring-primary transition"
        />

        <button 
          type="submit" 
          disabled={loading} 
          className="p-2.5 mt-2 bg-primary text-white font-semibold rounded-md hover:bg-primary-hover transition duration-200 disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
        >
          {loading ? 'Завантаження...' : 'Увійти'}
        </button>
      </form>
    </div>
  );
}