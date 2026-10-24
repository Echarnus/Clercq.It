import type { Metadata } from "next";
import { cache } from "react";
import Link from "next/link";
import { PageContainer } from "@/components/page-container";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { BlurImage } from "@/components/ui/blur-image";
import { ArrowLeft, Calendar } from "lucide-react";
import { fetchProjectById } from "../lib/api";
import { Markdown } from "@/components/markdown";
import { notFound } from "next/navigation";

interface PortfolioDetailPageProps {
  params: Promise<{ id: string }>;
}

// The page and its metadata need the same item; cache() fetches it once per request.
const getProject = cache(fetchProjectById);

export async function generateMetadata({ params }: PortfolioDetailPageProps): Promise<Metadata> {
  const { id } = await params;
  const project = await getProject(id);
  return project ? { title: project.title, description: project.shortDescription } : {};
}

export default async function PortfolioDetailPage({ params }: PortfolioDetailPageProps) {
  const { id } = await params;
  const project = await getProject(id);

  if (!project) {
    notFound();
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("en-US", {
      month: "long",
      year: "numeric",
    });
  };

  return (
    <PageContainer as="section" className="py-16">
      <Button asChild variant="ghost" className="mb-8">
        <Link href="/portfolio">
          <ArrowLeft className="mr-2 h-4 w-4" />
          Back to Portfolio
        </Link>
      </Button>

      <div className="grid grid-cols-1 gap-8 lg:grid-cols-4">
        {/* Project Header */}
        <h1 className="text-4xl font-bold text-slate-900 lg:col-span-3 dark:text-white">
          {project.title}
        </h1>

        {/* Project details */}
        <aside className="lg:col-start-4 lg:row-start-2">
          <div className="space-y-4 rounded-lg bg-white/80 p-6 shadow-lg backdrop-blur-xs lg:sticky lg:top-28 dark:bg-slate-800/80">
            <div className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-400">
              <Calendar className="h-4 w-4 shrink-0" />
              <span>
                {formatDate(project.startDate)} - {formatDate(project.endDate)}
              </span>
            </div>
            {project.featured && <Badge variant="default">Featured</Badge>}
            <div className="flex flex-wrap gap-2">
              {project.skills?.map((skill) => (
                <Badge key={skill} variant="secondary">
                  {skill}
                </Badge>
              ))}
            </div>
          </div>
        </aside>

        <article className="lg:col-span-3 lg:row-start-2">
          {/* Project Content with Image */}
          <div className="rounded-lg bg-white/80 p-8 shadow-lg backdrop-blur-xs dark:bg-slate-800/80">
            <BlurImage
              src={project.image || "/placeholder.svg"}
              alt={project.title}
              width={250}
              height={250}
              containerClassName="w-48 h-48 float-left mr-6 mb-4 rounded-lg shadow-md shrink-0"
            />

            <div className="leading-relaxed text-slate-600 dark:text-slate-400">
              {/* Content files default the body to the summary; do not show it twice. */}
              {project.longDescription.trim() !== project.shortDescription.trim() && (
                <p className="mb-4 text-lg">{project.shortDescription}</p>
              )}
              <Markdown>{project.longDescription}</Markdown>
            </div>
            <div className="clear-both" />
          </div>
        </article>
      </div>
    </PageContainer>
  );
}
