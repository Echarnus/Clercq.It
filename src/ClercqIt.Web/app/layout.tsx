import type React from "react";
import type { Metadata, Viewport } from "next";
import "./globals.css";
import { ThemeProvider } from "@/components/theme-provider";
import { ConditionalLayout } from "@/components/conditional-layout";
import { StoreProvider } from "@/lib/store/StoreProvider";

export const metadata: Metadata = {
  title: {
    default: "Clercq.It - Continuous Development",
    template: "%s | Clercq.It",
  },
  description:
    "Developer and Solution Architect specializing in .NET, TypeScript, React, Angular, and Cloud Technologies",
};

// Browser chrome (mobile address bar) follows the OS scheme: the light header is white,
// the dark page shell is slate-900 (docs/ux.md#themes).
export const viewport: Viewport = {
  themeColor: [
    { media: "(prefers-color-scheme: light)", color: "#ffffff" },
    { media: "(prefers-color-scheme: dark)", color: "#0f172a" },
  ],
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body className="font-sans">
        <StoreProvider>
          <ThemeProvider
            attribute="class"
            defaultTheme="system"
            enableSystem
            disableTransitionOnChange
          >
            <ConditionalLayout>{children}</ConditionalLayout>
          </ThemeProvider>
        </StoreProvider>
      </body>
    </html>
  );
}
