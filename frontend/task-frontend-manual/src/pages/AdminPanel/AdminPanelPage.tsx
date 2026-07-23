import { useState } from "react";
import { disableExperimentalFragmentVariables, gql } from "@apollo/client";
import { useMutation } from '@apollo/client/react';
import { GENERATE_INVITE_MUTATIONS } from '../../graphql/mutations/invateMutations';

interface GenerateInviteData {
    generateInvite: string;
}

export default function InviteUserForm() {
    const [email, setEmail] = useState('');
    const [isAdmin, setIsAdmin] = useState(false);
    const [generatedLink, setGeneratelink] = useState<string | null>(null);

    const [generateInvite, { loading, error }] = useMutation<GenerateInviteData>(GENERATE_INVITE_MUTATIONS);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setGeneratelink(null);

        try {
            const response = await generateInvite({
                variables: { email, isAdmin }
            });


            if (response.data?.generateInvite) {
                const token = response.data.generateInvite;

                const link = `${window.location.origin}/register?token=${token}`;
                setGeneratelink(link);
                setIsAdmin(false);
                setEmail('');
            }
        } catch (err) {
            console.error("Помилка генерації запрошення: ", err);
        }
    };

    const handleCopy = () => {
        if (generatedLink) {
            navigator.clipboard.writeText(generatedLink);
            alert('Посилання скопійовано в буфер обміну');
        }
    };

    return (
        <div style={{ padding: '20px', border: '1px solid #ccc', borderRadius: '8px', maxWidth: '400px', marginTop: '20px' }}>
            <h3>Запросити нового користувача</h3>
            <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '15px' }}>
                <div>
                    <label> Email користувача:</label>
                    <input
                        type="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        required
                        style={{ width: '100%', padding: '8px', marginTop: '5px', boxSizing: 'border-box' }}
                    />
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                    <input
                        type="checkbox"
                        id="isAdmin"
                        checked={isAdmin}
                        onChange={(e) => setIsAdmin(e.target.checked)}
                    />
                    <label htmlFor="isAdmin">Надати права Адміністратора</label>
                </div>

                {error && <p style={{ color: 'red', margin: '0' }}>Помилка: {error.message}</p>}

                <button type="submit" disabled={loading} style={{ padding: '10px', cursor: 'pointer' }}>
                    {loading ? 'Генерація...' : 'Створити запрошення'}
                </button>
            </form>

            {generatedLink && (
                <div style={{ marginTop: '20px', padding: '15px', backgroundColor: '#e6f7ff', border: '1px solid #91d5ff', borderRadius: '4px' }}>
                    <p style={{ margin: '0 0 10px 0', fontSize: '14px', fontWeight: 'bold' }}>Одноразове посилання створено:</p>
                    <input
                        type="text"
                        readOnly
                        value={generatedLink}
                        style={{ width: '100%', padding: '8px', boxSizing: 'border-box', marginBottom: '10px' }}
                    />
                    <button onClick={handleCopy} style={{ padding: '8px 12px', cursor: 'pointer', backgroundColor: '#1890ff', color: 'white', border: 'none', borderRadius: '4px' }}>
                        Скопіювати посилання
                    </button>
                </div>
            )}
        </div>


    );
}

