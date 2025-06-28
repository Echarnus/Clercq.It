import Link from "next/link"

interface HeaderProps {
  currentPage: "home" | "portfolio"
}

export function Header({ currentPage }: HeaderProps) {
  return (
    <header className="border-b bg-white/80 backdrop-blur-sm sticky top-0 z-50">
      <div className="container mx-auto px-4 py-4 flex justify-between items-center">
        <div>
          <h1 className="text-4xl text-slate-900 leading-tight tracking-wider">
            <span className="font-black">C</span>
            <span className="font-thin">lercq</span>
            <span className="font-black">l</span>
            <span className="font-thin">t</span>
          </h1>
          <p className="text-sm text-slate-700 tracking-[-0.05rem]">
            <span className="font-bold">C</span>
            <span className="font-light">ontinuous </span>
            <span className="font-bold">D</span>
            <span className="font-light">evelopment</span>
          </p>
        </div>
        <nav className="flex gap-6">
          <Link
            href="/"
            className={
              currentPage === "home"
                ? "text-slate-900 font-medium border-b-2 border-slate-900"
                : "text-slate-600 hover:text-slate-900 transition-colors hover:border-b-2 hover:border-slate-600"
            }
          >
            Home
          </Link>
          <Link
            href="/portfolio"
            className={
              currentPage === "portfolio"
                ? "text-slate-900 font-medium border-b-2 border-slate-900"
                : "text-slate-600 hover:text-slate-900 transition-colors hover:border-b-2 hover:border-slate-600"
            }
          >
            Portfolio
          </Link>
        </nav>
      </div>
    </header>
  )
}
