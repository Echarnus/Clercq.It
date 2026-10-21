import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { ArrowRight, Code, Cloud, GitBranch } from "lucide-react";
import Image from "next/image";
import { fetchFeaturedProjects } from "../portfolio/lib/api";

export default async function HomePage() {
  const featuredProjects = await fetchFeaturedProjects();

  const skills = [
    {
      icon: Code,
      label: "Full Stack Development",
      description: ".NET, TypeScript, React, Angular",
    },
    {
      icon: Cloud,
      label: "Cloud & DevOps",
      description: "Azure, Scaleway, Docker, Kubernetes",
    },
    {
      icon: GitBranch,
      label: "CI/CD",
      description: "Azure DevOps, GitHub Actions",
    },
  ];

  return (
    <div
      className="min-h-screen bg-linear-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800"
      style={{ fontFamily: "Arial, sans-serif" }}
    >
      {/* Hero Section */}
      <section className="container mx-auto px-4 py-16">
        <div className="mx-auto max-w-4xl text-center">
          <h2 className="mb-6 text-5xl font-bold text-slate-900 dark:text-white">
            Full Stack Developer
          </h2>
          <p className="mb-8 text-xl leading-relaxed text-slate-600 dark:text-slate-300">
            Passionate about creating complete digital solutions from A to Z. With expertise in
            modern technologies, I build scalable applications that solve real-world problems.
          </p>
          <div className="mb-8 flex flex-wrap justify-center gap-2">
            {[
              ".NET",
              "TypeScript",
              "React",
              "Angular",
              "ASP.NET",
              "Azure",
              "Docker",
              "Kubernetes",
            ].map((tech) => (
              <Badge key={tech} variant="secondary" className="px-3 py-1">
                {tech}
              </Badge>
            ))}
          </div>
          <Button
            asChild
            size="lg"
            className="bg-slate-900 hover:bg-slate-800 dark:bg-white dark:text-slate-900 dark:hover:bg-slate-200"
          >
            <Link href="/portfolio">
              View My Work <ArrowRight className="ml-2 h-4 w-4" />
            </Link>
          </Button>
        </div>
      </section>

      {/* Skills Section */}
      <section className="container mx-auto px-4 py-16">
        <h3 className="mb-12 text-center text-3xl font-bold text-slate-900 dark:text-white">
          What I Do
        </h3>
        <div className="mx-auto grid max-w-4xl gap-8 md:grid-cols-3">
          {skills.map((skill, index) => (
            <Card
              key={index}
              className="border-0 bg-white/80 text-center shadow-lg backdrop-blur-xs dark:bg-slate-800/80 dark:shadow-slate-900/20"
            >
              <CardHeader>
                <skill.icon className="mx-auto mb-4 h-12 w-12 text-slate-700 dark:text-slate-300" />
                <CardTitle className="text-slate-900 dark:text-white">{skill.label}</CardTitle>
              </CardHeader>
              <CardContent>
                <CardDescription className="text-slate-600 dark:text-slate-400">
                  {skill.description}
                </CardDescription>
              </CardContent>
            </Card>
          ))}
        </div>
      </section>

      {/* Featured Projects */}
      <section className="container mx-auto px-4 py-16">
        <div className="mb-12 text-center">
          <h3 className="mb-4 text-3xl font-bold text-slate-900 dark:text-white">
            Featured Projects
          </h3>
          <p className="mx-auto max-w-2xl text-slate-600 dark:text-slate-300">
            Here are some of the projects I&apos;ve been working on, showcasing my expertise in
            full-stack development and cloud technologies.
          </p>
        </div>

        <div className="mx-auto grid max-w-6xl gap-8 md:grid-cols-3">
          {featuredProjects.length === 0 ? (
            <div className="col-span-3 text-center text-slate-600 dark:text-slate-400">
              No featured projects available.
            </div>
          ) : (
            featuredProjects.slice(0, 3).map((project) => (
              <Link key={project.id} href={`/portfolio/${project.id}`} className="block">
                <Card className="group cursor-pointer border-0 bg-white/80 shadow-lg backdrop-blur-xs transition-all duration-300 hover:shadow-xl dark:bg-slate-800/80 dark:shadow-slate-900/20">
                  <CardHeader className="p-0">
                    <div className="relative overflow-hidden rounded-t-lg">
                      <Image
                        src={project.image || "/placeholder.svg"}
                        alt={project.title}
                        width={300}
                        height={200}
                        className="h-48 w-full object-cover transition-transform duration-300 group-hover:scale-105"
                      />
                    </div>
                  </CardHeader>
                  <CardContent className="p-6">
                    <CardTitle className="mb-2 text-slate-900 dark:text-white">
                      {project.title}
                    </CardTitle>
                    <CardDescription className="mb-4 text-slate-600 dark:text-slate-400">
                      {project.shortDescription}
                    </CardDescription>
                    <div className="flex flex-wrap gap-2">
                      {project.skills.map((skill) => (
                        <Badge key={skill} variant="outline" className="text-xs">
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

        <div className="mt-12 text-center">
          <Button asChild variant="outline" size="lg">
            <Link href="/portfolio">
              View All Projects <ArrowRight className="ml-2 h-4 w-4" />
            </Link>
          </Button>
        </div>
      </section>

      {/* Footer */}
    </div>
  );
}
