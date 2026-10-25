import type { Metadata } from "next";
import Link from "next/link";
import { PageContainer } from "@/components/page-container";
import { Card, CardContent, CardDescription, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { BlurImage } from "@/components/ui/blur-image";
import { fetchAllProjects } from "./lib/api";
import { imageOrNone } from "@/lib/utils";
import { pageMetadata, projectPath } from "@/lib/seo";

export const metadata: Metadata = pageMetadata({
  title: "Portfolio",
  description:
    "Projects by Kenneth De Clercq in solution architecture, .NET and TypeScript development and cloud technologies.",
  path: "/portfolio",
});

export default async function PortfolioPage() {
  const projects = await fetchAllProjects();

  return (
    <>
      {/* Portfolio Header */}
      <PageContainer as="section" className="py-16">
        <div className="max-w-3xl">
          <h1 className="mb-6 text-4xl font-bold text-slate-900 dark:text-white">My Portfolio</h1>
          <p className="text-xl leading-relaxed text-slate-600 dark:text-slate-300">
            A collection of projects showcasing my expertise in solution architecture, software
            development and cloud technologies. Each project demonstrates different aspects of my
            technical skills and problem-solving approach.
          </p>
        </div>
      </PageContainer>

      {/* Projects Grid */}
      <PageContainer as="section" className="pb-16">
        <div>
          <div className="grid gap-6 lg:grid-cols-2">
            {projects.length === 0 ? (
              <div className="py-12 text-center text-slate-600 dark:text-slate-400">
                No projects found. Check back soon for new portfolio items!
              </div>
            ) : (
              projects.map((project) => {
                const image = imageOrNone(project.image);
                return (
                  <Link key={project.id} href={projectPath(project)} className="block">
                    <Card className="group h-full cursor-pointer border-0 bg-white/80 shadow-lg backdrop-blur-xs transition-all duration-300 hover:shadow-xl md:flex md:flex-row dark:bg-slate-800/80 dark:shadow-slate-900/20">
                      {image && (
                        <BlurImage
                          src={image}
                          alt={project.title}
                          width={200}
                          height={200}
                          containerClassName="w-full md:w-48 h-48 md:h-auto md:min-h-48 shrink-0 rounded-t-lg md:rounded-l-lg md:rounded-tr-none"
                        />
                      )}

                      <CardContent className="flex flex-col justify-center p-6 md:flex-1">
                        <CardTitle className="mb-2 text-xl text-slate-900 dark:text-white">
                          {project.title}
                        </CardTitle>

                        <CardDescription className="mb-4 leading-relaxed text-slate-600 dark:text-slate-400">
                          {project.shortDescription}
                        </CardDescription>

                        <div className="flex flex-wrap gap-2">
                          {project.skills?.map((skill) => (
                            <Badge key={skill} variant="secondary" className="text-xs">
                              {skill}
                            </Badge>
                          ))}
                        </div>
                      </CardContent>
                    </Card>
                  </Link>
                );
              })
            )}
          </div>
        </div>
      </PageContainer>
    </>
  );
}
