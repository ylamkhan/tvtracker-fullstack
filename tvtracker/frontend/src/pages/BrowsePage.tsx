import { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Search, Filter, ChevronLeft, ChevronRight } from 'lucide-react';
import { showsApi } from '../services/api';
import ShowCard from '../components/ShowCard';

const GENRES = ['All', 'Drama', 'Comedy', 'Crime', 'Sci-Fi', 'Fantasy', 'Horror', 'Action', 'Mystery', 'History'];
const STATUSES = [{ value: '', label: 'All' }, { value: 'Ongoing', label: 'Airing' }, { value: 'Ended', label: 'Ended' }, { value: 'Cancelled', label: 'Cancelled' }];

export default function BrowsePage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [search, setSearch] = useState(searchParams.get('search') || '');
  const [genre, setGenre] = useState('');
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);

  useEffect(() => {
    const s = searchParams.get('search');
    if (s) setSearch(s);
  }, [searchParams]);

  const { data, isLoading } = useQuery({
    queryKey: ['shows', 'browse', search, genre, status, page],
    queryFn: () => showsApi.getAll({ search: search || undefined, genre: genre || undefined, status: status || undefined, page, pageSize: 20 }),
  });

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    if (search) setSearchParams({ search });
    else setSearchParams({});
  };

  return (
    <div style={{ padding: '40px 0 60px' }}>
      <div className="container">
        <h1 style={{ fontFamily: 'var(--font-display)', fontSize: 42, letterSpacing: 3, marginBottom: 32 }}>
          BROWSE SHOWS
        </h1>

        {/* Filters */}
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border)', borderRadius: 'var(--radius)', padding: 20, marginBottom: 32, display: 'flex', gap: 16, flexWrap: 'wrap', alignItems: 'center' }}>
          <form onSubmit={handleSearch} style={{ display: 'flex', gap: 10, flex: 1, minWidth: 240 }}>
            <div style={{ position: 'relative', flex: 1 }}>
              <Search size={16} style={{ position: 'absolute', left: 12, top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)', pointerEvents: 'none' }} />
              <input className="input" value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by title..." style={{ paddingLeft: 38 }} />
            </div>
            <button type="submit" className="btn btn-primary btn-sm">Search</button>
          </form>

          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <Filter size={15} color="var(--text-muted)" />
            <select value={status} onChange={e => { setStatus(e.target.value); setPage(1); }} className="input" style={{ width: 'auto', paddingRight: 32, fontSize: 13 }}>
              {STATUSES.map(s => <option key={s.value} value={s.value}>{s.label}</option>)}
            </select>
          </div>
        </div>

        {/* Genre filters */}
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 32 }}>
          {GENRES.map(g => (
            <button key={g} onClick={() => { setGenre(g === 'All' ? '' : g); setPage(1); }}
              style={{
                padding: '6px 16px', borderRadius: 20, fontSize: 13, fontWeight: 500,
                background: (g === 'All' ? !genre : genre === g) ? 'var(--accent)' : 'var(--bg-card)',
                color: (g === 'All' ? !genre : genre === g) ? 'white' : 'var(--text-secondary)',
                border: '1px solid',
                borderColor: (g === 'All' ? !genre : genre === g) ? 'var(--accent)' : 'var(--border)',
                cursor: 'pointer', transition: 'all 0.2s',
                boxShadow: (g === 'All' ? !genre : genre === g) ? '0 0 12px var(--accent-glow)' : 'none',
              }}>
              {g}
            </button>
          ))}
        </div>

        {/* Results */}
        {isLoading ? (
          <div style={{ display: 'flex', justifyContent: 'center', padding: 80 }}>
            <div className="loading-spinner" />
          </div>
        ) : data?.items.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '80px 0', color: 'var(--text-muted)' }}>
            <p style={{ fontSize: 48, marginBottom: 16 }}>🔍</p>
            <p style={{ fontSize: 18 }}>No shows found</p>
            <p style={{ fontSize: 14, marginTop: 8 }}>Try different search terms or filters</p>
          </div>
        ) : (
          <>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
              <p style={{ color: 'var(--text-secondary)', fontSize: 14 }}>
                {data?.totalCount} show{data?.totalCount !== 1 ? 's' : ''} found
              </p>
              <p style={{ color: 'var(--text-muted)', fontSize: 13 }}>Page {page} of {data?.totalPages}</p>
            </div>

            <div className="grid-shows fade-in">
              {data?.items.map(show => <ShowCard key={show.id} show={show} />)}
            </div>

            {/* Pagination */}
            {data && data.totalPages > 1 && (
              <div style={{ display: 'flex', justifyContent: 'center', gap: 8, marginTop: 40 }}>
                <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1} className="btn btn-ghost btn-sm">
                  <ChevronLeft size={16} /> Prev
                </button>
                {Array.from({ length: Math.min(5, data.totalPages) }, (_, i) => {
                  const pageNum = Math.max(1, Math.min(page - 2 + i, data.totalPages - 4 + i));
                  return (
                    <button key={pageNum} onClick={() => setPage(pageNum)}
                      className="btn btn-sm"
                      style={{ background: page === pageNum ? 'var(--accent)' : 'var(--bg-card)', color: page === pageNum ? 'white' : 'var(--text-secondary)', border: '1px solid var(--border)' }}>
                      {pageNum}
                    </button>
                  );
                })}
                <button onClick={() => setPage(p => Math.min(data.totalPages, p + 1))} disabled={page === data.totalPages} className="btn btn-ghost btn-sm">
                  Next <ChevronRight size={16} />
                </button>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
