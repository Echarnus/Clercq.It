import { z } from 'zod';

// Base API client configuration
// In production, use relative URLs. In development, use the full API URL.
const getBaseApiUrl = () => {
  if (typeof window !== 'undefined') {
    // Client-side: use relative URL in production
    return process.env.NODE_ENV === 'production' ? '' : (process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5035');
  }
  // Server-side: always use full URL for SSR
  return process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5035';
};

export const getApiUrl = () => getBaseApiUrl();
export const getProjectsBaseUrl = () => `${getBaseApiUrl()}/api/projects`;

// Zod schema for Project validation
export const ProjectSchema = z.object({
  id: z.string().uuid(),
  startDate: z.string().datetime(),
  endDate: z.string().datetime(),
  shortDescription: z.string(),
  longDescription: z.string(),
  image: z.string().url(),
  featured: z.boolean(),
  title: z.string().min(1),
  skills: z.array(z.string()),
});

// Infer TypeScript type from Zod schema
export type Project = z.infer<typeof ProjectSchema>;

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
