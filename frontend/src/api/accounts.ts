import { apiClient } from './client';
import type { AccountDto } from '../types';

export const accountsApi = {
  list: () =>
    apiClient.get<AccountDto[]>('/accounts').then((r) => r.data),

  create: (currency: string) =>
    apiClient.post<AccountDto>('/accounts', { currency }).then((r) => r.data),

  close: (id: string) =>
    apiClient.delete(`/accounts/${id}`).then((r) => r.data),
};
