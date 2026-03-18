import { Link } from 'react-router-dom';
import { Star, Heart, Eye } from 'lucide-react';
import type { Show } from '../types';
import { WATCH_STATUS_COLORS, type WatchStatus } from '../types';

const PLACEHOLDER = 'https://via.placeholder.com/180x270/1a1a2e/7c3aed?text=No+Poster';

interface ShowCardProps {
  show: Show;
  showProgress?: boolean;
  watchedEpisodes?: number;
  totalEpisodes?: number;
}

export default function ShowCard({ show, showProgress, watchedEpisodes, totalEpisodes }: ShowCardProps) {
  const progress = totalEpisodes && totalEpisodes > 0 ? (watchedEpisodes! / totalEpisodes) * 100 : 0;

  return (
    <Link to={`/shows/${show.id}`} style={{ display: 'block', position: 'relative' }}>
      <div style={{
        background: 'var(--bg-card)',
        borderRadius: 'var(--radius)',
        overflow: 'hidden',
        border: '1px solid var(--border)',
        transition: 'all 0.25s cubic-bezier(0.4, 0, 0.2, 1)',
        cursor: 'pointer',
      }}
        onMouseEnter={e => {
          (e.currentTarget as HTMLDivElement).style.transform = 'translateY(-6px)';
          (e.currentTarget as HTMLDivElement).style.borderColor = 'var(--accent)';
          (e.currentTarget as HTMLDivElement).style.boxShadow = '0 12px 40px rgba(124,58,237,0.25)';
        }}
        onMouseLeave={e => {
          (e.currentTarget as HTMLDivElement).style.transform = 'translateY(0)';
          (e.currentTarget as HTMLDivElement).style.borderColor = 'var(--border)';
          (e.currentTarget as HTMLDivElement).style.boxShadow = 'none';
        }}
      >
        {/* Poster */}
        <div style={{ position: 'relative', paddingTop: '150%', overflow: 'hidden', background: 'var(--bg-surface)' }}>
          <img
            src={show.posterUrl || PLACEHOLDER}
            alt={show.title}
            style={{ position: 'absolute', top: 0, left: 0, width: '100%', height: '100%', objectFit: 'cover' }}
            onError={e => { (e.target as HTMLImageElement).src = PLACEHOLDER; }}
          />
          {/* Overlays */}
          {show.isFavorite && (
            <div style={{ position: 'absolute', top: 8, right: 8, background: 'rgba(0,0,0,0.7)', borderRadius: '50%', width: 28, height: 28, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              <Heart size={14} fill="#ef4444" color="#ef4444" />
            </div>
          )}
          {show.userStatus && (
            <div style={{
              position: 'absolute', top: 8, left: 8,
              background: WATCH_STATUS_COLORS[show.userStatus as WatchStatus] + '22',
              border: `1px solid ${WATCH_STATUS_COLORS[show.userStatus as WatchStatus]}`,
              borderRadius: 6, padding: '2px 8px', fontSize: 10, fontWeight: 600,
              color: WATCH_STATUS_COLORS[show.userStatus as WatchStatus],
              backdropFilter: 'blur(8px)',
            }}>
              {show.userStatus === 'PlanToWatch' ? 'PTW' : show.userStatus === 'OnHold' ? 'Hold' : show.userStatus}
            </div>
          )}
          {/* Rating badge */}
          {show.averageRating > 0 && (
            <div style={{
              position: 'absolute', bottom: 8, left: 8,
              background: 'rgba(0,0,0,0.8)', backdropFilter: 'blur(8px)',
              borderRadius: 6, padding: '3px 8px',
              display: 'flex', alignItems: 'center', gap: 4, fontSize: 12, fontWeight: 600,
              color: 'var(--gold)'
            }}>
              <Star size={11} fill="currentColor" />
              {show.averageRating.toFixed(1)}
            </div>
          )}
          {/* Watched overlay */}
          {show.userStatus === 'Completed' && (
            <div style={{ position: 'absolute', bottom: 8, right: 8, background: 'rgba(34,197,94,0.15)', border: '1px solid #22c55e', borderRadius: 6, padding: '3px 7px' }}>
              <Eye size={12} color="#22c55e" />
            </div>
          )}
        </div>

        {/* Info */}
        <div style={{ padding: '10px 12px 12px' }}>
          <h3 style={{ fontSize: 13, fontWeight: 600, color: 'var(--text-primary)', lineHeight: 1.3, marginBottom: 4, overflow: 'hidden', display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical' }}>
            {show.title}
          </h3>
          {show.network && (
            <p style={{ fontSize: 11, color: 'var(--text-muted)', marginBottom: showProgress ? 8 : 0 }}>{show.network}</p>
          )}

          {/* Progress bar */}
          {showProgress && totalEpisodes && totalEpisodes > 0 && (
            <div>
              <div style={{ background: 'var(--bg-surface)', borderRadius: 4, height: 4, overflow: 'hidden' }}>
                <div style={{
                  width: `${progress}%`, height: '100%',
                  background: progress === 100 ? 'var(--green)' : 'var(--accent)',
                  borderRadius: 4, transition: 'width 0.3s'
                }} />
              </div>
              <p style={{ fontSize: 10, color: 'var(--text-muted)', marginTop: 4 }}>
                {watchedEpisodes}/{totalEpisodes} eps
              </p>
            </div>
          )}
        </div>
      </div>
    </Link>
  );
}
