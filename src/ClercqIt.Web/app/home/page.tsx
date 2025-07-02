import Link from "next/link";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { ArrowRight, Code, Cloud, GitBranch } from "lucide-react";
import Image from "next/image";

export default function HomePage() {
  const featuredProjects = [
    {
      title: "E-Commerce Platform",
      description:
        "Full-stack e-commerce solution built with React, .NET Core, and Azure",
      image: "/placeholder.svg?height=200&width=300",
      tags: ["React", ".NET Core", "Azure", "Docker"],
    },
  ];

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
      className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800"
      style={{ fontFamily: "Arial, sans-serif" }}
    >
      {/* Hero Section */}
      <section className="container mx-auto px-4 py-16">
        <div className="max-w-4xl mx-auto text-center">
          <h2 className="text-5xl font-bold text-slate-900 mb-6 dark:text-white">
            Full Stack Developer
          </h2>
          <p className="text-xl text-slate-600 mb-8 leading-relaxed dark:text-slate-300">
            Passionate about creating complete digital solutions from A to Z.
            With expertise in modern technologies, I build scalable applications
            that solve real-world problems.
          </p>
          <div className="flex flex-wrap justify-center gap-2 mb-8">
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
        <h3 className="text-3xl font-bold text-center text-slate-900 mb-12 dark:text-white">
          What I Do
        </h3>
        <div className="grid md:grid-cols-3 gap-8 max-w-4xl mx-auto">
          {skills.map((skill, index) => (
            <Card
              key={index}
              className="text-center border-0 shadow-lg bg-white/80 backdrop-blur-sm dark:bg-slate-800/80 dark:shadow-slate-900/20"
            >
              <CardHeader>
                <skill.icon className="h-12 w-12 mx-auto text-slate-700 mb-4 dark:text-slate-300" />
                <CardTitle className="text-slate-900 dark:text-white">
                  {skill.label}
                </CardTitle>
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
        <div className="text-center mb-12">
          <h3 className="text-3xl font-bold text-slate-900 mb-4 dark:text-white">
            Featured Projects
          </h3>
          <p className="text-slate-600 max-w-2xl mx-auto dark:text-slate-300">
            Here are some of the projects I've been working on, showcasing my
            expertise in full-stack development and cloud technologies.
          </p>
        </div>

        <div className="grid md:grid-cols-3 gap-8 max-w-6xl mx-auto">
          {featuredProjects.map((project, index) => (
            <Card
              key={index}
              className="group hover:shadow-xl transition-all duration-300 border-0 shadow-lg bg-white/80 backdrop-blur-sm dark:bg-slate-800/80 dark:shadow-slate-900/20"
            >
              <CardHeader className="p-0">
                <div className="relative overflow-hidden rounded-t-lg">
                  <Image
                    src={project.image || "/placeholder.svg"}
                    alt={project.title}
                    width={300}
                    height={200}
                    className="w-full h-48 object-cover group-hover:scale-105 transition-transform duration-300"
                  />
                </div>
              </CardHeader>
              <CardContent className="p-6">
                <CardTitle className="text-slate-900 mb-2 dark:text-white">
                  {project.title}
                </CardTitle>
                <CardDescription className="text-slate-600 mb-4 dark:text-slate-400">
                  {project.description}
                </CardDescription>
                <div className="flex flex-wrap gap-2">
                  {project.tags.map((tag) => (
                    <Badge key={tag} variant="outline" className="text-xs">
                      {tag}
                    </Badge>
                  ))}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        <div className="text-center mt-12">
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
