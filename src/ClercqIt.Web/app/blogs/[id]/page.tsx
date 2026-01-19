import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { BlurImage } from "@/components/ui/blur-image";
import { ArrowLeft, Calendar } from "lucide-react";
import { fetchBlogById } from "../lib/api";
import { notFound } from "next/navigation";

interface BlogDetailPageProps {
  params: Promise<{ id: string }>;
}

export default async function BlogDetailPage({ params }: BlogDetailPageProps) {
  const { id } = await params;
  const blog = await fetchBlogById(id);

  if (!blog) {
    notFound();
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("en-US", {
      month: "long",
      day: "numeric",
      year: "numeric",
    });
  };

  return (
    <div
      className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800"
      style={{ fontFamily: "Arial, sans-serif" }}
    >
      <section className="container mx-auto px-4 py-16">
        <div className="max-w-4xl mx-auto">
          <Button asChild variant="ghost" className="mb-8">
            <Link href="/blogs">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Blogs
            </Link>
          </Button>

          {/* Blog Header */}
          <div className="mb-8">
            <h1 className="text-4xl font-bold text-slate-900 mb-4 dark:text-white">
              {blog.shortDescription}
            </h1>

            <div className="flex items-center gap-4 text-slate-600 dark:text-slate-400">
              <div className="flex items-center gap-2">
                <Calendar className="h-4 w-4" />
                <span>{formatDate(blog.publishDate)}</span>
              </div>
            </div>
          </div>

          {/* Blog Content with Image */}
          <div className="bg-white/80 backdrop-blur-sm dark:bg-slate-800/80 rounded-lg shadow-lg p-8">
            <BlurImage
              src={blog.image || "/placeholder.svg"}
              alt={blog.shortDescription}
              width={250}
              height={250}
              containerClassName="w-48 h-48 float-left mr-6 mb-4 rounded-lg shadow-md flex-shrink-0"
            />

            <div className="text-slate-600 dark:text-slate-400 leading-relaxed">
              <div className="prose dark:prose-invert max-w-none">
                <p className="whitespace-pre-line">{blog.longDescription}</p>
              </div>
            </div>

            <div className="clear-both pt-6">
              <div className="flex flex-wrap gap-2">
                {blog.tags.map((tag) => (
                  <Badge key={tag} variant="secondary">
                    {tag}
                  </Badge>
                ))}
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
