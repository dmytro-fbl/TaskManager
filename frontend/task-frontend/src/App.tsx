import { useState } from 'react';

function App() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const [errorMessage, setErrorMessage] = useState('');

  const handleSumbit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage('');

    try {
      const response = await fetch('https://localhost:7190/api/Auth/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email: email, password: password }),
      });

      if(response.ok){
        const data =  await response.json();

        console.log("Успіх : ", data);

        localStorage.setItem('token', data.token);

        alert('Успішний вхід! токен збережено');
      }else{
        setErrorMessage('Неправильний email або пароль');
      }

    }catch(error){
      console.error('помилка мережі', error);
      setErrorMessage('Не вдалося з\'єднатися з сервером');
    }

  };

  return (
    <div style={{ padding: '20px', maxWidth: '400px', margin: '0 auto', fontFamily: 'sans-serif' }}>
      <h2>Вхід у Task Tracker</h2>

      <form onSubmit={handleSumbit} style={{ display: 'flex', flexDirection: "column", gap: '15px' }}>
        <div>
          <label>Email:</label> <br />
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            style={{ width: '100%', padding: '8px', marginTop: '5px' }}
          />
        </div>
        <div>
          <label>Пароль:</label>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            style={{ width: '100%', padding: '8px', marginTop: '5px' }}
          />
        </div>

        {errorMessage && (
          <div style={{ color: 'red', fontWeight: 'bold' }}>
            {errorMessage}
          </div>
        )}

        <button type="submit" style={{ padding: '10px', cursor: 'pointer', backgroundColor: '#007bff', color: 'white', border: 'none', borderRadius: '4px' }}>
          Увійти
        </button>
      </form>


    </div>
  )
}

export default App