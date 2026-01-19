import { ThemeToggle } from "@/components/theme-toggle";
import { Navigation } from "./navigation";

export function Header() {
  return (
    <header className="border-b bg-white/80 backdrop-blur-sm sticky top-0 z-50 dark:bg-slate-900/80 dark:border-slate-800">
      <div className="container mx-auto px-4 py-4 flex justify-between items-center">
        <div>
          <h1 className="text-4xl text-slate-900 leading-tight tracking-[0.31rem] dark:text-white">
            <span className="font-black">C</span>
            <span className="font-thin">lercq</span>
            <span className="font-black">I</span>
            <span className="font-thin">t</span>
          </h1>
          <p className="text-sm text-slate-700 tracking-tight dark:text-slate-300">
            <span className="font-bold">C</span>
            <span className="font-light">ontinuous </span>
            <span className="font-bold">D</span>
            <span className="font-light">evelopment</span>
          </p>
        </div>        <div className="flex items-center gap-4">
          <Navigation />
          <ThemeToggle />
        </div>
      </div>
    </header>
  );
}
