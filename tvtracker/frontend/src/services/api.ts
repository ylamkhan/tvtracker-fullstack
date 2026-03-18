import axios from 'axios';
import type { AuthResponse, PagedResult, Show, ShowDetail, UserShow, UserStats, ShowReview } from '../types';

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      window.location.href = '/login';
    }
    return Promise.reject(err);
  }
);

// Auth
export const authApi = {
  register: (data: { email: string; password: string; displayName: string }) =>
    api.post<AuthResponse>('/auth/register', data).then(r => r.data),
  login: (data: { email: string; password: string }) =>
    api.post<AuthResponse>('/auth/login', data).then(r => r.data),
};

// Shows
export const showsApi = {
  getAll: (params?: { search?: string; genre?: string; status?: string; page?: number; pageSize?: number }) =>
    api.get<PagedResult<Show>>('/shows', { params }).then(r => r.data),
  getById: (id: number) =>
    api.get<ShowDetail>(`/shows/${id}`).then(r => r.data),
  create: (data: Partial<Show>) =>
    api.post<Show>('/shows', data).then(r => r.data),
  track: (id: number, data: { status?: string; userRating?: number; isFavorite?: boolean; notes?: string }) =>
    api.post<UserShow>(`/shows/${id}/track`, data).then(r => r.data),
  untrack: (id: number) =>
    api.delete(`/shows/${id}/track`),
  getReviews: (id: number) =>
    api.get<ShowReview[]>(`/shows/${id}/reviews`).then(r => r.data),
  createReview: (id: number, data: { content: string; rating: number; containsSpoilers: boolean }) =>
    api.post<ShowReview>(`/shows/${id}/reviews`, data).then(r => r.data),
};

// Episodes
export const episodesApi = {
  markWatched: (id: number) =>
    api.post(`/episodes/${id}/watch`).then(r => r.data),
  unmarkWatched: (id: number) =>
    api.delete(`/episodes/${id}/watch`).then(r => r.data),
  rate: (id: number, data: { rating: number; comment?: string }) =>
    api.post(`/episodes/${id}/rate`, data).then(r => r.data),
};

// User
export const userApi = {
  getList: (status?: string) =>
    api.get<UserShow[]>('/user/list', { params: status ? { status } : {} }).then(r => r.data),
  getStats: () =>
    api.get<UserStats>('/user/stats').then(r => r.data),
};

export default api;
