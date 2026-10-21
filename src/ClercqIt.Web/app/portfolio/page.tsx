import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { BlurImage } from "@/components/ui/blur-image";
import { ArrowLeft } from "lucide-react";
import { fetchAllProjects } from "./lib/api";

export default async function PortfolioPage() {
  const projects = await fetchAllProjects();

  return (
    <div
      className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800"
      style={{ fontFamily: "Arial, sans-serif" }}
    >
      {/* Portfolio Header */}
      <section className="container mx-auto px-4 py-16">
        <div className="mx-auto max-w-4xl">
          <Button asChild variant="ghost" className="mb-8">
            <Link href="/">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Home
            </Link>
          </Button>

          <h2 className="mb-6 text-4xl font-bold text-slate-900 dark:text-white">My Portfolio</h2>
          <p className="text-xl leading-relaxed text-slate-600 dark:text-slate-300">
            A collection of projects showcasing my expertise in full-stack development, cloud
            technologies, and modern software architecture. Each project demonstrates different
            aspects of my technical skills and problem-solving approach.
          </p>
        </div>
      </section>

      {/* Projects Grid */}
      <section className="container mx-auto px-4 pb-16">
        <div className="mx-auto max-w-4xl">
          <div className="grid gap-6">
            {projects.length === 0 ? (
              <div className="py-12 text-center text-slate-600 dark:text-slate-400">
                No projects found. Check back soon for new portfolio items!
              </div>
            ) : (
              projects.map((project) => (
                <Link key={project.id} href={`/portfolio/${project.id}`} className="block">
                  <Card className="group cursor-pointer border-0 bg-white/80 shadow-lg backdrop-blur-sm transition-all duration-300 hover:shadow-xl dark:bg-slate-800/80 dark:shadow-slate-900/20 md:flex md:flex-row">
                    <BlurImage
                      src={project.image || "/placeholder.svg"}
                      alt={project.title}
                      width={200}
                      height={200}
                      containerClassName="w-full md:w-48 h-48 flex-shrink-0 rounded-t-lg md:rounded-l-lg md:rounded-tr-none"
                    />

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
              ))
            )}
          </div>
        </div>
      </section>
    </div>
  );
}
