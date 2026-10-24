import type { Metadata } from "next";
import { cache } from "react";
import Link from "next/link";
import { PageContainer } from "@/components/page-container";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { BlurImage } from "@/components/ui/blur-image";
import { ArrowLeft, Calendar } from "lucide-react";
import { fetchBlogBySegment } from "../lib/api";
import { Markdown } from "@/components/markdown";
import { JsonLd } from "@/components/json-ld";
import { notFound, permanentRedirect } from "next/navigation";
import {
  absoluteUrl,
  AUTHOR_NAME,
  blogPath,
  breadcrumbJsonLd,
  metaDescription,
  pageMetadata,
  personJsonLd,
  SITE_NAME,
} from "@/lib/seo";

interface BlogDetailPageProps {
  params: Promise<{ slug: string }>;
}

// The page and its metadata need the same item; cache() fetches it once per request.
const getBlog = cache(fetchBlogBySegment);

export async function generateMetadata({ params }: BlogDetailPageProps): Promise<Metadata> {
  const { slug } = await params;
  const blog = await getBlog(slug);
  if (!blog) return {};

  const description = metaDescription(blog.shortDescription || blog.longDescription);
  const path = blogPath(blog);
  return {
    ...pageMetadata({
      title: blog.title,
      description,
      path,
      type: "article",
      image: blog.image || undefined,
      openGraph: { publishedTime: blog.publishDate, authors: [AUTHOR_NAME], tags: blog.tags },
    }),
    keywords: blog.tags,
    authors: [{ name: AUTHOR_NAME }],
  };
}

export default async function BlogDetailPage({ params }: BlogDetailPageProps) {
  const { slug } = await params;
  const blog = await getBlog(slug);

  if (!blog) {
    notFound();
  }

  // Old GUID links and stale slugs move permanently to the canonical URL.
  const path = blogPath(blog);
  if (`/blogs/${slug}` !== path) {
    permanentRedirect(path);
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("en-US", {
      month: "long",
      day: "numeric",
      year: "numeric",
    });
  };

  return (
    <PageContainer as="section" className="py-16">
      <JsonLd
        data={[
          {
            "@type": "BlogPosting",
            "@id": absoluteUrl(path),
            url: absoluteUrl(path),
            mainEntityOfPage: absoluteUrl(path),
            headline: blog.title,
            description: metaDescription(blog.shortDescription || blog.longDescription),
            datePublished: blog.publishDate,
            dateModified: blog.publishDate,
            ...(blog.image ? { image: absoluteUrl(blog.image) } : {}),
            keywords: blog.tags.join(", "),
            author: personJsonLd(),
            publisher: personJsonLd(),
            isPartOf: { "@type": "Blog", name: `${SITE_NAME} blog`, url: absoluteUrl("/blogs") },
          },
          breadcrumbJsonLd([
            { name: "Home", path: "/" },
            { name: "Blog", path: "/blogs" },
            { name: blog.title, path },
          ]),
        ]}
      />
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
            <BlurImage
              src={blog.image || "/placeholder.svg"}
              alt={blog.title}
              width={250}
              height={250}
              containerClassName="w-48 h-48 float-left mr-6 mb-4 rounded-lg shadow-md shrink-0"
            />

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
