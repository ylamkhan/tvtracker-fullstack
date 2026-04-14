import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Tv, TrendingUp, Star, ArrowRight, Play } from 'lucide-react';
import { showsApi } from '../services/api';
import ShowCard from '../components/ShowCard';
import { useAuthStore } from '../store/authStore';

export default function HomePage() {
  const { isAuthenticated, user } = useAuthStore();
  const { data: topShows } = useQuery({
    queryKey: ['shows', 'top'],
    queryFn: () => showsApi.getAll({ pageSize: 12 }),
  });
  const { data: ongoingShows } = useQuery({
    queryKey: ['shows', 'ongoing'],
    queryFn: () => showsApi.getAll({ status: 'Ongoing', pageSize: 6 }),
  });

  return (
    <div>
      {/* Hero */}
      <section style={{
        background: 'linear-gradient(135deg, rgb(0 0 0) 0%, rgb(49 2 6) 50%, #000000 100%)',
        padding: '80px 24px 64px',
        position: 'relative',
        overflow: 'hidden',
      }}>
        {/* Background grid effect */}
        <div style={{
          position: 'absolute', inset: 0, opacity: 0.04,
          backgroundImage: 'linear-gradient(var(--border) 1px, transparent 1px), linear-gradient(90deg, var(--border) 1px, transparent 1px)',
          backgroundSize: '40px 40px',
        }} />
        <div style={{ position: 'absolute', top: -100, right: -100, width: 400, height: 400, background: 'radial-gradient(circle, rgba(225,29,72,0.2) 0%, transparent 70%)', borderRadius: '50%' }} />

        <div className="container" style={{ position: 'relative', textAlign: 'center' }}>
          <div style={{ display: 'inline-flex', alignItems: 'center', gap: 8, background: 'rgb(143 0 31)', border: '1px solid rgb(114 112 115)', borderRadius: 20, padding: '6px 16px', marginBottom: 24, fontSize: 13, color: 'var(--accent-light)' }}>
            <Play size={13} fill="currentColor" />
            Track every episode. Rate every moment.
          </div>

          <h1 style={{
            fontFamily: 'var(--font-display)',
            fontSize: 'clamp(48px, 8vw, 96px)',
            letterSpacing: 4,
            lineHeight: 0.95,
            marginBottom: 24,
          }}>
            YOUR TV<br />
            <span style={{ color: 'var(--accent)', textShadow: '0 0 40px var(--accent-glow)' }}>UNIVERSE</span>
          </h1>

          <p style={{ fontSize: 18, color: 'var(--text-secondary)', maxWidth: 480, margin: '0 auto 40px', lineHeight: 1.7 }}>
            Track shows, rate episodes, discover what to watch next. Your personal TV companion.
          </p>

          <div style={{ display: 'flex', gap: 14, justifyContent: 'center', flexWrap: 'wrap' }}>
            {isAuthenticated ? (
              <>
                <Link to="/my-list" className="btn btn-primary" style={{ fontSize: 15, padding: '12px 28px' }}>
                  <Tv size={17} /> My List
                </Link>
                <Link to="/browse" className="btn btn-ghost" style={{ fontSize: 15, padding: '12px 28px' }}>
                  <TrendingUp size={17} /> Browse Shows
                </Link>
              </>
            ) : (
              <>
                <Link to="/register" className="btn btn-primary" style={{ fontSize: 15, padding: '12px 28px' }}>
                  Start Tracking Free
                </Link>
                <Link to="/browse" className="btn btn-ghost" style={{ fontSize: 15, padding: '12px 28px' }}>
                  Browse Shows <ArrowRight size={16} />
                </Link>
              </>
            )}
          </div>

          {/* Stats row */}
          {isAuthenticated && (
            <div style={{ display: 'flex', gap: 32, justifyContent: 'center', marginTop: 48, flexWrap: 'wrap' }}>
              {[
                { label: 'Welcome back,', value: user?.displayName || '', highlight: true },
                { label: 'Shows in catalog', value: topShows?.totalCount?.toString() || '—' },
              ].map(item => (
                <div key={item.label} style={{ textAlign: 'center' }}>
                  <div style={{ fontSize: 28, fontFamily: 'var(--font-display)', letterSpacing: 2, color: item.highlight ? 'var(--accent-light)' : 'var(--text-primary)' }}>
                    {item.value}
                  </div>
                  <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>{item.label}</div>
                </div>
              ))}
            </div>
          )}
        </div>
      </section>

      {/* Top Rated Shows */}
      <section style={{ padding: '60px 0' }}>
        <div className="container">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 28 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
              <div style={{ width: 4, height: 28, background: 'var(--accent)', borderRadius: 2, boxShadow: '0 0 12px var(--accent-glow)' }} />
              <h2 style={{ fontFamily: 'var(--font-display)', fontSize: 28, letterSpacing: 2 }}>
                <Star size={20} style={{ display: 'inline', marginRight: 8, color: 'var(--gold)' }} />
                TOP RATED
              </h2>
            </div>
            <Link to="/browse" style={{ display: 'flex', alignItems: 'center', gap: 6, color: 'var(--accent-light)', fontSize: 14 }}>
              View all <ArrowRight size={15} />
            </Link>
          </div>

          {topShows ? (
            <div className="grid-shows fade-in">
              {topShows.items.map(show => (
                <ShowCard key={show.id} show={show} />
              ))}
            </div>
          ) : (
            <div style={{ display: 'flex', justifyContent: 'center', padding: 60 }}>
              <div className="loading-spinner" />
            </div>
          )}
        </div>
      </section>

      {/* Currently Airing */}
      {ongoingShows && ongoingShows.items.length > 0 && (
        <section style={{ padding: '0 0 60px' }}>
          <div className="container">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 28 }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                <div style={{ width: 4, height: 28, background: 'var(--green)', borderRadius: 2 }} />
                <h2 style={{ fontFamily: 'var(--font-display)', fontSize: 28, letterSpacing: 2 }}>
                  <span style={{ display: 'inline-block', width: 8, height: 8, background: 'var(--green)', borderRadius: '50%', marginRight: 10, boxShadow: '0 0 8px var(--green)', animation: 'pulse 2s infinite' }} />
                  NOW AIRING
                </h2>
              </div>
            </div>
            <div className="grid-shows fade-in">
              {ongoingShows.items.map(show => (
                <ShowCard key={show.id} show={show} />
              ))}
            </div>
          </div>
        </section>
      )}

      {/* CTA for non-auth */}
      {!isAuthenticated && (
        <section style={{ padding: '60px 24px', background: 'var(--bg-secondary)', borderTop: '1px solid var(--border)' }}>
          <div style={{ maxWidth: 600, margin: '0 auto', textAlign: 'center' }}>
            <h2 style={{ fontFamily: 'var(--font-display)', fontSize: 40, letterSpacing: 3, marginBottom: 16 }}>
              JOIN THE COMMUNITY
            </h2>
            <p style={{ color: 'var(--text-secondary)', marginBottom: 32, lineHeight: 1.7 }}>
              Create a free account to track your shows, rate episodes, write reviews, and see your personal watching stats.
            </p>
            <Link to="/register" className="btn btn-primary" style={{ fontSize: 16, padding: '14px 36px' }}>
              Create Free Account
            </Link>
          </div>
        </section>
      )}

      <style>{`@keyframes pulse { 0%,100%{opacity:1}50%{opacity:0.4} }`}</style>
    </div>
  );
}
