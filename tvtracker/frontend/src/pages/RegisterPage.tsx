import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Tv } from 'lucide-react';
import toast from 'react-hot-toast';
import { authApi } from '../services/api';
import { useAuthStore } from '../store/authStore';

const labelStyle: React.CSSProperties = { display: 'block', fontSize: 13, color: 'var(--text-secondary)', marginBottom: 6, fontWeight: 500 };

export default function RegisterPage() {
  const [form, setForm] = useState({ email: '', password: '', displayName: '' });
  const [loading, setLoading] = useState(false);
  const { setAuth } = useAuthStore();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (form.password.length < 6) { toast.error('Password must be at least 6 characters'); return; }
    setLoading(true);
    try {
      const res = await authApi.register(form);
      setAuth(res.user, res.token);
      toast.success(`Welcome, ${res.user.displayName}! 🎉`);
      navigate('/');
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      toast.error(error.response?.data?.message || 'Registration failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{
      minHeight: '100vh', background: 'var(--bg-primary)',
      display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 24,
      backgroundImage: 'radial-gradient(at 80% 50%, #3d0101 0%, transparent 60%)',
    }}>
      <div style={{ width: '100%', maxWidth: 420 }}>
        <div style={{ textAlign: 'center', marginBottom: 40 }}>
          <Link to="/" style={{ display: 'inline-flex', alignItems: 'center', gap: 10, marginBottom: 32 }}>
            <div style={{ background: 'var(--accent)', borderRadius: 8, padding: 8, boxShadow: '0 0 20px var(--accent-glow)' }}>
              <Tv size={22} color="white" />
            </div>
            <span style={{ fontFamily: 'var(--font-display)', fontSize: 24, letterSpacing: 3 }}>TVTRACKER</span>
          </Link>
          <h1 style={{ fontFamily: 'var(--font-display)', fontSize: 36, letterSpacing: 3, marginBottom: 8 }}>JOIN FREE</h1>
          <p style={{ color: 'var(--text-secondary)', fontSize: 14 }}>Start tracking your TV journey today</p>
        </div>
        <div className="card" style={{ padding: 32 }}>
          <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
            <div>
              <label style={labelStyle}>Display Name</label>
              <input className="input" placeholder="Your name" value={form.displayName}
                onChange={e => setForm(p => ({ ...p, displayName: e.target.value }))} required />
            </div>
            <div>
              <label style={labelStyle}>Email</label>
              <input className="input" type="email" placeholder="you@example.com" value={form.email}
                onChange={e => setForm(p => ({ ...p, email: e.target.value }))} required />
            </div>
            <div>
              <label style={labelStyle}>Password <span style={{ color: 'var(--text-muted)', fontWeight: 400 }}>(min 6 chars)</span></label>
              <input className="input" type="password" placeholder="••••••••" value={form.password}
                onChange={e => setForm(p => ({ ...p, password: e.target.value }))} required />
            </div>
            <button type="submit" className="btn btn-primary" style={{ width: '100%', justifyContent: 'center', marginTop: 8, padding: '12px', fontSize: 15 }} disabled={loading}>
              {loading ? 'Creating account...' : 'Create Account'}
            </button>
          </form>
          <p style={{ textAlign: 'center', marginTop: 24, color: 'var(--text-secondary)', fontSize: 14 }}>
            Already have an account? <Link to="/login" style={{ color: 'var(--accent-light)', fontWeight: 500 }}>Sign in</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
