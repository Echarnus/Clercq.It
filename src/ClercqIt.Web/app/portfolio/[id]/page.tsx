import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { BlurImage } from "@/components/ui/blur-image";
import { ArrowLeft, Calendar } from "lucide-react";
import { fetchProjectById } from "../lib/api";
import { notFound } from "next/navigation";

interface PortfolioDetailPageProps {
  params: Promise<{ id: string }>;
}

export default async function PortfolioDetailPage({ params }: PortfolioDetailPageProps) {
  const { id } = await params;
  const project = await fetchProjectById(id);

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
    <div
      className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800"
      style={{ fontFamily: "Arial, sans-serif" }}
    >
      <section className="container mx-auto px-4 py-16">
        <div className="mx-auto max-w-4xl">
          <Button asChild variant="ghost" className="mb-8">
            <Link href="/portfolio">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Portfolio
            </Link>
          </Button>

          {/* Project Header */}
          <div className="mb-8">
            <h1 className="mb-4 text-4xl font-bold text-slate-900 dark:text-white">
              {project.title}
            </h1>

            <div className="mb-6 flex items-center gap-4 text-slate-600 dark:text-slate-400">
              <div className="flex items-center gap-2">
                <Calendar className="h-4 w-4" />
                <span>
                  {formatDate(project.startDate)} - {formatDate(project.endDate)}
                </span>
              </div>
              {project.featured && <Badge variant="default">Featured</Badge>}
            </div>
          </div>

          {/* Project Content with Image */}
          <div className="rounded-lg bg-white/80 p-8 shadow-lg backdrop-blur-sm dark:bg-slate-800/80">
            <BlurImage
              src={project.image || "/placeholder.svg"}
              alt={project.title}
              width={250}
              height={250}
              containerClassName="w-48 h-48 float-left mr-6 mb-4 rounded-lg shadow-md flex-shrink-0"
            />

            <div className="leading-relaxed text-slate-600 dark:text-slate-400">
              <p className="mb-4 text-lg">{project.shortDescription}</p>
              <div className="prose dark:prose-invert max-w-none">
                <p className="whitespace-pre-line">{project.longDescription}</p>
              </div>
            </div>

            <div className="clear-both pt-6">
              <div className="flex flex-wrap gap-2">
                {project.skills?.map((skill) => (
                  <Badge key={skill} variant="secondary">
                    {skill}
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
