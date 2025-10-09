import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { ArrowLeft, Calendar } from "lucide-react";
import Image from "next/image";
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
            <div className="flex items-center gap-4 text-slate-600 dark:text-slate-400 mb-6">
              <div className="flex items-center gap-2">
                <Calendar className="h-4 w-4" />
                <span>{formatDate(blog.publishDate)}</span>
              </div>
            </div>

            <div className="flex flex-wrap gap-2 mb-6">
              {blog.tags.map((tag) => (
                <Badge key={tag} variant="secondary">
                  {tag}
                </Badge>
              ))}
            </div>
          </div>

          {/* Blog Image */}
          <div className="mb-8 rounded-lg overflow-hidden shadow-lg">
            <Image
              src={blog.image || "/placeholder.svg"}
              alt={blog.shortDescription}
              width={800}
              height={500}
              className="w-full object-cover"
            />
          </div>

          {/* Blog Content */}
          <div className="bg-white/80 backdrop-blur-sm dark:bg-slate-800/80 rounded-lg shadow-lg p-8">
            <div className="text-slate-600 dark:text-slate-400 mb-6 leading-relaxed">
              <p className="mb-4 text-lg font-medium text-slate-700 dark:text-slate-300">
                {blog.shortDescription}
              </p>
              <div className="prose dark:prose-invert max-w-none">
                <p className="whitespace-pre-line">{blog.longDescription}</p>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
