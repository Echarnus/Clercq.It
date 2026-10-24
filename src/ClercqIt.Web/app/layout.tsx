import type React from "react";
import type { Metadata } from "next";
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
