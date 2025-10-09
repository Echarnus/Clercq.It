import Link from "next/link";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { ArrowLeft } from "lucide-react";
import Image from "next/image";
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
        <div className="max-w-4xl mx-auto">
          <Button asChild variant="ghost" className="mb-8">
            <Link href="/">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Home
            </Link>
          </Button>

          <h2 className="text-4xl font-bold text-slate-900 mb-6 dark:text-white">
            My Portfolio
          </h2>
          <p className="text-xl text-slate-600 leading-relaxed dark:text-slate-300">
            A collection of projects showcasing my expertise in full-stack
            development, cloud technologies, and modern software architecture.
            Each project demonstrates different aspects of my technical skills
            and problem-solving approach.
          </p>
        </div>
      </section>

      {/* Projects Grid */}
      <section className="container mx-auto px-4 pb-16">
        <div className="max-w-6xl mx-auto">
          <div className="grid gap-8">
            {projects.length === 0 ? (
              <div className="text-center text-slate-600 dark:text-slate-400">
                No projects found. Add markdown files to
                src/ClercqIt.Web/content/projects
              </div>
            ) : (
              projects.map((project) => (
                <Link 
                  key={project.id} 
                  href={`/portfolio/${project.id}`}
                  className="block"
                >
                  <Card
                    className={`group hover:shadow-xl transition-all duration-300 border-0 shadow-lg bg-white/80 backdrop-blur-sm dark:bg-slate-800/80 dark:shadow-slate-900/20 cursor-pointer ${
                      project.featured ? "md:grid md:grid-cols-2 md:gap-8" : ""
                    }`}
                  >
                    <div
                      className={`relative overflow-hidden ${
                        project.featured ? "md:order-1" : ""
                      }`}
                    >
                      <Image
                        src={project.image || "/placeholder.svg"}
                        alt={project.title}
                        width={400}
                        height={250}
                        className={`w-full object-cover group-hover:scale-105 transition-transform duration-300 ${
                          project.featured
                            ? "h-64 md:h-full rounded-t-lg md:rounded-l-lg md:rounded-t-none"
                            : "h-48 rounded-t-lg"
                        }`}
                      />
                    </div>

                    <CardContent
                      className={`p-6 ${
                        project.featured
                          ? "md:order-2 md:flex md:flex-col md:justify-center"
                          : ""
                      }`}
                    >
                      <div className="flex items-start justify-between mb-4">
                        <CardTitle className="text-slate-900 text-xl dark:text-white">
                          {project.title}
                        </CardTitle>
                      </div>

                      <CardDescription className="text-slate-600 mb-4 leading-relaxed dark:text-slate-400">
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
