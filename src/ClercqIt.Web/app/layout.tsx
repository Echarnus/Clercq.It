import type React from "react";
import type { Metadata, Viewport } from "next";
import "./globals.css";
import { ThemeProvider } from "@/components/theme-provider";
import { ConditionalLayout } from "@/components/conditional-layout";
import { StoreProvider } from "@/lib/store/StoreProvider";
import { AUTHOR_NAME, DEFAULT_OG_IMAGE, SITE_DESCRIPTION, SITE_NAME, SITE_URL } from "@/lib/seo";

const DEFAULT_TITLE = `${AUTHOR_NAME} - Developer & Solution Architect`;

// Site-wide defaults; each public page sets its own title, description and canonical URL
// through pageMetadata() (docs/seo.md).
export const metadata: Metadata = {
  metadataBase: new URL(SITE_URL),
  title: {
    default: DEFAULT_TITLE,
    template: `%s | ${SITE_NAME}`,
  },
  description: SITE_DESCRIPTION,
  applicationName: SITE_NAME,
  authors: [{ name: AUTHOR_NAME, url: SITE_URL }],
  creator: AUTHOR_NAME,
  robots: { index: true, follow: true },
  openGraph: {
    type: "website",
    siteName: SITE_NAME,
    locale: "en_US",
    title: DEFAULT_TITLE,
    description: SITE_DESCRIPTION,
    images: [{ url: DEFAULT_OG_IMAGE, width: 1200, height: 630, alt: SITE_NAME }],
  },
  twitter: { card: "summary_large_image" },
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
