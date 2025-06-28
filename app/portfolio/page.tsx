import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { ArrowLeft, ExternalLink, Github, Mail } from "lucide-react"
import { Header } from "@/components/header"
import Image from "next/image"

export default function PortfolioPage() {
  const projects = [
    {
      title: "E-Commerce Platform",
      description:
        "A comprehensive e-commerce solution built with React frontend and .NET Core backend. Features include user authentication, payment processing, inventory management, and admin dashboard. Deployed using Docker containers on Azure with CI/CD pipeline.",
      image: "/placeholder.svg?height=250&width=400",
      tags: ["React", ".NET Core", "Azure", "Docker", "SQL Server"],
      liveUrl: "#",
      githubUrl: "#",
      featured: true,
    },
    {
      title: "Task Management App",
      description:
        "Angular-based project management tool with real-time collaboration features. Built with ASP.NET backend, SignalR for real-time updates, and deployed on Kubernetes cluster with automated scaling.",
      image: "/placeholder.svg?height=250&width=400",
      tags: ["Angular", "ASP.NET", "SignalR", "K8s", "PostgreSQL"],
      liveUrl: "#",
      githubUrl: "#",
      featured: true,
    },
    {
      title: "Cloud Analytics Dashboard",
      description:
        "TypeScript-based dashboard for monitoring cloud infrastructure metrics. Integrates with Azure Monitor and provides real-time insights into application performance and resource utilization.",
      image: "/placeholder.svg?height=250&width=400",
      tags: ["TypeScript", "Azure", "Docker", "DevOps", "MongoDB"],
      liveUrl: "#",
      githubUrl: "#",
      featured: true,
    },
    {
      title: "Microservices API Gateway",
      description:
        "Scalable API gateway built with .NET Core, handling authentication, rate limiting, and request routing for microservices architecture. Deployed using Docker Swarm with load balancing.",
      image: "/placeholder.svg?height=250&width=400",
      tags: [".NET Core", "Docker", "Redis", "Nginx", "Microservices"],
      liveUrl: "#",
      githubUrl: "#",
      featured: false,
    },
    {
      title: "React Native Mobile App",
      description:
        "Cross-platform mobile application for inventory management. Features offline synchronization, barcode scanning, and real-time updates. Backend powered by ASP.NET Core Web API.",
      image: "/placeholder.svg?height=250&width=400",
      tags: ["React Native", "ASP.NET Core", "SQLite", "Azure", "Mobile"],
      liveUrl: "#",
      githubUrl: "#",
      featured: false,
    },
    {
      title: "DevOps Automation Suite",
      description:
        "Custom DevOps tools and scripts for automating deployment pipelines. Includes infrastructure as code templates, monitoring setup, and automated testing frameworks using Azure DevOps and GitHub Actions.",
      image: "/placeholder.svg?height=250&width=400",
      tags: ["Azure DevOps", "GitHub Actions", "Terraform", "PowerShell", "YAML"],
      liveUrl: "#",
      githubUrl: "#",
      featured: false,
    },
  ]

  return (
    <div
      className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800"
      style={{ fontFamily: "Arial, sans-serif" }}
    >
      {/* Header */}
      <Header currentPage="portfolio" />

      {/* Portfolio Header */}
      <section className="container mx-auto px-4 py-16">
        <div className="max-w-4xl mx-auto">
          <Button asChild variant="ghost" className="mb-8">
            <Link href="/">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Home
            </Link>
          </Button>

          <h2 className="text-4xl font-bold text-slate-900 mb-6 dark:text-white">My Portfolio</h2>
          <p className="text-xl text-slate-600 leading-relaxed dark:text-slate-300">
            A collection of projects showcasing my expertise in full-stack development, cloud technologies, and modern
            software architecture. Each project demonstrates different aspects of my technical skills and
            problem-solving approach.
          </p>
        </div>
      </section>

      {/* Projects Grid */}
      <section className="container mx-auto px-4 pb-16">
        <div className="max-w-6xl mx-auto">
          <div className="grid gap-8">
            {projects.map((project, index) => (
              <Card
                key={index}
                className={`group hover:shadow-xl transition-all duration-300 border-0 shadow-lg bg-white/80 backdrop-blur-sm dark:bg-slate-800/80 dark:shadow-slate-900/20 ${project.featured ? "md:grid md:grid-cols-2 md:gap-8" : ""}`}
              >
                <div className={`relative overflow-hidden ${project.featured ? "md:order-1" : ""}`}>
                  <Image
                    src={project.image || "/placeholder.svg"}
                    alt={project.title}
                    width={400}
                    height={250}
                    className={`w-full object-cover group-hover:scale-105 transition-transform duration-300 ${project.featured ? "h-64 md:h-full rounded-t-lg md:rounded-l-lg md:rounded-t-none" : "h-48 rounded-t-lg"}`}
                  />
                </div>

                <CardContent
                  className={`p-6 ${project.featured ? "md:order-2 md:flex md:flex-col md:justify-center" : ""}`}
                >
                  <div className="flex items-start justify-between mb-4">
                    <CardTitle className="text-slate-900 text-xl dark:text-white">{project.title}</CardTitle>
                    <div className="flex gap-2 ml-4">
                      <Button asChild size="sm" variant="outline">
                        <Link href={project.liveUrl}>
                          <ExternalLink className="h-4 w-4" />
                        </Link>
                      </Button>
                      <Button asChild size="sm" variant="outline">
                        <Link href={project.githubUrl}>
                          <Github className="h-4 w-4" />
                        </Link>
                      </Button>
                    </div>
                  </div>

                  <CardDescription className="text-slate-600 mb-4 leading-relaxed dark:text-slate-400">
                    {project.description}
                  </CardDescription>

                  <div className="flex flex-wrap gap-2">
                    {project.tags.map((tag) => (
                      <Badge key={tag} variant="secondary" className="text-xs">
                        {tag}
                      </Badge>
                    ))}
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="bg-slate-900 text-white py-12">
        <div className="container mx-auto px-4">
          <div className="grid md:grid-cols-3 gap-8 max-w-4xl mx-auto">
            <div>
              <h4 className="font-semibold mb-4">Portfolio</h4>
              <ul className="space-y-2 text-slate-300">
                <li>
                  <Link href="/portfolio" className="hover:text-white transition-colors">
                    All Projects
                  </Link>
                </li>
                <li>
                  <Link href="/portfolio" className="hover:text-white transition-colors">
                    Web Applications
                  </Link>
                </li>
                <li>
                  <Link href="/portfolio" className="hover:text-white transition-colors">
                    Cloud Solutions
                  </Link>
                </li>
              </ul>
            </div>
            <div>
              <h4 className="font-semibold mb-4">Sitemap</h4>
              <ul className="space-y-2 text-slate-300">
                <li>
                  <Link href="/" className="hover:text-white transition-colors">
                    Home
                  </Link>
                </li>
                <li>
                  <Link href="/portfolio" className="hover:text-white transition-colors">
                    Portfolio
                  </Link>
                </li>
                <li>
                  <Link href="#" className="hover:text-white transition-colors">
                    About
                  </Link>
                </li>
                <li>
                  <Link href="#" className="hover:text-white transition-colors">
                    Contact
                  </Link>
                </li>
              </ul>
            </div>
            <div>
              <h4 className="font-semibold mb-4">Connect</h4>
              <ul className="space-y-3 text-slate-300">
                <li>
                  <Link
                    href="https://www.linkedin.com/in/your-linkedin-profile"
                    className="hover:text-white transition-colors flex items-center gap-2"
                  >
                    <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
                      <path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433c-1.144 0-2.063-.926-2.063-2.065 0-1.138.92-2.063 2.063-2.063 1.14 0 2.064.925 2.064 2.063 0 1.139-.925 2.065-2.064 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z" />
                    </svg>
                    LinkedIn
                  </Link>
                </li>
                <li>
                  <Link
                    href="https://github.com/your-github-profile"
                    className="hover:text-white transition-colors flex items-center gap-2"
                  >
                    <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
                      <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z" />
                    </svg>
                    GitHub
                  </Link>
                </li>
                <li>
                  <Link
                    href="mailto:your-email@example.com"
                    className="hover:text-white transition-colors flex items-center gap-2"
                  >
                    <Mail className="w-4 h-4" />
                    Email
                  </Link>
                </li>
              </ul>
            </div>
          </div>
          <div className="text-center mt-8 pt-8 border-t border-slate-800 text-slate-400">
            <p>Made with ❤️ using React, .NET, Docker • Hosted on Scaleway</p>
          </div>
        </div>
      </footer>
    </div>
  )
}
