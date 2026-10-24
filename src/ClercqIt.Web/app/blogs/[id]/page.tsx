import type { Metadata } from "next";
import { cache } from "react";
import Link from "next/link";
import { PageContainer } from "@/components/page-container";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { BlurImage } from "@/components/ui/blur-image";
import { ArrowLeft, Calendar } from "lucide-react";
import { fetchBlogById } from "../lib/api";
import { Markdown } from "@/components/markdown";
import { notFound } from "next/navigation";
import { imageOrNone } from "@/lib/utils";

interface BlogDetailPageProps {
  params: Promise<{ id: string }>;
}

// The page and its metadata need the same item; cache() fetches it once per request.
const getBlog = cache(fetchBlogById);

export async function generateMetadata({ params }: BlogDetailPageProps): Promise<Metadata> {
  const { id } = await params;
  const blog = await getBlog(id);
  return blog ? { title: blog.title, description: blog.shortDescription } : {};
}

export default async function BlogDetailPage({ params }: BlogDetailPageProps) {
  const { id } = await params;
  const blog = await getBlog(id);

  if (!blog) {
    notFound();
  }

  const image = imageOrNone(blog.image);

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("en-US", {
      month: "long",
      day: "numeric",
      year: "numeric",
    });
  };

  return (
    <PageContainer as="section" className="py-16">
      <Button asChild variant="ghost" className="mb-8">
        <Link href="/blogs">
          <ArrowLeft className="mr-2 h-4 w-4" />
          Back to Blogs
        </Link>
      </Button>

      <div className="grid grid-cols-1 gap-8 lg:grid-cols-4">
        {/* Blog Header */}
        <h1 className="text-4xl font-bold text-slate-900 lg:col-span-3 dark:text-white">
          {blog.title}
        </h1>

        {/* Blog details */}
        <aside className="lg:col-start-4 lg:row-span-2 lg:row-start-1">
          <div className="space-y-4 rounded-lg bg-white/80 p-6 shadow-lg backdrop-blur-xs lg:sticky lg:top-28 dark:bg-slate-800/80">
            <div className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-400">
              <Calendar className="h-4 w-4 shrink-0" />
              <span>{formatDate(blog.publishDate)}</span>
            </div>
            <div className="flex flex-wrap gap-2">
              {blog.tags.map((tag) => (
                <Badge key={tag} variant="secondary">
                  {tag}
                </Badge>
              ))}
            </div>
          </div>
        </aside>

        <article className="lg:col-span-3 lg:row-start-2">
          {/* Blog Content with Image */}
          <div className="rounded-lg bg-white/80 p-8 shadow-lg backdrop-blur-xs dark:bg-slate-800/80">
            {image && (
              <BlurImage
                src={image}
                alt={blog.title}
                width={250}
                height={250}
                containerClassName="w-48 h-48 float-left mr-6 mb-4 rounded-lg shadow-md shrink-0"
              />
            )}

            <div className="leading-relaxed text-slate-600 dark:text-slate-400">
              {/* Content files default the body to the summary; do not show it twice. */}
              {blog.longDescription.trim() !== blog.shortDescription.trim() && (
                <p className="mb-4 text-lg">{blog.shortDescription}</p>
              )}
              <Markdown>{blog.longDescription}</Markdown>
            </div>
            <div className="clear-both" />
          </div>
        </article>
      </div>
    </PageContainer>
  );
}
