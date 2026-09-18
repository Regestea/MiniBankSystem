/**
 * Barrel export — deliberately does NOT import theme.css here.
 * Each app imports the single global stylesheet once at its entrypoint:
 *   web:    app/layout.tsx → import "@minibank/shared/src/theme.css"
 *   mobile: src/main.tsx   → (via app/App → theme.css)
 * This keeps Next.js global-CSS rules happy and avoids duplicate injection.
 */

export * from "./types";
export * from "./constants";
export * from "./constants/imageUrls";
export * from "./utils/format";
export * from "./utils/validation";
export * from "./utils/authValidation";
export * from "./mock-data/mock";
export * from "./services/types";
export * from "./services/mockBankService";
export * from "./api/config";
export * from "./api/tokenStorage";
export * from "./api/client";
export * from "./api/dto";
export * from "./api/authApi";
export * from "./api/apiBankService";
export * from "./store/BankContext";
export * from "./store/AuthContext";
export * from "./hooks/useTransferFlow";
export * from "./hooks/useTopUpFlow";

export { Button } from "./components/Button/Button";
export { OceanBackground } from "./components/OceanBackground/OceanBackground";
export { Input } from "./components/Input/Input";
export { AmountInput } from "./components/AmountInput/AmountInput";
export { AccountNumberInput } from "./components/AccountNumberInput/AccountNumberInput";
export { Checkbox } from "./components/Checkbox/Checkbox";
export { Card } from "./components/Card/Card";
export { BalanceCard } from "./components/BalanceCard/BalanceCard";
export { TransactionItem } from "./components/TransactionItem/TransactionItem";
export { TransactionList } from "./components/TransactionList/TransactionList";
export { RecipientCard } from "./components/RecipientCard/RecipientCard";
export { Avatar } from "./components/Avatar/Avatar";
export { SectionHeader } from "./components/SectionHeader/SectionHeader";
export { EmptyState } from "./components/EmptyState/EmptyState";
export { LoadingState } from "./components/LoadingState/LoadingState";
export { SuccessState } from "./components/SuccessState/SuccessState";
export { PageHeader } from "./components/PageHeader/PageHeader";
export { Stepper } from "./components/Stepper/Stepper";
export { Modal } from "./components/Modal/Modal";
