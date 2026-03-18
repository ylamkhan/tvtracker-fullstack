export interface User {
  id: string;
  email: string;
  displayName: string;
  avatarUrl?: string;
  createdAt: string;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  user: User;
}

export interface Show {
  id: number;
  title: string;
  description?: string;
  posterUrl?: string;
  backdropUrl?: string;
  genre?: string;
  network?: string;
  status: string;
  averageRating: number;
  ratingCount: number;
  firstAirDate?: string;
  lastAirDate?: string;
  seasonsCount: number;
  episodesCount: number;
  userStatus?: string;
  userRating?: number;
  isFavorite: boolean;
}

export interface ShowDetail extends Show {
  seasons: Season[];
  reviews: ShowReview[];
}

export interface Season {
  id: number;
  showId: number;
  seasonNumber: number;
  title?: string;
  description?: string;
  posterUrl?: string;
  airDate?: string;
  episodes: Episode[];
}

export interface Episode {
  id: number;
  seasonId: number;
  episodeNumber: number;
  title: string;
  description?: string;
  durationMinutes?: number;
  airDate?: string;
  thumbnailUrl?: string;
  averageRating: number;
  ratingCount: number;
  isWatched: boolean;
  userRating?: number;
}

export interface UserShow {
  id: number;
  show: Show;
  status: string;
  userRating?: number;
  isFavorite: boolean;
  addedAt: string;
  startedAt?: string;
  finishedAt?: string;
  notes?: string;
  watchedEpisodes: number;
  totalEpisodes: number;
}

export interface ShowReview {
  id: number;
  user: User;
  content: string;
  rating: number;
  containsSpoilers: boolean;
  createdAt: string;
}

export interface UserStats {
  totalShows: number;
  watchingShows: number;
  completedShows: number;
  planToWatchShows: number;
  totalEpisodesWatched: number;
  totalMinutesWatched: number;
  averageRating: number;
  recentlyWatched: Show[];
  favorites: Show[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export type WatchStatus = 'Watching' | 'Completed' | 'OnHold' | 'Dropped' | 'PlanToWatch';

export const WATCH_STATUS_LABELS: Record<WatchStatus, string> = {
  Watching: 'Watching',
  Completed: 'Completed',
  OnHold: 'On Hold',
  Dropped: 'Dropped',
  PlanToWatch: 'Plan to Watch',
};

export const WATCH_STATUS_COLORS: Record<WatchStatus, string> = {
  Watching: '#22c55e',
  Completed: '#3b82f6',
  OnHold: '#f59e0b',
  Dropped: '#ef4444',
  PlanToWatch: '#8b5cf6',
};
