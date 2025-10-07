import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Home, Search, FileQuestion } from "lucide-react";

export default function NotFound() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-blue-50 to-slate-100 dark:from-slate-900 dark:via-slate-900 dark:to-slate-800 flex items-center justify-center px-4">
      <div className="max-w-2xl w-full text-center">
        {/* Animated 404 Illustration */}
        <div className="relative mb-8">
          <div className="relative">
            {/* Large 404 Text with gradient */}
            <h1 className="text-[150px] md:text-[200px] font-bold leading-none bg-gradient-to-br from-slate-900 via-blue-600 to-slate-700 dark:from-slate-100 dark:via-blue-400 dark:to-slate-300 bg-clip-text text-transparent select-none">
              404
            </h1>
            
            {/* Floating icon */}
            <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 animate-bounce">
              <FileQuestion className="w-16 h-16 md:w-20 md:h-20 text-blue-500 dark:text-blue-400 opacity-50" strokeWidth={1.5} />
            </div>
          </div>
          
          {/* Decorative elements */}
          <div className="absolute -top-4 -left-4 w-24 h-24 bg-blue-500/10 dark:bg-blue-400/10 rounded-full blur-xl animate-pulse"></div>
          <div className="absolute -bottom-4 -right-4 w-32 h-32 bg-purple-500/10 dark:bg-purple-400/10 rounded-full blur-xl animate-pulse delay-700"></div>
        </div>

        {/* Message */}
        <div className="space-y-4 mb-8">
          <h2 className="text-3xl md:text-4xl font-bold text-slate-900 dark:text-white">
            Page Not Found
          </h2>
          <p className="text-lg text-slate-600 dark:text-slate-400 max-w-md mx-auto">
            Oops! The page you&apos;re looking for seems to have wandered off into the digital void.
          </p>
        </div>

        {/* Action Buttons */}
        <div className="flex flex-col sm:flex-row gap-4 justify-center items-center">
          <Link href="/">
            <Button size="lg" className="w-full sm:w-auto gap-2 bg-gradient-to-r from-blue-600 to-blue-700 hover:from-blue-700 hover:to-blue-800 dark:from-blue-500 dark:to-blue-600 dark:hover:from-blue-600 dark:hover:to-blue-700">
              <Home className="w-4 h-4" />
              Back to Home
            </Button>
          </Link>
          
          <Link href="/portfolio">
            <Button size="lg" variant="outline" className="w-full sm:w-auto gap-2 border-2">
              <Search className="w-4 h-4" />
              Explore Portfolio
            </Button>
          </Link>
        </div>

        {/* Helpful suggestion */}
        <div className="mt-12 pt-8 border-t border-slate-200 dark:border-slate-700">
          <p className="text-sm text-slate-500 dark:text-slate-500">
            Try using the navigation menu above or return to the{" "}
            <Link href="/" className="text-blue-600 dark:text-blue-400 hover:underline font-medium">
              homepage
            </Link>
            {" "}to find what you&apos;re looking for.
          </p>
        </div>

        {/* Fun error code */}
        <div className="mt-6">
          <p className="text-xs text-slate-400 dark:text-slate-600 font-mono">
            ERROR_PAGE_NOT_FOUND_404
          </p>
        </div>
      </div>
    </div>
  );
}
