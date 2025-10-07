import Link from "next/link";
import { FileQuestion } from "lucide-react";

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
              {/* Large 404 Text - solid color matching text */}
              <h1 className="text-[150px] md:text-[200px] font-bold leading-none text-slate-900 dark:text-white select-none">
                404
              </h1>
              
              {/* Floating icon */}
              <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 animate-bounce">
                <FileQuestion className="w-16 h-16 md:w-20 md:h-20 text-slate-400 dark:text-slate-500 opacity-50" strokeWidth={1.5} />
              </div>
            </div>
            
            {/* Decorative elements */}
            <div className="absolute -top-4 -left-4 w-24 h-24 bg-slate-200/50 dark:bg-slate-700/50 rounded-full blur-xl animate-pulse"></div>
            <div className="absolute -bottom-4 -right-4 w-32 h-32 bg-slate-300/50 dark:bg-slate-600/50 rounded-full blur-xl animate-pulse delay-700"></div>
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

          {/* Helpful suggestion */}
          <div className="mt-12 pt-8 border-t border-slate-200 dark:border-slate-700">
            <p className="text-sm text-slate-500 dark:text-slate-500">
              Try using the navigation menu above or return to the{" "}
              <Link href="/" className="text-slate-900 dark:text-white hover:underline font-medium">
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
