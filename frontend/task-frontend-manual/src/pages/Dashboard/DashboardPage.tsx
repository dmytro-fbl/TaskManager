import { useNavigate } from "react-router-dom";

export default function DashboardPage(){
    const navigate = useNavigate();

    const handleLogout = () => {
        localStorage.removeItem('token');
        navigate('/login');
    };

    return (
        <div style={{ padding: '20px', maxWidth: '800px', margin: '0 auto', fontFamily: 'sans-serif' }}>
            <header>
                <h1>Головна панель (Dashboard)</h1>
                <button 
                onClick={handleLogout}
                style={{ padding: '8px 16px', backgroundColor: '#ff4d4f', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}
                >
                    Вийти
                </button>
            </header>

            <main style={{ marginTop: '20px' }}>
                <h2>Вітаємо в системі</h2>
                <p>Ви успішно увійшли в систему. Тут незабаром з'являться ваші проєкти та завдання.</p>
            </main>
        </div>
    );
};