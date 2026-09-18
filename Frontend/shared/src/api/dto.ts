/** Backend DTOs — mirror the C# records 1:1 (camelCase via System.Text.Json defaults). */

export interface CustomerResponseDto {
  customerId: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  status: string;
  createdAt: string;
}

export interface CustomerDetailDto extends CustomerResponseDto {}

export interface OverviewAccountDto {
  accountId: string;
  accountNumber: string;
  accountType: string;
  status: string;
  balance: number;
  createdAt: string;
}

export interface CustomerOverviewDto {
  customerId: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  status: string;
  createdAt: string;
  accounts: OverviewAccountDto[];
  totalBalance: number;
}

export interface AccountDto {
  accountId: string;
  accountNumber: string;
  accountType: string;
  status: string;
  balance: number;
  createdAt: string;
}

export interface TransactionResponseDto {
  transactionId: string;
  type: string;
  amount: number;
  referenceId: string;
  occurredOn: string;
}

export interface TransferResponseDto {
  transactionId: string;
  amount: number;
  referenceId: string;
  fromAccountId: string;
  toAccountId: string;
  occurredOn: string;
}

export interface PreviewTransferDto {
  toAccountId: string;
  accountNumber: string;
  holderFullName: string;
  maskedHolderName: string;
}

export interface MyTransactionDto {
  transactionId: string;
  type: string;
  amount: number;
  sourceAccountNumber?: string | null;
  destinationAccountNumber?: string | null;
  occurredOn: string;
  referenceId: string;
  description?: string | null;
}

export interface MyTransactionsDto {
  page: number;
  pageSize: number;
  total: number;
  items: MyTransactionDto[];
}

export interface BeneficiaryDto {
  beneficiaryId: string;
  accountNumber: string;
  holderName: string;
  createdAt: string;
}

export interface LoginResponseDto {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

export interface RegisterRequestDto {
  email: string;
  password: string;
  fullName: string;
  phoneNumber: string;
}
