import Link from "next/link";
import { ThemeToggle } from "@/components/theme-toggle";
import { PageContainer } from "@/components/page-container";
import { Navigation } from "./navigation";

export function Header() {
  return (
    <header className="sticky top-0 z-50 border-b bg-white/80 backdrop-blur-xs dark:border-slate-800 dark:bg-slate-900/80">
      <PageContainer className="flex items-center justify-between py-4">
        <Link href="/" aria-label="Clercq.It, go to the homepage" className="block">
          <span className="block text-4xl leading-tight tracking-[0.31rem] text-slate-900 dark:text-white">
            <span className="font-black">C</span>
            <span className="font-thin">lercq</span>
            <span className="font-black">I</span>
            <span className="font-thin">t</span>
          </span>
          <span className="block text-sm tracking-tight text-slate-700 dark:text-slate-300">
            <span className="font-bold">C</span>
            <span className="font-light">ontinuous </span>
            <span className="font-bold">D</span>
            <span className="font-light">evelopment</span>
          </span>
        </Link>
        <div className="flex items-center gap-4">
          <Navigation />
          <ThemeToggle />
        </div>
      </PageContainer>
    </header>
  );
}
