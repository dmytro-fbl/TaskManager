import { useState } from "react";
import { useQuery, useMutation } from "@apollo/client/react";
import { FiClock, FiAlertCircle, FiCopy, FiPlus, FiX } from "react-icons/fi";
import { getFriendlyErrorMessage } from "../../../../utils/errorHandler";
import { GET_PENDING_INVITES_QUERY } from "../../../../graphql/queries/user/userQuery";
import { GENERATE_INVITE_MUTATIONS } from "../../../../graphql/mutations/autorization/invateMutations";
import ErrorMessage from "../../../../components/ui/ErrorMessage";


interface PendingUser {
    id: string,
    email: string,
    isAdmin: boolean,
    inviteToken: string,
    inviteExpiresAt: string;
    createdAt: string;
}

interface GetPendingInvitesData {
    pendingInviteUsers: PendingUser[];
}

interface GenerateInviteData {
  generateInvite: string;
}

export default function PendingInviteTab() {

    const [isModalOpen, setIsModalOpen] = useState(false);

    const { data, loading, error, refetch } = useQuery<GetPendingInvitesData>(GET_PENDING_INVITES_QUERY, {
        fetchPolicy: 'network-only'
    });

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('uk-UA', {
            month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
        });
    };

    const copyToClipboard = (token: string) => {
        const inviteLink = `${window.location.origin}/register?token=${token}`;
        navigator.clipboard.writeText(inviteLink);
        alert("Посилання скопійовано");

    };

    const [email, setEmail] = useState('');
    const [isAdmin, setIsAdmin] = useState(false);
    const [generatedLink, setGeneratelink] = useState<string | null>(null);

    const [generateInvite, {loading: inviteLoading, error: inviteErorr}] = useMutation<GenerateInviteData>(GENERATE_INVITE_MUTATIONS);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setGeneratelink(null);

        try {
            const response = await generateInvite({
                variables: { email, isAdmin }
            });

            await refetch();

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


    if (loading) return <div className="p-10 text-center">Завантаження...</div>;
    if (error) return <div className="p-10 text-red-500">{getFriendlyErrorMessage(error)}</div>;

    return (
        <>

            <div className="bg-bg-card rounded-xl shadow-sm border border-gray-100 overflow-hidden mt-6">
                <div className="p-6 border-b border-gray-100 flex justify-between items-center bg-white">
                    <div className="flex items-center gap-4">
                        <h2 className="text-xl font-bold text-text-main">Очікують на реєстрацію</h2>
                        <span className="bg-orange-100 text-orange-700 px-3 py-1 rounded-full text-sm font-medium">
                            Всього: {data?.pendingInviteUsers.length || 0}
                        </span>
                    </div>

                    {/* НОВА КНОПКА ВИКЛИКУ МОДАЛКИ */}
                    <button
                        onClick={() => setIsModalOpen(true)}
                        className="flex items-center gap-2 bg-primary text-white px-4 py-2 rounded-lg font-medium hover:bg-primary-hover transition-colors shadow-sm"
                    >
                        <FiPlus size={18} />
                        Запросити
                    </button>
                </div>



                <div className="overflow-x-auto">
                    <table className="w-full text-left border-collapse">
                        <thead>
                            <tr className="bg-gray-50 text-text-muted text-sm uppercase tracking-wider">
                                <th className="p-4 font-medium">Email</th>
                                <th className="p-4 font-medium">Роль</th>
                                <th className="p-4 font-medium">Дійсний до</th>
                                <th className="p-4 font-medium text-right">Дії</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-100">
                            {data?.pendingInviteUsers.map((user) => {
                                const isExpired = new Date(user.inviteExpiresAt) < new Date();

                                return (
                                    <tr key={user.id} className={`transition-colors ${isExpired ? 'bg-red-50/50' : 'hover:bg-gray-50/50'}`}>
                                        <td className="p-4 font-medium text-text-main">
                                            {user.email}
                                        </td>
                                        <td className="p-4">
                                            <span className={`px-3 py-1 rounded-full text-xs font-semibold ${user.isAdmin ? 'bg-purple-100 text-purple-700' : 'bg-blue-100 text-blue-700'
                                                }`}>
                                                {user.isAdmin ? 'Admin' : 'User'}
                                            </span>
                                        </td>
                                        <td className="p-4">
                                            <div className={`flex items-center gap-1.5 text-sm font-medium ${isExpired ? 'text-red-600' : 'text-green-600'}`}>
                                                {isExpired ? <FiAlertCircle size={16} /> : <FiClock size={16} />}
                                                {formatDate(user.inviteExpiresAt)}
                                                {isExpired && <span className="ml-2 text-xs bg-red-100 px-2 py-0.5 rounded-md">Прострочено</span>}
                                            </div>
                                        </td>
                                        <td className="p-4 text-right">
                                            <button
                                                onClick={() => copyToClipboard(user.inviteToken)}
                                                disabled={isExpired}
                                                title={isExpired ? "Посилання прострочене" : "Скопіювати посилання"}
                                                className={`p-2 rounded-md transition-colors ${isExpired
                                                    ? 'text-gray-300 cursor-not-allowed'
                                                    : 'text-gray-500 hover:text-primary hover:bg-primary/10'
                                                    }`}
                                            >
                                                <FiCopy size={18} />
                                            </button>
                                        </td>
                                    </tr>
                                );
                            })}

                            {data?.pendingInviteUsers.length === 0 && (
                                <tr>
                                    <td colSpan={4} className="p-8 text-center text-text-muted">
                                        Немає користувачів, які очікують на реєстрацію
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>

            {/* МОДАЛЬНЕ ВІКНО */}
            {isModalOpen && (
                <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm animate-in fade-in duration-200">
                    <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md p-6 relative">

                        <button
                            onClick={() => setIsModalOpen(false)}
                            className="absolute top-4 right-4 text-gray-400 hover:text-gray-700 hover:bg-gray-100 p-1.5 rounded-lg transition-colors"
                        >
                            <FiX size={24} />
                        </button>

                        {/* Заголовок форми */}
                        <h3 className="text-xl font-bold text-text-main mb-6">
                            Відправити запрошення
                        </h3>
                        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
                            {inviteErorr && <ErrorMessage message={getFriendlyErrorMessage(inviteErorr as any)} />}

                            <div>
                                <label className="block text-sm font-medium text-text-main mb-1">
                                    Email користувача:
                                </label>
                                <input
                                    type="email"
                                    value={email}
                                    onChange={(e) => setEmail(e.target.value)}
                                    required
                                    className="w-full p-2.5 border border-gray-300 rounded-md outline-none focus:border-transparent focus:ring-2 focus:ring-primary transition"
                                />
                            </div>

                            <div className="flex items-center gap-3 my-2">
                                <input
                                    type="checkbox"
                                    id="isAdmin"
                                    checked={isAdmin}
                                    onChange={(e) => setIsAdmin(e.target.checked)}
                                    className="w-4 h-4 text-primary bg-gray-100 border-gray-300 rounded focus:ring-primary cursor-pointer"
                                />
                                <label htmlFor="isAdmin" className="text-sm font-medium text-text-main cursor-pointer select-none">
                                    Надати права Адміністратора
                                </label>
                            </div>

                            <button
                                type="submit"
                                disabled={inviteLoading}
                                className="w-full p-2.5 bg-primary text-white font-semibold rounded-md hover:bg-primary-hover transition duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                                {inviteLoading ? 'Генерація...' : 'Створити запрошення'}
                            </button>
                        </form>

                        {generatedLink && (
                            <div className="mt-6 p-4 bg-blue-50 border border-blue-200 rounded-lg">
                                <p className="text-sm font-bold text-text-main mb-3">
                                    Одноразове посилання створено:
                                </p>
                                <input
                                    type="text"
                                    readOnly
                                    value={generatedLink || ''}
                                    className="w-full p-2.5 mb-3 border border-blue-200 rounded-md bg-white text-gray-700 outline-none"
                                />
                                <button
                                    onClick={handleCopy}
                                    type="button"
                                    className="w-full p-2.5 bg-white border border-primary text-primary font-semibold rounded-md hover:bg-primary hover:text-white transition duration-200"
                                >
                                    Скопіювати посилання
                                </button>
                            </div>
                        )}

                    </div>
                </div>
            )}
        </>
    );
}