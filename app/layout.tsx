import type React from "react"
import type { Metadata } from "next"
import { Inter } from "next/font/google"
import "./globals.css"

const inter = Inter({ subsets: ["latin"] })

export const metadata: Metadata = {
  title: "Clercqlt - Continuous Development",
  description: "Full Stack Developer specializing in .NET, TypeScript, React, Angular, and Cloud Technologies",
    generator: 'v0.dev'
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en">
      <body className={`${inter.className} font-sans`} style={{ fontFamily: "Arial, sans-serif" }}>
        {children}
      </body>
    </html>
  )
}
