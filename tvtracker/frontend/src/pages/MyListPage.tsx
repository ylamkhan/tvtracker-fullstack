import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { List, Eye, CheckCircle, Clock, XCircle, BookmarkPlus } from 'lucide-react';
import { Link } from 'react-router-dom';
import { userApi } from '../services/api';
import ShowCard from '../components/ShowCard';
import type { WatchStatus } from '../types';
import { WATCH_STATUS_LABELS, WATCH_STATUS_COLORS } from '../types';

const TABS: { key: string; label: string; icon: React.ReactNode }[] = [
  { key: '', label: 'All', icon: <List size={15} /> },
  { key: 'Watching', label: 'Watching', icon: <Eye size={15} /> },
  { key: 'Completed', label: 'Completed', icon: <CheckCircle size={15} /> },
  { key: 'PlanToWatch', label: 'Plan to Watch', icon: <BookmarkPlus size={15} /> },
  { key: 'OnHold', label: 'On Hold', icon: <Clock size={15} /> },
  { key: 'Dropped', label: 'Dropped', icon: <XCircle size={15} /> },
];

export default function MyListPage() {
  const [activeTab, setActiveTab] = useState('');

  const { data: userShows, isLoading } = useQuery({
    queryKey: ['user-list', activeTab],
    queryFn: () => userApi.getList(activeTab || undefined),
  });

  const countByStatus = (status: string) => userShows?.filter(u => status ? u.status === status : true).length || 0;

  return (
    <div style={{ padding: '40px 0 60px' }}>
      <div className="container">
        <h1 style={{ fontFamily: 'var(--font-display)', fontSize: 42, letterSpacing: 3, marginBottom: 8 }}>MY LIST</h1>
        <p style={{ color: 'var(--text-secondary)', marginBottom: 32 }}>
          {userShows?.length || 0} shows tracked
        </p>

        {/* Status Tabs */}
        <div style={{ display: 'flex', gap: 6, overflowX: 'auto', marginBottom: 32, paddingBottom: 4 }}>
          {TABS.map(tab => {
            const active = activeTab === tab.key;
            const color = tab.key ? WATCH_STATUS_COLORS[tab.key as WatchStatus] : 'var(--accent)';
            return (
              <button key={tab.key} onClick={() => setActiveTab(tab.key)}
                style={{
                  display: 'flex', alignItems: 'center', gap: 8, padding: '8px 18px',
                  borderRadius: 8, fontSize: 13, fontWeight: 500, whiteSpace: 'nowrap',
                  background: active ? color + '22' : 'var(--bg-card)',
                  color: active ? color : 'var(--text-secondary)',
                  border: `1px solid ${active ? color : 'var(--border)'}`,
                  cursor: 'pointer', transition: 'all 0.2s',
                  boxShadow: active ? `0 0 12px ${color}33` : 'none',
                }}>
                {tab.icon} {tab.label}
                {tab.key !== activeTab && userShows && (
                  <span style={{ fontSize: 11, background: 'var(--bg-surface)', borderRadius: 10, padding: '1px 7px', color: 'var(--text-muted)' }}>
                    {tab.key ? userShows.filter(u => u.status === tab.key).length : userShows.length}
                  </span>
                )}
              </button>
            );
          })}
        </div>

        {isLoading ? (
          <div style={{ display: 'flex', justifyContent: 'center', padding: 80 }}>
            <div className="loading-spinner" />
          </div>
        ) : !userShows || userShows.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '80px 0' }}>
            <p style={{ fontSize: 48, marginBottom: 16 }}>📺</p>
            <p style={{ fontSize: 18, color: 'var(--text-secondary)' }}>
              {activeTab ? `No shows in "${WATCH_STATUS_LABELS[activeTab as WatchStatus]}"` : 'Your list is empty'}
            </p>
            <p style={{ fontSize: 14, color: 'var(--text-muted)', marginTop: 8, marginBottom: 24 }}>
              Browse shows and add them to your list!
            </p>
            <Link to="/browse" className="btn btn-primary">Browse Shows</Link>
          </div>
        ) : (
          <div className="grid-shows fade-in">
            {userShows.map(userShow => (
              <ShowCard
                key={userShow.id}
                show={userShow.show}
                showProgress={userShow.status === 'Watching'}
                watchedEpisodes={userShow.watchedEpisodes}
                totalEpisodes={userShow.totalEpisodes}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
