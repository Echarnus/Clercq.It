// Base API client configuration
const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5035';

export const getApiUrl = () => API_URL;
export const getProjectsBaseUrl = () => `${API_URL}/api/projects`;

export interface Project {
  id: string;
  startDate: string;
  endDate: string;
  shortDescription: string;
  longDescription: string;
  image: string;
  featured: boolean;
  title: string;
  skills: string[];
}

// Base fetch function for projects
export async function fetchProjects(endpoint: string): Promise<Project[]> {
  try {
    const response = await fetch(`${getProjectsBaseUrl()}${endpoint}`, {
      cache: 'no-store',
    });
    
    if (!response.ok) {
      console.error('Failed to fetch projects:', response.statusText);
      return [];
    }
    
    return await response.json();
  } catch (error) {
    console.error('Error fetching projects:', error);
    return [];
  }
}

// Base fetch function for a single project
export async function fetchProject(endpoint: string): Promise<Project | null> {
  try {
    const response = await fetch(`${getProjectsBaseUrl()}${endpoint}`, {
      cache: 'no-store',
    });
    
    if (!response.ok) {
      console.error('Failed to fetch project:', response.statusText);
      return null;
    }
    
    return await response.json();
  } catch (error) {
    console.error('Error fetching project:', error);
    return null;
  }
}
