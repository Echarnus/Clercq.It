import { z } from "zod";

// Base API client configuration
// In the browser in production, use relative URLs (nginx proxies /api to the API).
// On the server, call the API directly. API_INTERNAL_URL is read at runtime (src/start.sh
// sets it in the container); NEXT_PUBLIC_API_URL is inlined at build time, so it is only a
// local development fallback. Port 5000 is the API inside the container, 5001 under Aspire.
const getBaseApiUrl = () => {
  if (typeof window !== "undefined") {
    return process.env.NODE_ENV === "production"
      ? ""
      : process.env.NEXT_PUBLIC_API_URL || "http://localhost:5001";
  }
  return (
    process.env.API_INTERNAL_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    (process.env.NODE_ENV === "production" ? "http://localhost:5000" : "http://localhost:5001")
  );
};

export const getApiUrl = () => getBaseApiUrl();
export const getProjectsBaseUrl = () => `${getBaseApiUrl()}/api/projects`;
export const getBlogsBaseUrl = () => `${getBaseApiUrl()}/api/blogs`;

// Zod schema for Project validation
export const ProjectSchema = z.object({
  id: z.guid(),
  startDate: z.iso.datetime(),
  endDate: z.iso.datetime(),
  shortDescription: z.string(),
  longDescription: z.string(),
  image: z.string(), // Can be empty string or URL
  featured: z.boolean(),
  title: z.string().min(1),
  skills: z.array(z.string()),
});

// Infer TypeScript type from Zod schema
export type Project = z.infer<typeof ProjectSchema>;

// Zod schema for Blog validation
export const BlogSchema = z.object({
  id: z.guid(),
  createdDate: z.iso.datetime(),
  publishDate: z.iso.datetime(),
  shortDescription: z.string(),
  longDescription: z.string(),
  image: z.string(), // Can be empty string or URL
  tags: z.array(z.string()),
});

// Infer TypeScript type from Zod schema
export type Blog = z.infer<typeof BlogSchema>;

// Base fetch function for projects with validation
export async function fetchProjects(endpoint: string): Promise<Project[]> {
  try {
    const response = await fetch(`${getProjectsBaseUrl()}${endpoint}`, {
      cache: "no-store",
    });

    if (!response.ok) {
      console.error("Failed to fetch projects:", response.statusText);
      return [];
    }

    const data: unknown = await response.json();

    // Validate the response data
    const validationResult = z.array(ProjectSchema).safeParse(data);

    if (!validationResult.success) {
      console.error("Invalid project data:", validationResult.error);
      return [];
    }

    return validationResult.data;
  } catch (error) {
    console.error("Error fetching projects:", error);
    return [];
  }
}

// Base fetch function for a single project with validation
export async function fetchProject(endpoint: string): Promise<Project | null> {
  try {
    const response = await fetch(`${getProjectsBaseUrl()}${endpoint}`, {
      cache: "no-store",
    });

    if (!response.ok) {
      console.error("Failed to fetch project:", response.statusText);
      return null;
    }

    const data: unknown = await response.json();

    // Validate the response data
    const validationResult = ProjectSchema.safeParse(data);

    if (!validationResult.success) {
      console.error("Invalid project data:", validationResult.error);
      return null;
    }

    return validationResult.data;
  } catch (error) {
    console.error("Error fetching project:", error);
    return null;
  }
}

// Base fetch function for blogs with validation
export async function fetchBlogs(endpoint: string): Promise<Blog[]> {
  try {
    const response = await fetch(`${getBlogsBaseUrl()}${endpoint}`, {
      cache: "no-store",
    });

    if (!response.ok) {
      console.error("Failed to fetch blogs:", response.statusText);
      return [];
    }

    const data: unknown = await response.json();

    // Validate the response data
    const validationResult = z.array(BlogSchema).safeParse(data);

    if (!validationResult.success) {
      console.error("Invalid blog data:", validationResult.error);
      return [];
    }

    return validationResult.data;
  } catch (error) {
    console.error("Error fetching blogs:", error);
    return [];
  }
}

// Base fetch function for a single blog with validation
export async function fetchBlog(endpoint: string): Promise<Blog | null> {
  try {
    const response = await fetch(`${getBlogsBaseUrl()}${endpoint}`, {
      cache: "no-store",
    });

    if (!response.ok) {
      console.error("Failed to fetch blog:", response.statusText);
      return null;
    }

    const data: unknown = await response.json();

    // Validate the response data
    const validationResult = BlogSchema.safeParse(data);

    if (!validationResult.success) {
      console.error("Invalid blog data:", validationResult.error);
      return null;
    }

    return validationResult.data;
  } catch (error) {
    console.error("Error fetching blog:", error);
    return null;
  }
}
