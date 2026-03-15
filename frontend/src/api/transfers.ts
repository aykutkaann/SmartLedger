import { apiClient } from './client';
import type { TransactionPageDto, TransferDetailDto, TransferDto, TransferPayload } from '../types';

export const transfersApi = {
  initiate: (payload: TransferPayload) =>
    apiClient.post<TransferDto>('/transfers', payload).then((r) => r.data),

  getDetail: (id: string) =>
    apiClient.get<TransferDetailDto>(`/transfers/${id}`).then((r) => r.data),

  getHistory: (accountId: string, page = 1, pageSize = 20) =>
    apiClient
      .get<TransactionPageDto>(`/accounts/${accountId}/transactions`, {
        params: { page, pageSize },
      })
      .then((r) => r.data),
};
