// ── Auth ──────────────────────────────────────────────────────────────────────
export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  email: string;
  role: string;
}

export interface RegisterPayload {
  email: string;
  password: string;
}

export interface LoginPayload {
  email: string;
  password: string;
}

// ── Accounts ──────────────────────────────────────────────────────────────────
export type AccountStatus = 'Active' | 'Frozen' | 'Closed';

export interface AccountDto {
  id: string;
  iban: string;
  balance: number;
  currency: string;
  status: AccountStatus;
  createdAt: string;
}

// ── Transactions / Transfers ──────────────────────────────────────────────────
export type TransactionStatus = 'Pending' | 'Complated' | 'Flagged' | 'Rejected';

export interface TransferDto {
  id: string;
  fromAccountId: string;
  toAccountId: string;
  amount: number;
  currency: string;
  status: TransactionStatus;
  createdAt: string;
}

export interface TransactionPageDto {
  items: TransferDto[];
  page: number;
  pageSize: number;
}

export interface TransferDetailDto {
  id: string;
  fromAccountId: string;
  toAccountId: string;
  amount: number;
  currency: string;
  status: TransactionStatus;
  fraudScore: number;
  fraudSignals: string;
  createdAt: string;
}

export interface TransferPayload {
  fromAccountId: string;
  toAccountId: string;
  amount: number;
  description?: string;
}
