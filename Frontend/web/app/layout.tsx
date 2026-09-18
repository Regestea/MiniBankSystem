import "@minibank/shared/src/theme.css";
import type { Metadata } from "next";
import type React from "react";
import { BankShell } from "../components/BankShell";
import { Providers } from "../components/Providers";
import "./globals.css";

export const metadata: Metadata = {
  title: "Mini Bank — Water Banking",
  description: "Mini Bank desktop banking — live API.",
};

export default function RootLayout({ children }: { children: React.ReactNode }): React.JSX.Element {
  return (
    <html lang="en">
      <body>
        <Providers>
          <BankShell>{children}</BankShell>
        </Providers>
      </body>
    </html>
  );
}
