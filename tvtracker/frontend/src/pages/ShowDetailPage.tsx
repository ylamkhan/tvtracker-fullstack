import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Star, Heart, CheckCircle, Circle, ChevronDown, ChevronUp, Calendar, Clock, Tv, ArrowLeft, MessageSquare } from 'lucide-react';
import toast from 'react-hot-toast';
import { showsApi, episodesApi } from '../services/api';
import { useAuthStore } from '../store/authStore';
import StarRating from '../components/StarRating';
import type { WatchStatus } from '../types';
import { WATCH_STATUS_LABELS, WATCH_STATUS_COLORS } from '../types';

const PLACEHOLDER_BACKDROP = 'https://via.placeholder.com/1280x720/12122a/7c3aed?text=No+Backdrop';
const PLACEHOLDER_POSTER = 'https://via.placeholder.com/300x450/1a1a2e/7c3aed?text=No+Poster';

export default function ShowDetailPage() {
  const { id } = useParams<{ id: string }>();
  const showId = parseInt(id!);
  const qc = useQueryClient();
  const { isAuthenticated } = useAuthStore();
  const [expandedSeason, setExpandedSeason] = useState<number | null>(null);
  const [reviewText, setReviewText] = useState('');
  const [reviewRating, setReviewRating] = useState(0);
  const [reviewSpoilers, setReviewSpoilers] = useState(false);

  const { data: show, isLoading } = useQuery({
    queryKey: ['show', showId],
    queryFn: () => showsApi.getById(showId),
  });

  const trackMutation = useMutation({
    mutationFn: (data: { status?: string; userRating?: number; isFavorite?: boolean }) =>
      showsApi.track(showId, data),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['show', showId] }); toast.success('Updated!'); },
    onError: () => toast.error('Please sign in first'),
  });

  const watchMutation = useMutation({
    mutationFn: ({ episodeId, watched }: { episodeId: number; watched: boolean }) =>
      watched ? episodesApi.markWatched(episodeId) : episodesApi.unmarkWatched(episodeId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['show', showId] }),
    onError: () => toast.error('Please sign in'),
  });

  const rateMutation = useMutation({
    mutationFn: ({ episodeId, rating }: { episodeId: number; rating: number }) =>
      episodesApi.rate(episodeId, { rating }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['show', showId] }); toast.success('Episode rated!'); },
  });

  const reviewMutation = useMutation({
    mutationFn: () => showsApi.createReview(showId, { content: reviewText, rating: reviewRating, containsSpoilers: reviewSpoilers }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['show', showId] }); setReviewText(''); setReviewRating(0); toast.success('Review posted!'); },
    onError: () => toast.error('Could not post review'),
  });

  if (isLoading) return (
    <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
      <div className="loading-spinner" />
    </div>
  );

  if (!show) return <div style={{ textAlign: 'center', padding: 80 }}>Show not found</div>;

  const genres = show.genre?.split(',') || [];

  return (
    <div>
      {/* Backdrop Hero */}
      <div style={{ position: 'relative', height: 420, overflow: 'hidden' }}>
        <img src={show.backdropUrl || PLACEHOLDER_BACKDROP} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover', opacity: 0.4 }}
          onError={e => { (e.target as HTMLImageElement).src = PLACEHOLDER_BACKDROP; }} />
        <div style={{ position: 'absolute', inset: 0, background: 'linear-gradient(to top, var(--bg-primary) 30%, transparent 100%)' }} />
        <div style={{ position: 'absolute', inset: 0, background: 'linear-gradient(to right, var(--bg-primary) 0%, transparent 50%)' }} />
      </div>

      {/* Main Content */}
      <div className="container" style={{ marginTop: -200, position: 'relative', paddingBottom: 60 }}>
        <Link to="/browse" style={{ display: 'inline-flex', alignItems: 'center', gap: 6, color: 'var(--text-secondary)', fontSize: 13, marginBottom: 24 }}>
          <ArrowLeft size={15} /> Back to Browse
        </Link>

        <div style={{ display: 'flex', gap: 36, alignItems: 'flex-start', flexWrap: 'wrap' }}>
          {/* Poster */}
          <div style={{ flexShrink: 0 }}>
            <img src={show.posterUrl || PLACEHOLDER_POSTER} alt={show.title}
              style={{ width: 200, borderRadius: 'var(--radius)', boxShadow: 'var(--shadow-lg)', border: '1px solid var(--border)' }}
              onError={e => { (e.target as HTMLImageElement).src = PLACEHOLDER_POSTER; }} />
          </div>

          {/* Info */}
          <div style={{ flex: 1, minWidth: 280 }}>
            {/* Status badge */}
            <div style={{ display: 'flex', gap: 10, marginBottom: 12, flexWrap: 'wrap' }}>
              <span className="badge" style={{ background: show.status === 'Ongoing' ? 'rgba(34,197,94,0.15)' : 'rgba(148,163,184,0.1)', color: show.status === 'Ongoing' ? 'var(--green)' : 'var(--text-secondary)', border: `1px solid ${show.status === 'Ongoing' ? 'var(--green)' : 'var(--border)'}` }}>
                {show.status === 'Ongoing' && <span style={{ display: 'inline-block', width: 6, height: 6, background: 'var(--green)', borderRadius: '50%', marginRight: 6 }} />}
                {show.status}
              </span>
              {genres.map(g => <span key={g} className="badge" style={{ background: 'rgba(124,58,237,0.1)', color: 'var(--accent-light)', border: '1px solid rgba(124,58,237,0.2)' }}>{g.trim()}</span>)}
            </div>

            <h1 style={{ fontFamily: 'var(--font-display)', fontSize: 'clamp(32px, 5vw, 56px)', letterSpacing: 2, lineHeight: 1, marginBottom: 16 }}>
              {show.title}
            </h1>

            {/* Meta row */}
            <div style={{ display: 'flex', gap: 20, marginBottom: 16, flexWrap: 'wrap', color: 'var(--text-secondary)', fontSize: 13 }}>
              {show.network && <span style={{ display: 'flex', alignItems: 'center', gap: 6 }}><Tv size={14} /> {show.network}</span>}
              {show.firstAirDate && <span style={{ display: 'flex', alignItems: 'center', gap: 6 }}><Calendar size={14} /> {new Date(show.firstAirDate).getFullYear()}{show.lastAirDate && ` – ${new Date(show.lastAirDate).getFullYear()}`}</span>}
              <span style={{ display: 'flex', alignItems: 'center', gap: 6 }}><Tv size={14} /> {show.seasonsCount} season{show.seasonsCount !== 1 ? 's' : ''} · {show.episodesCount} episodes</span>
            </div>

            {/* Rating */}
            <div style={{ display: 'flex', alignItems: 'center', gap: 14, marginBottom: 20 }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <Star size={20} fill="var(--gold)" color="var(--gold)" />
                <span style={{ fontFamily: 'var(--font-display)', fontSize: 28, letterSpacing: 1 }}>{show.averageRating.toFixed(1)}</span>
                <span style={{ color: 'var(--text-muted)', fontSize: 13 }}>/ 10 · {show.ratingCount} ratings</span>
              </div>
            </div>

            {show.description && <p style={{ color: 'var(--text-secondary)', lineHeight: 1.8, marginBottom: 28, maxWidth: 600 }}>{show.description}</p>}

            {/* User Actions */}
            {isAuthenticated ? (
              <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
                <select
                  value={show.userStatus || ''}
                  onChange={e => trackMutation.mutate({ status: e.target.value || undefined })}
                  style={{
                    background: show.userStatus ? WATCH_STATUS_COLORS[show.userStatus as WatchStatus] + '22' : 'var(--bg-surface)',
                    border: `1px solid ${show.userStatus ? WATCH_STATUS_COLORS[show.userStatus as WatchStatus] : 'var(--border)'}`,
                    borderRadius: 8, padding: '8px 16px', color: show.userStatus ? WATCH_STATUS_COLORS[show.userStatus as WatchStatus] : 'var(--text-secondary)',
                    fontSize: 13, cursor: 'pointer', fontWeight: 500, fontFamily: 'var(--font-body)',
                  }}>
                  <option value="">+ Add to List</option>
                  {Object.entries(WATCH_STATUS_LABELS).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
                </select>

                <button onClick={() => trackMutation.mutate({ isFavorite: !show.isFavorite })}
                  className="btn btn-ghost btn-sm"
                  style={{ borderColor: show.isFavorite ? 'var(--red)' : 'var(--border)', color: show.isFavorite ? 'var(--red)' : 'var(--text-secondary)' }}>
                  <Heart size={15} fill={show.isFavorite ? 'currentColor' : 'none'} />
                  {show.isFavorite ? 'Favorited' : 'Favorite'}
                </button>

                {show.userStatus && (
                  <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <span style={{ fontSize: 13, color: 'var(--text-secondary)' }}>Your rating:</span>
                    <StarRating value={show.userRating || 0} onChange={(r) => trackMutation.mutate({ userRating: r })} size={16} />
                  </div>
                )}
              </div>
            ) : (
              <Link to="/login" className="btn btn-primary">Sign in to track this show</Link>
            )}
          </div>
        </div>

        {/* Episodes Accordion */}
        {show.seasons.length > 0 && (
          <div style={{ marginTop: 60 }}>
            <h2 style={{ fontFamily: 'var(--font-display)', fontSize: 28, letterSpacing: 2, marginBottom: 20 }}>EPISODES</h2>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              {show.seasons.map(season => (
                <div key={season.id} className="card">
                  <button
                    onClick={() => setExpandedSeason(expandedSeason === season.id ? null : season.id)}
                    style={{ width: '100%', padding: '16px 20px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', background: 'none', color: 'var(--text-primary)', fontSize: 15, fontWeight: 600 }}>
                    <div style={{ display: 'flex', gap: 16, alignItems: 'center' }}>
                      <span style={{ fontFamily: 'var(--font-display)', fontSize: 18, letterSpacing: 1, color: 'var(--accent-light)' }}>
                        S{season.seasonNumber}
                      </span>
                      <span>{season.title || `Season ${season.seasonNumber}`}</span>
                      <span style={{ fontSize: 12, color: 'var(--text-muted)', fontWeight: 400 }}>
                        {season.episodes.length} episodes
                        {isAuthenticated && ` · ${season.episodes.filter(e => e.isWatched).length} watched`}
                      </span>
                    </div>
                    {expandedSeason === season.id ? <ChevronUp size={18} color="var(--text-muted)" /> : <ChevronDown size={18} color="var(--text-muted)" />}
                  </button>

                  {expandedSeason === season.id && (
                    <div style={{ borderTop: '1px solid var(--border)' }}>
                      {season.episodes.map(episode => (
                        <div key={episode.id} style={{
                          display: 'flex', alignItems: 'center', gap: 14, padding: '12px 20px',
                          borderBottom: '1px solid var(--border)', transition: 'background 0.15s',
                          background: episode.isWatched ? 'rgba(34,197,94,0.03)' : 'transparent',
                        }}
                          onMouseEnter={e => (e.currentTarget.style.background = 'var(--bg-card-hover)')}
                          onMouseLeave={e => (e.currentTarget.style.background = episode.isWatched ? 'rgba(34,197,94,0.03)' : 'transparent')}>

                          {/* Watch toggle */}
                          {isAuthenticated && (
                            <button onClick={() => watchMutation.mutate({ episodeId: episode.id, watched: !episode.isWatched })} style={{ background: 'none', flexShrink: 0 }}>
                              {episode.isWatched
                                ? <CheckCircle size={20} color="var(--green)" fill="rgba(34,197,94,0.2)" />
                                : <Circle size={20} color="var(--border-light)" />}
                            </button>
                          )}

                          <div style={{ fontSize: 13, color: 'var(--accent-light)', fontWeight: 600, minWidth: 32, fontFamily: 'var(--font-display)', letterSpacing: 1 }}>
                            {String(episode.episodeNumber).padStart(2, '0')}
                          </div>

                          <div style={{ flex: 1 }}>
                            <p style={{ fontSize: 14, fontWeight: 500, color: episode.isWatched ? 'var(--text-secondary)' : 'var(--text-primary)' }}>{episode.title}</p>
                            <div style={{ display: 'flex', gap: 12, marginTop: 2, flexWrap: 'wrap' }}>
                              {episode.airDate && <span style={{ fontSize: 11, color: 'var(--text-muted)', display: 'flex', alignItems: 'center', gap: 4 }}><Calendar size={10} />{new Date(episode.airDate).toLocaleDateString()}</span>}
                              {episode.durationMinutes && <span style={{ fontSize: 11, color: 'var(--text-muted)', display: 'flex', alignItems: 'center', gap: 4 }}><Clock size={10} />{episode.durationMinutes}m</span>}
                              {episode.averageRating > 0 && <span style={{ fontSize: 11, color: 'var(--gold)', display: 'flex', alignItems: 'center', gap: 4 }}><Star size={10} fill="currentColor" />{episode.averageRating.toFixed(1)}</span>}
                            </div>
                          </div>

                          {/* Rate episode */}
                          {isAuthenticated && (
                            <div style={{ flexShrink: 0 }}>
                              <StarRating value={episode.userRating || 0} max={10} onChange={(r) => rateMutation.mutate({ episodeId: episode.id, rating: r })} size={14} />
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Reviews Section */}
        <div style={{ marginTop: 60 }}>
          <h2 style={{ fontFamily: 'var(--font-display)', fontSize: 28, letterSpacing: 2, marginBottom: 24 }}>
            <MessageSquare size={22} style={{ display: 'inline', marginRight: 10, color: 'var(--accent-light)' }} />
            REVIEWS
          </h2>

          {/* Write Review */}
          {isAuthenticated && (
            <div className="card" style={{ padding: 24, marginBottom: 24 }}>
              <h3 style={{ fontSize: 15, fontWeight: 600, marginBottom: 16 }}>Write a Review</h3>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
                <textarea
                  value={reviewText}
                  onChange={e => setReviewText(e.target.value)}
                  placeholder="Share your thoughts about this show..."
                  className="input"
                  rows={4}
                  style={{ resize: 'vertical' }}
                />
                <div style={{ display: 'flex', gap: 20, flexWrap: 'wrap', alignItems: 'center' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                    <span style={{ fontSize: 13, color: 'var(--text-secondary)' }}>Rating:</span>
                    <StarRating value={reviewRating} onChange={setReviewRating} />
                  </div>
                  <label style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13, color: 'var(--text-secondary)', cursor: 'pointer' }}>
                    <input type="checkbox" checked={reviewSpoilers} onChange={e => setReviewSpoilers(e.target.checked)} />
                    Contains spoilers
                  </label>
                  <button
                    onClick={() => reviewMutation.mutate()}
                    disabled={!reviewText.trim() || reviewRating === 0 || reviewMutation.isPending}
                    className="btn btn-primary btn-sm">
                    {reviewMutation.isPending ? 'Posting...' : 'Post Review'}
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Review List */}
          {show.reviews.length === 0 ? (
            <div style={{ textAlign: 'center', padding: '40px', color: 'var(--text-muted)', background: 'var(--bg-card)', borderRadius: 'var(--radius)', border: '1px solid var(--border)' }}>
              <p>No reviews yet. Be the first to share your thoughts!</p>
            </div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
              {show.reviews.map(review => (
                <div key={review.id} className="card" style={{ padding: 20 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 12 }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                      <div style={{ width: 36, height: 36, borderRadius: '50%', background: 'var(--accent)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 14, fontWeight: 600 }}>
                        {review.user.displayName[0].toUpperCase()}
                      </div>
                      <div>
                        <p style={{ fontWeight: 600, fontSize: 14 }}>{review.user.displayName}</p>
                        <p style={{ fontSize: 11, color: 'var(--text-muted)' }}>{new Date(review.createdAt).toLocaleDateString()}</p>
                      </div>
                    </div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                      <Star size={14} fill="var(--gold)" color="var(--gold)" />
                      <span style={{ fontWeight: 600 }}>{review.rating}</span>
                      <span style={{ color: 'var(--text-muted)', fontSize: 13 }}>/10</span>
                    </div>
                  </div>
                  {review.containsSpoilers && (
                    <span style={{ fontSize: 11, background: 'rgba(239,68,68,0.1)', color: 'var(--red)', border: '1px solid var(--red)', borderRadius: 4, padding: '2px 8px', marginBottom: 10, display: 'inline-block' }}>
                      ⚠ Spoilers
                    </span>
                  )}
                  <p style={{ color: 'var(--text-secondary)', lineHeight: 1.8, fontSize: 14 }}>{review.content}</p>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
