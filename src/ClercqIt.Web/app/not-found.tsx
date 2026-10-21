import Link from "next/link";
import { FileQuestion } from "lucide-react";

export default function NotFound() {
  return (
    <div
      className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800"
      style={{ fontFamily: "Arial, sans-serif" }}
    >
      <section className="container mx-auto px-4 py-16">
        <div className="mx-auto max-w-4xl text-center">
          {/* Animated 404 Illustration */}
          <div className="relative mb-8">
            <div className="relative">
              {/* Large 404 Text - solid color matching text */}
              <h1 className="select-none text-[150px] font-bold leading-none text-slate-900 dark:text-white md:text-[200px]">
                404
              </h1>

              {/* Floating icon */}
              <div className="absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 animate-bounce">
                <FileQuestion
                  className="h-16 w-16 text-slate-400 opacity-50 dark:text-slate-500 md:h-20 md:w-20"
                  strokeWidth={1.5}
                />
              </div>
            </div>

            {/* Decorative elements */}
            <div className="absolute -left-4 -top-4 h-24 w-24 animate-pulse rounded-full bg-slate-200/50 blur-xl dark:bg-slate-700/50"></div>
            <div className="absolute -bottom-4 -right-4 h-32 w-32 animate-pulse rounded-full bg-slate-300/50 blur-xl delay-700 dark:bg-slate-600/50"></div>
          </div>

          {/* Message */}
          <div className="mb-8 space-y-4">
            <h2 className="text-3xl font-bold text-slate-900 dark:text-white md:text-4xl">
              Page Not Found
            </h2>
            <p className="text-xl leading-relaxed text-slate-600 dark:text-slate-300">
              Oops! The page you&apos;re looking for seems to have wandered off into the digital
              void.
            </p>
          </div>

          {/* Helpful suggestion */}
          <div className="mt-12 border-t border-slate-200 pt-8 dark:border-slate-700">
            <p className="text-sm text-slate-500 dark:text-slate-500">
              Try using the navigation menu above or return to the{" "}
              <Link href="/" className="font-medium text-slate-900 hover:underline dark:text-white">
                homepage
              </Link>{" "}
              to find what you&apos;re looking for.
            </p>
          </div>

          {/* Fun error code */}
          <div className="mt-6">
            <p className="font-mono text-xs text-slate-400 dark:text-slate-600">
              ERROR_PAGE_NOT_FOUND_404
            </p>
          </div>
        </div>
      </section>
    </div>
  );
}
