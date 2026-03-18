import { useQuery } from '@tanstack/react-query';
import { useAuthStore } from '../store/authStore';
import { userApi } from '../services/api';
import { Tv, CheckCircle, Eye, BookmarkPlus, Clock, Star, Heart } from 'lucide-react';
import { Link } from 'react-router-dom';

function StatCard({ icon, label, value, color }: { icon: React.ReactNode; label: string; value: string | number; color: string }) {
  return (
    <div className="card" style={{ padding: 20, display: 'flex', alignItems: 'center', gap: 16 }}>
      <div style={{ width: 48, height: 48, borderRadius: 12, background: color + '22', display: 'flex', alignItems: 'center', justifyContent: 'center', color, border: `1px solid ${color}44` }}>
        {icon}
      </div>
      <div>
        <p style={{ fontFamily: 'var(--font-display)', fontSize: 28, letterSpacing: 1, lineHeight: 1 }}>{value}</p>
        <p style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 2 }}>{label}</p>
      </div>
    </div>
  );
}

export default function ProfilePage() {
  const { user } = useAuthStore();
  const { data: stats, isLoading } = useQuery({
    queryKey: ['user-stats'],
    queryFn: userApi.getStats,
  });

  const hours = stats ? Math.floor(stats.totalMinutesWatched / 60) : 0;
  const days = Math.floor(hours / 24);

  return (
    <div style={{ padding: '40px 0 60px' }}>
      <div className="container">
        {/* Profile Header */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 24, marginBottom: 48 }}>
          <div style={{
            width: 80, height: 80, borderRadius: '50%',
            background: 'linear-gradient(135deg, var(--accent), var(--accent-light))',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            fontSize: 32, fontWeight: 700, color: 'white',
            boxShadow: '0 0 30px var(--accent-glow)'
          }}>
            {user?.displayName[0].toUpperCase()}
          </div>
          <div>
            <h1 style={{ fontFamily: 'var(--font-display)', fontSize: 40, letterSpacing: 2 }}>{user?.displayName}</h1>
            <p style={{ color: 'var(--text-muted)', fontSize: 14 }}>{user?.email}</p>
            <p style={{ color: 'var(--text-muted)', fontSize: 13, marginTop: 4 }}>
              Member since {user?.createdAt ? new Date(user.createdAt).toLocaleDateString('en-US', { month: 'long', year: 'numeric' }) : '—'}
            </p>
          </div>
        </div>

        {isLoading ? (
          <div style={{ display: 'flex', justifyContent: 'center', padding: 60 }}>
            <div className="loading-spinner" />
          </div>
        ) : stats ? (
          <>
            {/* Stats Grid */}
            <h2 style={{ fontFamily: 'var(--font-display)', fontSize: 24, letterSpacing: 2, marginBottom: 20 }}>WATCHING STATS</h2>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: 14, marginBottom: 48 }}>
              <StatCard icon={<Tv size={22} />} label="Total Shows" value={stats.totalShows} color="var(--accent-light)" />
              <StatCard icon={<Eye size={22} />} label="Watching" value={stats.watchingShows} color="var(--green)" />
              <StatCard icon={<CheckCircle size={22} />} label="Completed" value={stats.completedShows} color="var(--blue)" />
              <StatCard icon={<BookmarkPlus size={22} />} label="Plan to Watch" value={stats.planToWatchShows} color="#8b5cf6" />
              <StatCard icon={<Tv size={22} />} label="Episodes Watched" value={stats.totalEpisodesWatched} color="var(--gold)" />
              <StatCard icon={<Clock size={22} />} label="Time Watched" value={days > 0 ? `${days}d ${hours % 24}h` : `${hours}h`} color="#f97316" />
              {stats.averageRating > 0 && (
                <StatCard icon={<Star size={22} />} label="Avg Episode Rating" value={stats.averageRating.toFixed(1)} color="var(--gold)" />
              )}
            </div>

            {/* Favorites */}
            {stats.favorites.length > 0 && (
              <div style={{ marginBottom: 48 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
                  <h2 style={{ fontFamily: 'var(--font-display)', fontSize: 24, letterSpacing: 2 }}>
                    <Heart size={18} style={{ display: 'inline', marginRight: 10, color: 'var(--red)' }} />
                    FAVORITES
                  </h2>
                  <Link to="/my-list" style={{ color: 'var(--accent-light)', fontSize: 13 }}>View all →</Link>
                </div>
                <div style={{ display: 'flex', gap: 14, overflowX: 'auto', paddingBottom: 8 }}>
                  {stats.favorites.map(show => (
                    <Link key={show.id} to={`/shows/${show.id}`} style={{ flexShrink: 0, width: 120 }}>
                      <div className="card" style={{ overflow: 'hidden', transition: 'transform 0.2s' }}
                        onMouseEnter={e => (e.currentTarget.style.transform = 'translateY(-4px)')}
                        onMouseLeave={e => (e.currentTarget.style.transform = 'translateY(0)')}>
                        <img
                          src={show.posterUrl || 'https://via.placeholder.com/120x180/1a1a2e/7c3aed?text=?'}
                          alt={show.title}
                          style={{ width: '100%', aspectRatio: '2/3', objectFit: 'cover' }}
                          onError={e => { (e.target as HTMLImageElement).src = 'https://via.placeholder.com/120x180/1a1a2e/7c3aed?text=?'; }}
                        />
                        <div style={{ padding: '8px 10px' }}>
                          <p style={{ fontSize: 11, fontWeight: 500, overflow: 'hidden', display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical' }}>{show.title}</p>
                        </div>
                      </div>
                    </Link>
                  ))}
                </div>
              </div>
            )}

            {/* Recently Added */}
            {stats.recentlyWatched.length > 0 && (
              <div>
                <h2 style={{ fontFamily: 'var(--font-display)', fontSize: 24, letterSpacing: 2, marginBottom: 20 }}>RECENTLY ADDED</h2>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                  {stats.recentlyWatched.map(show => (
                    <Link key={show.id} to={`/shows/${show.id}`}>
                      <div className="card" style={{ padding: '12px 16px', display: 'flex', alignItems: 'center', gap: 14, transition: 'background 0.2s' }}
                        onMouseEnter={e => (e.currentTarget.style.background = 'var(--bg-card-hover)')}
                        onMouseLeave={e => (e.currentTarget.style.background = 'var(--bg-card)')}>
                        <img src={show.posterUrl || 'https://via.placeholder.com/40x60/1a1a2e/7c3aed?text=?'} alt={show.title}
                          style={{ width: 36, height: 52, objectFit: 'cover', borderRadius: 4 }}
                          onError={e => { (e.target as HTMLImageElement).src = 'https://via.placeholder.com/40x60/1a1a2e/7c3aed?text=?'; }} />
                        <div style={{ flex: 1 }}>
                          <p style={{ fontWeight: 500, fontSize: 14 }}>{show.title}</p>
                          {show.averageRating > 0 && (
                            <div style={{ display: 'flex', alignItems: 'center', gap: 4, marginTop: 2 }}>
                              <Star size={11} fill="var(--gold)" color="var(--gold)" />
                              <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>{show.averageRating.toFixed(1)}</span>
                            </div>
                          )}
                        </div>
                      </div>
                    </Link>
                  ))}
                </div>
              </div>
            )}

            {stats.totalShows === 0 && (
              <div style={{ textAlign: 'center', padding: '60px 0' }}>
                <p style={{ fontSize: 48, marginBottom: 16 }}>📺</p>
                <p style={{ color: 'var(--text-secondary)', marginBottom: 24 }}>Start tracking shows to see your stats!</p>
                <Link to="/browse" className="btn btn-primary">Browse Shows</Link>
              </div>
            )}
          </>
        ) : null}
      </div>
    </div>
  );
}
