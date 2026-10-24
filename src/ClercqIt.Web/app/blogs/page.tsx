import type { Metadata } from "next";
import { pageMetadata } from "@/lib/seo";
import { PageContainer } from "@/components/page-container";
import { fetchAllBlogs } from "./lib/api";
import { BlogsContent } from "./blogs-content";

export const metadata: Metadata = pageMetadata({
  title: "Blog",
  description:
    "Insights, tutorials and thoughts by Kenneth De Clercq on software development, cloud technologies and web architecture.",
  path: "/blogs",
});

export default async function BlogsPage() {
  const blogs = await fetchAllBlogs();

  return (
    <>
      {/* Blogs Header */}
      <PageContainer as="section" className="py-16">
        <div className="max-w-3xl">
          <h1 className="mb-6 text-4xl font-bold text-slate-900 dark:text-white">Blog Posts</h1>
          <p className="text-xl leading-relaxed text-slate-600 dark:text-slate-300">
            Insights, tutorials, and thoughts on software development, cloud technologies, and
            modern web architecture.
          </p>
        </div>
      </PageContainer>

      {/* Blogs Content */}
      <PageContainer as="section" className="pb-16">
        <div>
          {blogs.length === 0 ? (
            <div className="text-center text-slate-600 dark:text-slate-400">
              No blog posts available yet.
            </div>
          ) : (
            <BlogsContent blogs={blogs} />
          )}
        </div>
      </PageContainer>
    </>
  );
}
