import type { MetadataRoute } from "next";
import { fetchAllProjects } from "./portfolio/lib/api";
import { fetchAllBlogs } from "./blogs/lib/api";
import { absoluteUrl, blogPath, projectPath } from "@/lib/seo";

// Content changes through the admin UI and the MCP server, so build the list per request.
export const dynamic = "force-dynamic";

// /sitemap.xml (docs/seo.md): the public pages plus every project and blog post.
export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const [projects, blogs] = await Promise.all([fetchAllProjects(), fetchAllBlogs()]);

  const pages: MetadataRoute.Sitemap = [
    { url: absoluteUrl("/"), changeFrequency: "monthly", priority: 1 },
    { url: absoluteUrl("/portfolio"), changeFrequency: "monthly", priority: 0.8 },
    { url: absoluteUrl("/blogs"), changeFrequency: "weekly", priority: 0.8 },
    { url: absoluteUrl("/certifications"), changeFrequency: "monthly", priority: 0.6 },
  ];

  return [
    ...pages,
    ...projects.map((project) => ({
      url: absoluteUrl(projectPath(project)),
      changeFrequency: "yearly" as const,
      priority: 0.7,
    })),
    ...blogs.map((blog) => ({
      url: absoluteUrl(blogPath(blog)),
      lastModified: new Date(blog.publishDate),
      changeFrequency: "yearly" as const,
      priority: 0.7,
    })),
  ];
}
