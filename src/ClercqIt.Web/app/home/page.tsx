import Link from "next/link";
import { PageContainer } from "@/components/page-container";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { ArrowRight, Code, Cloud, GitBranch } from "lucide-react";
import Image from "next/image";
import { imageOrNone } from "@/lib/utils";
import { fetchFeaturedProjects } from "../portfolio/lib/api";
import type { Metadata } from "next";
import { JsonLd } from "@/components/json-ld";
import {
  AUTHOR_NAME,
  pageMetadata,
  personJsonLd,
  projectPath,
  SITE_DESCRIPTION,
  SITE_NAME,
  SITE_URL,
} from "@/lib/seo";

const HOME_TITLE = `${AUTHOR_NAME} - Developer & Solution Architect`;

// The home page is served at / and /home; both name / as canonical.
export const metadata: Metadata = {
  ...pageMetadata({ title: HOME_TITLE, description: SITE_DESCRIPTION, path: "/", type: "profile" }),
  title: { absolute: `${HOME_TITLE} | ${SITE_NAME}` },
};

export default async function HomePage() {
  const featuredProjects = await fetchFeaturedProjects();

  const skills = [
    {
      icon: Code,
      label: "Architecture & Development",
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
    <>
      <JsonLd
        data={[
          personJsonLd(),
          {
            "@type": "WebSite",
            "@id": `${SITE_URL}/#website`,
            url: SITE_URL,
            name: SITE_NAME,
            description: SITE_DESCRIPTION,
            publisher: { "@id": personJsonLd()["@id"] },
          },
        ]}
      />
      {/* Hero Section */}
      <PageContainer as="section" className="py-16">
        <div className="mx-auto max-w-3xl text-center">
          <h1 className="mb-6 text-5xl font-bold text-slate-900 dark:text-white">
            Developer & Architect
          </h1>
          <p className="mb-8 text-xl leading-relaxed text-slate-600 dark:text-slate-300">
            Passionate about creating complete digital solutions from A to Z. As a solution
            architect who still writes code, I design and build scalable applications that solve
            real-world problems.
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
      </PageContainer>

      {/* Skills Section */}
      <PageContainer as="section" className="py-16">
        <h2 className="mb-12 text-center text-3xl font-bold text-slate-900 dark:text-white">
          What I Do
        </h2>
        <div className="grid gap-8 md:grid-cols-3">
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
      </PageContainer>

      {/* Featured Projects */}
      <PageContainer as="section" className="py-16">
        <div className="mb-12 text-center">
          <h2 className="mb-4 text-3xl font-bold text-slate-900 dark:text-white">
            Featured Projects
          </h2>
          <p className="mx-auto max-w-2xl text-slate-600 dark:text-slate-300">
            Here are some of the projects I&apos;ve been working on, showcasing my expertise in
            software architecture, development and cloud technologies.
          </p>
        </div>

        <div className="grid gap-8 md:grid-cols-3">
          {featuredProjects.length === 0 ? (
            <div className="text-center text-slate-600 md:col-span-3 dark:text-slate-400">
              No featured projects available.
            </div>
          ) : (
            featuredProjects.slice(0, 3).map((project) => {
              const image = imageOrNone(project.image);
              return (
                <Link key={project.id} href={projectPath(project)} className="block">
                  <Card className="group h-full cursor-pointer border-0 bg-white/80 shadow-lg backdrop-blur-xs transition-all duration-300 hover:shadow-xl dark:bg-slate-800/80 dark:shadow-slate-900/20">
                    {image && (
                      <CardHeader className="p-0">
                        <div className="relative overflow-hidden rounded-t-lg">
                          <Image
                            src={image}
                            alt={project.title}
                            width={300}
                            height={200}
                            className="h-48 w-full object-cover transition-transform duration-300 group-hover:scale-105"
                          />
                        </div>
                      </CardHeader>
                    )}
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
              );
            })
          )}
        </div>

        <div className="mt-12 text-center">
          <Button asChild variant="outline" size="lg">
            <Link href="/portfolio">
              View All Projects <ArrowRight className="ml-2 h-4 w-4" />
            </Link>
          </Button>
        </div>
      </PageContainer>
    </>
  );
}
