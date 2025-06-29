import Link from "next/link"
import { ThemeToggle } from "@/components/theme-toggle"

interface HeaderProps {
  currentPage: "home" | "portfolio"
}

export function Header({ currentPage }: HeaderProps) {
  return (
    <header className="border-b bg-white/80 backdrop-blur-sm sticky top-0 z-50 dark:bg-slate-900/80 dark:border-slate-800">
      <div className="container mx-auto px-4 py-4 flex justify-between items-center">
        <div>
          <h1 className="text-4xl text-slate-900 leading-tight tracking-wider dark:text-white">
            <span className="font-black">C</span>
            <span className="font-thin">lercq</span>
            <span className="font-black">l</span>
            <span className="font-thin">t</span>
          </h1>
          <p className="text-sm text-slate-700 tracking-[-0.05rem] dark:text-slate-300">
            <span className="font-bold">C</span>
            <span className="font-light">ontinuous </span>
            <span className="font-bold">D</span>
            <span className="font-light">evelopment</span>
          </p>
        </div>
        <div className="flex items-center gap-4">
          <nav className="flex gap-6">
            <Link
              href="/"
              className={
                currentPage === "home"
                  ? "text-slate-900 font-medium border-b-2 border-slate-900 dark:text-white dark:border-white"
                  : "text-slate-600 hover:text-slate-900 transition-colors hover:border-b-2 hover:border-slate-600 dark:text-slate-400 dark:hover:text-white dark:hover:border-slate-400"
              }
            >
              Home
            </Link>
            <Link
              href="/portfolio"
              className={
                currentPage === "portfolio"
                  ? "text-slate-900 font-medium border-b-2 border-slate-900 dark:text-white dark:border-white"
                  : "text-slate-600 hover:text-slate-900 transition-colors hover:border-b-2 hover:border-slate-600 dark:text-slate-400 dark:hover:text-white dark:hover:border-slate-400"
              }
            >
              Portfolio
            </Link>
          </nav>
          <ThemeToggle />
        </div>
      </div>
    </header>
  )
}
