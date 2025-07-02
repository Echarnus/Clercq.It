import type React from "react"
import type { Metadata } from "next"
import { Inter } from "next/font/google"
import "./globals.css"
import { ThemeProvider } from "@/components/theme-provider"
import { Footer } from "@/components/footer"
import { Header } from "@/components/header"

const inter = Inter({ subsets: ["latin"] })

export const metadata: Metadata = {
  title: "Clercqlt - Continuous Development",
  description: "Full Stack Developer specializing in .NET, TypeScript, React, Angular, and Cloud Technologies",
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body className={`${inter.className} font-sans`} style={{ fontFamily: "Arial, sans-serif" }}>
        <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
          <Header currentPage={"home"} />
          {children}
          <Footer />
        </ThemeProvider>
      </body>
    </html>
  )
}
