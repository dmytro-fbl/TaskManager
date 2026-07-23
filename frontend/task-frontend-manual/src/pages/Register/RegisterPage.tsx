import { useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation } from '@apollo/client/react';
import { VERIFY_TOKEN_QUERY, COMPLETE_REGISTRATION_MUTATION } from '../../graphql/queries/inviteQueries';

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
    const [isSuccess, setIsSuccess] = useState(false);

    const { data, loading, error } = useQuery<VerifyTokenResponse>(VERIFY_TOKEN_QUERY, {
        variables: { token: token },
        skip: !token,
    });

    const [completeReg, { loading: isSubmitting, error: submitError }] = useMutation<CompleteRegResponse>(
        COMPLETE_REGISTRATION_MUTATION
    );

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

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
        return <div>Помилка: У посиланні немає токена реєстрації</div>;
    }

    if (loading) {
        return <div>Перевіряємо посилання... Зачекайте</div>;
    }

    if (error) {
        return <div>Помилка: Токен не дійсний або його термін дії минув</div>;
    }


    return (
        <div>
            <h1>Завершення реєстрації</h1>
            <p>Ваш email: <b>{data?.verifyInviteToken}</b></p>

            <form onSubmit={handleSubmit}>
                <div>
                    <label>Ім'я:</label>
                    <br />
                    <input
                        type="text"
                        value={name}
                        onChange={(e) => setName(e.target.value)}
                        required
                    />
                </div>

                <div style={{ marginTop: '10px' }}>
                    <label>Придумайте пароль:</label>
                    <br />
                    <input
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        required
                    />
                </div>

                {submitError && (
                    <p style={{ color: 'red' }}>Помилка: {submitError.message}</p>
                )}

                <button type="submit" disabled={isSubmitting} style={{ marginTop: '15px' }}>
                    {isSubmitting ? 'Реєструємо...' : 'Зареєструватись'}
                </button>
            </form>
        </div>
    );
}
