import { z } from 'zod';

// Base API client configuration
// In production, use relative URLs. In development, use the full API URL.
// Default port 5000 matches the Aspire AppHost configuration
const getBaseApiUrl = () => {
  if (typeof window !== 'undefined') {
    // Client-side: use relative URL in production
    return process.env.NODE_ENV === 'production' ? '' : (process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001');
  }
  // Server-side: always use full URL for SSR
  return process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';
};

export const getApiUrl = () => getBaseApiUrl();
export const getProjectsBaseUrl = () => `${getBaseApiUrl()}/api/projects`;
export const getBlogsBaseUrl = () => `${getBaseApiUrl()}/api/blogs`;

// Zod schema for Project validation
export const ProjectSchema = z.object({
  id: z.string().uuid(),
  startDate: z.string().datetime(),
  endDate: z.string().datetime(),
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
  id: z.string().uuid(),
  createdDate: z.string().datetime(),
  publishDate: z.string().datetime(),
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
      cache: 'no-store',
    });
    
    if (!response.ok) {
      console.error('Failed to fetch projects:', response.statusText);
      return [];
    }
    
    const data = await response.json();
    
    // Validate the response data
    const validationResult = z.array(ProjectSchema).safeParse(data);
    
    if (!validationResult.success) {
      console.error('Invalid project data:', validationResult.error);
      return [];
    }
    
    return validationResult.data;
  } catch (error) {
    console.error('Error fetching projects:', error);
    return [];
  }
}

// Base fetch function for a single project with validation
export async function fetchProject(endpoint: string): Promise<Project | null> {
  try {
    const response = await fetch(`${getProjectsBaseUrl()}${endpoint}`, {
      cache: 'no-store',
    });
    
    if (!response.ok) {
      console.error('Failed to fetch project:', response.statusText);
      return null;
    }
    
    const data = await response.json();
    
    // Validate the response data
    const validationResult = ProjectSchema.safeParse(data);
    
    if (!validationResult.success) {
      console.error('Invalid project data:', validationResult.error);
      return null;
    }
    
    return validationResult.data;
  } catch (error) {
    console.error('Error fetching project:', error);
    return null;
  }
}

// Base fetch function for blogs with validation
export async function fetchBlogs(endpoint: string): Promise<Blog[]> {
  try {
    const response = await fetch(`${getBlogsBaseUrl()}${endpoint}`, {
      cache: 'no-store',
    });
    
    if (!response.ok) {
      console.error('Failed to fetch blogs:', response.statusText);
      return [];
    }
    
    const data = await response.json();
    
    // Validate the response data
    const validationResult = z.array(BlogSchema).safeParse(data);
    
    if (!validationResult.success) {
      console.error('Invalid blog data:', validationResult.error);
      return [];
    }
    
    return validationResult.data;
  } catch (error) {
    console.error('Error fetching blogs:', error);
    return [];
  }
}

// Base fetch function for a single blog with validation
export async function fetchBlog(endpoint: string): Promise<Blog | null> {
  try {
    const response = await fetch(`${getBlogsBaseUrl()}${endpoint}`, {
      cache: 'no-store',
    });
    
    if (!response.ok) {
      console.error('Failed to fetch blog:', response.statusText);
      return null;
    }
    
    const data = await response.json();
    
    // Validate the response data
    const validationResult = BlogSchema.safeParse(data);
    
    if (!validationResult.success) {
      console.error('Invalid blog data:', validationResult.error);
      return null;
    }
    
    return validationResult.data;
  } catch (error) {
    console.error('Error fetching blog:', error);
    return null;
  }
}
