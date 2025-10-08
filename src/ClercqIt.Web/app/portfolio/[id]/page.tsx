import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { ArrowLeft, Calendar } from "lucide-react";
import Image from "next/image";
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
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'long',
      year: 'numeric',
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
            <Link href="/portfolio">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Portfolio
            </Link>
          </Button>

          {/* Project Header */}
          <div className="mb-8">
            <h1 className="text-4xl font-bold text-slate-900 mb-4 dark:text-white">
              {project.title}
            </h1>
            
            <div className="flex items-center gap-4 text-slate-600 dark:text-slate-400 mb-6">
              <div className="flex items-center gap-2">
                <Calendar className="h-4 w-4" />
                <span>
                  {formatDate(project.startDate)} - {formatDate(project.endDate)}
                </span>
              </div>
              {project.featured && (
                <Badge variant="default">Featured</Badge>
              )}
            </div>

            <div className="flex flex-wrap gap-2 mb-6">
              {project.skills?.map((skill) => (
                <Badge key={skill} variant="secondary">
                  {skill}
                </Badge>
              ))}
            </div>
          </div>

          {/* Project Image */}
          <div className="mb-8 rounded-lg overflow-hidden shadow-lg">
            <Image
              src={project.image || "/placeholder.svg"}
              alt={project.title}
              width={800}
              height={500}
              className="w-full object-cover"
            />
          </div>

          {/* Project Description */}
          <div className="bg-white/80 backdrop-blur-sm dark:bg-slate-800/80 rounded-lg shadow-lg p-8">
            <h2 className="text-2xl font-bold text-slate-900 mb-4 dark:text-white">
              About This Project
            </h2>
            
            <div className="text-slate-600 dark:text-slate-400 mb-6 leading-relaxed">
              <p className="mb-4 text-lg">{project.shortDescription}</p>
              <div className="prose dark:prose-invert max-w-none">
                <p className="whitespace-pre-line">{project.longDescription}</p>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
