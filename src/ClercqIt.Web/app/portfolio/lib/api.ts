import { Project } from '@/lib/api/projectsApi';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5035';

export async function fetchAllProjects(): Promise<Project[]> {
  try {
    const response = await fetch(`${API_URL}/api/projects`, {
      cache: 'no-store', // Ensure fresh data for SSR
    });
    
    if (!response.ok) {
      console.error('Failed to fetch projects:', response.statusText);
      return [];
    }
    
    const projects = await response.json();
    
    // Sort projects by startDate, newest first
    return projects.sort((a: Project, b: Project) => 
      new Date(b.startDate).getTime() - new Date(a.startDate).getTime()
    );
  } catch (error) {
    console.error('Error fetching projects:', error);
    return [];
  }
}

export async function fetchFeaturedProjects(): Promise<Project[]> {
  try {
    const response = await fetch(`${API_URL}/api/projects/featured`, {
      cache: 'no-store',
    });
    
    if (!response.ok) {
      console.error('Failed to fetch featured projects:', response.statusText);
      return [];
    }
    
    return await response.json();
  } catch (error) {
    console.error('Error fetching featured projects:', error);
    return [];
  }
}

export async function fetchProjectById(id: string): Promise<Project | null> {
  try {
    const response = await fetch(`${API_URL}/api/projects/${id}`, {
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
