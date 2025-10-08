import Link from "next/link";
import { Button } from "@/components/ui/button";
import { ArrowLeft } from "lucide-react";
import { fetchAllBlogs } from "./lib/api";
import { BlogsContent } from "./blogs-content";

export default async function BlogsPage() {
  const blogs = await fetchAllBlogs();

  return (
    <div
      className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800"
      style={{ fontFamily: "Arial, sans-serif" }}
    >
      {/* Blogs Header */}
      <section className="container mx-auto px-4 py-16">
        <div className="max-w-6xl mx-auto">
          <Button asChild variant="ghost" className="mb-8">
            <Link href="/">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Home
            </Link>
          </Button>

          <h2 className="text-4xl font-bold text-slate-900 mb-6 dark:text-white">
            Blog Posts
          </h2>
          <p className="text-xl text-slate-600 leading-relaxed dark:text-slate-300">
            Insights, tutorials, and thoughts on software development, cloud
            technologies, and modern web architecture.
          </p>
        </div>
      </section>

      {/* Blogs Content */}
      <section className="container mx-auto px-4 pb-16">
        <div className="max-w-6xl mx-auto">
          {blogs.length === 0 ? (
            <div className="text-center text-slate-600 dark:text-slate-400">
              No blog posts available yet.
            </div>
          ) : (
            <BlogsContent blogs={blogs} />
          )}
        </div>
      </section>
    </div>
  );
}
