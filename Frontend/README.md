# Mini Bank — Frontend

Mock-data frontend prototype for the Mini Bank banking application (Water / Ocean theme, single theme).

```
Frontend/
├── shared/    # @minibank/shared — theme, types, mock-data, services, store, reusable UI
├── web/       # minibank-web — Next.js desktop banking dashboard
└── mobile/    # minibank-mobile — React + Vite + Capacitor Android app (feature-based)
```

## Architecture

```
UI (web / mobile)
  ↓  useBank() / useTransferFlow() / useTopUpFlow()
BankContext (mock frontend state: balance, transactions, recipients)
  ↓  IBankService
MockBankService  ──(later)──▶  ApiBankService (real backend)
```

All screens consume mock data through `IBankService` / `BankContext`.
Replacing `MockBankService` with a real API implementation requires no UI changes.

## Backend mapping (for later)

| Mock method | Real endpoint |
|---|---|
| `getProfile()` | `GET /customers/profile` + `GET /customers/overview` |
| `previewRecipient(accountNumber)` | `POST /transfers/preview-by-number` |
| `transfer(...)` | `POST /transfers/by-account-number` |
| `topUp(amount)` | `POST /accounts/{id}/topup` |
| `getBeneficiaries()` | `GET /beneficiaries` |
| `saveBeneficiary(...)` | `POST /beneficiaries` |
| `getTransactions()` | `GET /transactions/mine` |

## Run

```bash
cd Frontend
npm install
npm run dev:web      # Next.js → http://localhost:3000
npm run dev:mobile   # Vite → http://localhost:5173
```

## Notes

- Styling: CSS Modules only, design tokens in `shared/src/theme.css`. No Tailwind.
- Single Water/Ocean theme. Scenic areas are CSS gradient placeholders —
  swap `background: linear-gradient(...)` for `background-image: url(...)` later.
- Currency: USD only. Account numbers: `IR-XXXXXXXXXX` or 16 digits.
