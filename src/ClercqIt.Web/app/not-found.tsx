import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Home, Search, FileQuestion } from "lucide-react";

export default function NotFound() {
  return (
    <div
      className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800"
      style={{ fontFamily: "Arial, sans-serif" }}
    >
      <section className="container mx-auto px-4 py-16">
        <div className="max-w-4xl mx-auto text-center">
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
            <p className="text-xl text-slate-600 leading-relaxed dark:text-slate-300">
              Oops! The page you&apos;re looking for seems to have wandered off into the digital void.
            </p>
          </div>

          {/* Action Buttons */}
          <div className="flex flex-col sm:flex-row gap-4 justify-center items-center">
            <Button
              asChild
              size="lg"
              className="w-full sm:w-auto bg-slate-900 hover:bg-slate-800 dark:bg-white dark:text-slate-900 dark:hover:bg-slate-200"
            >
              <Link href="/">
                <Home className="mr-2 h-4 w-4" />
                Back to Home
              </Link>
            </Button>
            
            <Button asChild size="lg" variant="outline" className="w-full sm:w-auto">
              <Link href="/portfolio">
                <Search className="mr-2 h-4 w-4" />
                Explore Portfolio
              </Link>
            </Button>
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
      </section>
    </div>
  );
}
