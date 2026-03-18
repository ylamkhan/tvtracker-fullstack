import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Tv, Eye, EyeOff } from 'lucide-react';
import toast from 'react-hot-toast';
import { authApi } from '../services/api';
import { useAuthStore } from '../store/authStore';

export default function LoginPage() {
  const [form, setForm] = useState({ email: '', password: '' });
  const [showPass, setShowPass] = useState(false);
  const [loading, setLoading] = useState(false);
  const { setAuth } = useAuthStore();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const res = await authApi.login(form);
      setAuth(res.user, res.token);
      toast.success(`Welcome back, ${res.user.displayName}!`);
      navigate('/');
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      toast.error(error.response?.data?.message || 'Login failed');
    } finally {
      setLoading(false);
    }
  };

  return <AuthLayout title="SIGN IN" subtitle="Welcome back to your TV universe">
    <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div>
        <label style={labelStyle}>Email</label>
        <input className="input" type="email" placeholder="you@example.com" value={form.email}
          onChange={e => setForm(p => ({ ...p, email: e.target.value }))} required />
      </div>
      <div>
        <label style={labelStyle}>Password</label>
        <div style={{ position: 'relative' }}>
          <input className="input" type={showPass ? 'text' : 'password'} placeholder="••••••••" value={form.password}
            onChange={e => setForm(p => ({ ...p, password: e.target.value }))} required style={{ paddingRight: 44 }} />
          <button type="button" onClick={() => setShowPass(!showPass)} style={{ position: 'absolute', right: 12, top: '50%', transform: 'translateY(-50%)', background: 'none', color: 'var(--text-muted)', display: 'flex' }}>
            {showPass ? <EyeOff size={17} /> : <Eye size={17} />}
          </button>
        </div>
      </div>
      <button type="submit" className="btn btn-primary" style={{ width: '100%', justifyContent: 'center', marginTop: 8, padding: '12px', fontSize: 15 }} disabled={loading}>
        {loading ? 'Signing in...' : 'Sign In'}
      </button>
    </form>
    <p style={{ textAlign: 'center', marginTop: 24, color: 'var(--text-secondary)', fontSize: 14 }}>
      No account? <Link to="/register" style={{ color: 'var(--accent-light)', fontWeight: 500 }}>Create one free</Link>
    </p>
  </AuthLayout>;
}

const labelStyle: React.CSSProperties = { display: 'block', fontSize: 13, color: 'var(--text-secondary)', marginBottom: 6, fontWeight: 500 };

function AuthLayout({ title, subtitle, children }: { title: string; subtitle: string; children: React.ReactNode }) {
  return (
    <div style={{
      minHeight: '100vh', background: 'var(--bg-primary)',
      display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 24,
      backgroundImage: 'radial-gradient(ellipse at 20% 50%, rgba(124,58,237,0.08) 0%, transparent 60%)',
    }}>
      <div style={{ width: '100%', maxWidth: 420 }}>
        <div style={{ textAlign: 'center', marginBottom: 40 }}>
          <Link to="/" style={{ display: 'inline-flex', alignItems: 'center', gap: 10, marginBottom: 32 }}>
            <div style={{ background: 'var(--accent)', borderRadius: 8, padding: 8, boxShadow: '0 0 20px var(--accent-glow)' }}>
              <Tv size={22} color="white" />
            </div>
            <span style={{ fontFamily: 'var(--font-display)', fontSize: 24, letterSpacing: 3 }}>TVTRACKER</span>
          </Link>
          <h1 style={{ fontFamily: 'var(--font-display)', fontSize: 36, letterSpacing: 3, marginBottom: 8 }}>{title}</h1>
          <p style={{ color: 'var(--text-secondary)', fontSize: 14 }}>{subtitle}</p>
        </div>
        <div className="card" style={{ padding: 32 }}>
          {children}
        </div>
      </div>
    </div>
  );
}
