import { Outlet, Link, useNavigate, useLocation } from 'react-router-dom';
import { useState } from 'react';
import { Tv, Search, List, User, LogOut, Menu, X, Home, Compass } from 'lucide-react';
import { useAuthStore } from '../store/authStore';
import toast from 'react-hot-toast';

export default function Layout() {
  const { user, isAuthenticated, logout } = useAuthStore();
  const navigate = useNavigate();
  const location = useLocation();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');

  const handleLogout = () => {
    logout();
    toast.success('Logged out');
    navigate('/');
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      navigate(`/browse?search=${encodeURIComponent(searchQuery.trim())}`);
      setSearchQuery('');
    }
  };

  const isActive = (path: string) => location.pathname === path;

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <nav style={{
        background: 'rgba(13,13,26,0.95)',
        backdropFilter: 'blur(20px)',
        borderBottom: '1px solid var(--border)',
        position: 'sticky', top: 0, zIndex: 100,
        padding: '0 24px',
      }}>
        <div style={{ maxWidth: 1280, margin: '0 auto', display: 'flex', alignItems: 'center', gap: 24, height: 64 }}>
          {/* Logo */}
          <Link to="/" style={{ display: 'flex', alignItems: 'center', gap: 10, flexShrink: 0 }}>
            <div style={{
              background: 'var(--accent)', borderRadius: 8, padding: 6,
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              boxShadow: '0 0 16px var(--accent-glow)'
            }}>
              <Tv size={18} color="white" />
            </div>
            <span style={{ fontFamily: 'var(--font-display)', fontSize: 22, letterSpacing: 2, color: 'var(--text-primary)' }}>
              TVTRACKER
            </span>
          </Link>

          {/* Desktop Nav Links */}
          <div style={{ display: 'flex', gap: 4, alignItems: 'center' }} className="desktop-nav">
            <NavLink to="/" icon={<Home size={15} />} label="Home" active={isActive('/')} />
            <NavLink to="/browse" icon={<Compass size={15} />} label="Browse" active={isActive('/browse')} />
            {isAuthenticated && <NavLink to="/my-list" icon={<List size={15} />} label="My List" active={isActive('/my-list')} />}
          </div>

          {/* Search */}
          <form onSubmit={handleSearch} style={{ flex: 1, maxWidth: 320, display: 'flex', position: 'relative' }}>
            <Search size={16} style={{ position: 'absolute', left: 12, top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)', pointerEvents: 'none' }} />
            <input
              value={searchQuery}
              onChange={e => setSearchQuery(e.target.value)}
              placeholder="Search shows..."
              className="input"
              style={{ paddingLeft: 38, fontSize: 13, height: 38 }}
            />
          </form>

          {/* Auth */}
          <div style={{ display: 'flex', gap: 10, alignItems: 'center', marginLeft: 'auto' }}>
            {isAuthenticated ? (
              <>
                <Link to="/profile" style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '6px 12px', borderRadius: 8, background: 'var(--bg-surface)', border: '1px solid var(--border)', color: 'var(--text-primary)', fontSize: 13 }}>
                  <div style={{ width: 28, height: 28, borderRadius: '50%', background: 'var(--accent)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 12, fontWeight: 600 }}>
                    {user?.displayName?.[0]?.toUpperCase()}
                  </div>
                  <span style={{ maxWidth: 100, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{user?.displayName}</span>
                </Link>
                <button onClick={handleLogout} className="btn btn-ghost btn-sm" title="Logout">
                  <LogOut size={15} />
                </button>
              </>
            ) : (
              <>
                <Link to="/login" className="btn btn-ghost btn-sm">Sign In</Link>
                <Link to="/register" className="btn btn-primary btn-sm">Sign Up</Link>
              </>
            )}
          </div>

          {/* Mobile menu toggle */}
          <button onClick={() => setMobileOpen(!mobileOpen)} style={{ display: 'none', background: 'none', color: 'var(--text-primary)' }} className="mobile-menu-btn">
            {mobileOpen ? <X size={22} /> : <Menu size={22} />}
          </button>
        </div>

        {/* Mobile menu */}
        {mobileOpen && (
          <div style={{ borderTop: '1px solid var(--border)', padding: '12px 0', background: 'var(--bg-primary)' }}>
            <MobileNavLink to="/" label="Home" onClick={() => setMobileOpen(false)} />
            <MobileNavLink to="/browse" label="Browse" onClick={() => setMobileOpen(false)} />
            {isAuthenticated && <MobileNavLink to="/my-list" label="My List" onClick={() => setMobileOpen(false)} />}
            {isAuthenticated && <MobileNavLink to="/profile" label="Profile" onClick={() => setMobileOpen(false)} />}
          </div>
        )}
      </nav>

      <main style={{ flex: 1 }}>
        <Outlet />
      </main>

      <footer style={{ borderTop: '1px solid var(--border)', padding: '24px', textAlign: 'center', color: 'var(--text-muted)', fontSize: 13 }}>
        <span style={{ fontFamily: 'var(--font-display)', letterSpacing: 2, marginRight: 8 }}>TVTRACKER</span>
        © {new Date().getFullYear()} — Built with ASP.NET Core + React + PostgreSQL
      </footer>

      <style>{`
        @media (max-width: 768px) {
          .desktop-nav { display: none !important; }
          .mobile-menu-btn { display: flex !important; }
        }
      `}</style>
    </div>
  );
}

function NavLink({ to, icon, label, active }: { to: string; icon: React.ReactNode; label: string; active: boolean }) {
  return (
    <Link to={to} style={{
      display: 'flex', alignItems: 'center', gap: 6,
      padding: '6px 14px', borderRadius: 8, fontSize: 14, fontWeight: 500,
      color: active ? 'white' : 'var(--text-secondary)',
      background: active ? 'var(--accent)' : 'transparent',
      boxShadow: active ? '0 0 12px var(--accent-glow)' : 'none',
      transition: 'all 0.2s',
    }}>
      {icon}{label}
    </Link>
  );
}

function MobileNavLink({ to, label, onClick }: { to: string; label: string; onClick: () => void }) {
  return (
    <Link to={to} onClick={onClick} style={{ display: 'block', padding: '12px 24px', color: 'var(--text-secondary)', fontSize: 15, borderBottom: '1px solid var(--border)' }}>
      {label}
    </Link>
  );
}
