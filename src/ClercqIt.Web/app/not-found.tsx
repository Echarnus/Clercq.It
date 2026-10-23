import Link from "next/link";
import { PageContainer } from "@/components/page-container";
import { FileQuestion } from "lucide-react";

export default function NotFound() {
  return (
    <>
      <PageContainer as="section" className="py-16">
        <div className="mx-auto max-w-3xl text-center">
          {/* Animated 404 Illustration */}
          <div className="relative mb-8">
            <div className="relative">
              {/* Large 404 Text - solid color matching text */}
              <h1 className="text-[150px] leading-none font-bold text-slate-900 select-none md:text-[200px] dark:text-white">
                404
              </h1>

              {/* Floating icon */}
              <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 animate-bounce">
                <FileQuestion
                  className="h-16 w-16 text-slate-400 opacity-50 md:h-20 md:w-20 dark:text-slate-500"
                  strokeWidth={1.5}
                />
              </div>
            </div>

            {/* Decorative elements */}
            <div className="absolute -top-4 -left-4 h-24 w-24 animate-pulse rounded-full bg-slate-200/50 blur-xl dark:bg-slate-700/50"></div>
            <div className="absolute -right-4 -bottom-4 h-32 w-32 animate-pulse rounded-full bg-slate-300/50 blur-xl delay-700 dark:bg-slate-600/50"></div>
          </div>

          {/* Message */}
          <div className="mb-8 space-y-4">
            <h2 className="text-3xl font-bold text-slate-900 md:text-4xl dark:text-white">
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
      </PageContainer>
    </>
  );
}
