import { useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation } from '@apollo/client/react';
import { VERIFY_TOKEN_QUERY, COMPLETE_REGISTRATION_MUTATION } from '../../graphql/queries/inviteQueries';
import ErrorMessage from '../../components/ui/ErrorMessage';
import { getFriendlyErrorMessage } from '../../utils/errorHandler';

interface VerifyTokenResponse {
    verifyInviteToken: string;
}

interface CompleteRegResponse {
    completeRegistration: boolean;
}

export default function RegisterPage() {
    const [searchParams] = useSearchParams();
    const token = searchParams.get('token');

    const navigate = useNavigate();

    const [name, setName] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassowrd] = useState('');

    const [validationError, setValidationError] = useState<string | null>(null);

    const { data, loading, error } = useQuery<VerifyTokenResponse>(VERIFY_TOKEN_QUERY, {
        variables: { token: token },
        skip: !token,
    });

    const [completeReg, { loading: isSubmitting, error: submitError }] = useMutation<CompleteRegResponse>(
        COMPLETE_REGISTRATION_MUTATION
    );

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setValidationError(null);

        if(name.trim().length < 2){
          setValidationError("Імя повинно містити щонайменше 2 символи");
          return;
        }

        if(password.length < 6){
          setValidationError("Пароль повинен містити щонайменше 6 символів");
          return;
        }

        if(password != confirmPassword){
          setValidationError("Паролі не співпадають!");
          return;
        }

        try {
            const result = await completeReg({
                variables: {
                    token: token,
                    name: name,
                    password: password
                }
            });

            if (result.data?.completeRegistration) {
                navigate('/login');
            }
        } catch (err) {
            console.error("Помилка при реєстрації", err);
        }
    };

    if (!token) {
        return (
            <div className="max-w-[400px] mx-auto mt-10 p-5 bg-bg-card rounded-xl shadow-md border border-gray-100">
                <ErrorMessage message="Помилка: У посиланні немає токена реєстрації" />
            </div>
        );
    }

    if (loading) {
        return (
            <div className="max-w-[400px] mx-auto mt-10 p-8 bg-bg-card rounded-xl shadow-md border border-gray-100 text-center">
                <div className="animate-spin rounded-full h-10 w-10 border-4 border-gray-200 border-t-primary mx-auto mb-4"></div>
                <p className="text-text-muted font-medium">
                    Перевіряємо посилання... Зачекайте
                </p>
            </div>
        );
    }

    if (error) {
        return (
            <div className="max-w-[400px] mx-auto mt-10 p-5 bg-bg-card rounded-xl shadow-md border border-gray-100">
                <ErrorMessage message="Помилка: Токен не дійсний або його термін дії минув" />
            </div>
        );
    }


    return (
    <div className="max-w-[400px] mx-auto p-5 font-sans mt-10 bg-bg-card rounded-xl shadow-md border border-gray-100">
      <h2 className="text-2xl font-bold text-center text-text-main mb-2">
        Завершення реєстрації
      </h2>
      <p className="text-center text-text-muted mb-6">
        Ваш email: <b className="text-text-main">{data?.verifyInviteToken}</b>
      </p>

      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        
        {validationError && <ErrorMessage message={validationError}/>}
        {submitError && <ErrorMessage message={getFriendlyErrorMessage(submitError)}/>}

        <div>
          <label className="block text-sm font-medium text-text-main mb-1">
            Ім'я:
          </label>
          <input
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            className="w-full p-2.5 border border-gray-300 rounded-md outline-none focus:border-transparent focus:ring-2 focus:ring-primary transition"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-text-main mb-1">
            Придумайте пароль:
          </label>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            className="w-full p-2.5 border border-gray-300 rounded-md outline-none focus:border-transparent focus:ring-2 focus:ring-primary transition"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-text-main mb-1">
            Повторіть пароль:
          </label>
          <input
            type="password"
            value={confirmPassword}
            onChange={(e) => setConfirmPassowrd(e.target.value)}
            required
            className="w-full p-2.5 border border-gray-300 rounded-md outline-none focus:border-transparent focus:ring-2 focus:ring-primary transition"
          />
        </div>

        <button 
          type="submit" 
          disabled={isSubmitting} 
          className="w-full p-2.5 mt-2 bg-primary text-white font-semibold rounded-md hover:bg-primary-hover transition duration-200 disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
        >
          {isSubmitting ? 'Реєструємо...' : 'Зареєструватись'}
        </button>
      </form>
    </div>
  );
}
