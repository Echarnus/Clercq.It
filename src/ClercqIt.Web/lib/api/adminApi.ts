import { z } from 'zod';
import { getApiUrl, BlogSchema, ProjectSchema, type Blog, type Project } from './baseApi';

// Certification schema
export const CertificationSchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  issuer: z.string(),
  issueDate: z.string(),
  expiryDate: z.string().nullable(),
  credentialId: z.string(),
  credentialUrl: z.string(),
  description: z.string(),
  image: z.string(),
});

export type Certification = z.infer<typeof CertificationSchema>;

// Get authentication token from localStorage
function getAuthToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem('admin_token');
}

// Authenticated fetch wrapper
async function authFetch(
  endpoint: string,
  options: RequestInit = {}
): Promise<Response> {
  const token = getAuthToken();
  if (!token) {
    throw new Error('Not authenticated');
  }

  const headers = new Headers(options.headers);
  headers.set('Authorization', `Bearer ${token}`);

  return fetch(`${getApiUrl()}${endpoint}`, {
    ...options,
    headers,
  });
}

// ============================================
// BLOGS CRUD
// ============================================

export async function fetchAllBlogs(): Promise<Blog[]> {
  try {
    const response = await fetch(`${getApiUrl()}/api/blogs`, {
      cache: 'no-store',
    });

    if (!response.ok) {
      console.error('Failed to fetch blogs:', response.statusText);
      return [];
    }

    const data = await response.json();
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

export async function fetchBlogById(id: string): Promise<Blog | null> {
  try {
    const response = await fetch(`${getApiUrl()}/api/blogs/${id}`, {
      cache: 'no-store',
    });

    if (!response.ok) {
      return null;
    }

    const data = await response.json();
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

export interface CreateBlogData {
  shortDescription: string;
  longDescription: string;
  image: File;
  tags: string[];
}

export async function createBlog(data: CreateBlogData): Promise<Blog> {
  const formData = new FormData();
  formData.append('shortDescription', data.shortDescription);
  formData.append('longDescription', data.longDescription);
  formData.append('image', data.image);
  formData.append('tags', data.tags.join(','));

  const response = await authFetch('/api/blogs', {
    method: 'POST',
    body: formData,
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to create blog');
  }

  return response.json();
}

export interface UpdateBlogData {
  shortDescription: string;
  longDescription: string;
  image?: File;
  tags: string[];
}

export async function updateBlog(id: string, data: UpdateBlogData): Promise<Blog> {
  const formData = new FormData();
  formData.append('shortDescription', data.shortDescription);
  formData.append('longDescription', data.longDescription);
  if (data.image) {
    formData.append('image', data.image);
  }
  formData.append('tags', data.tags.join(','));

  const response = await authFetch(`/api/blogs/${id}`, {
    method: 'PUT',
    body: formData,
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to update blog');
  }

  return response.json();
}

export async function deleteBlog(id: string): Promise<void> {
  const response = await authFetch(`/api/blogs/${id}`, {
    method: 'DELETE',
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to delete blog');
  }
}

// ============================================
// PROJECTS CRUD
// ============================================

export async function fetchAllProjects(): Promise<Project[]> {
  try {
    const response = await fetch(`${getApiUrl()}/api/projects`, {
      cache: 'no-store',
    });

    if (!response.ok) {
      console.error('Failed to fetch projects:', response.statusText);
      return [];
    }

    const data = await response.json();
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

export async function fetchProjectById(id: string): Promise<Project | null> {
  try {
    const response = await fetch(`${getApiUrl()}/api/projects/${id}`, {
      cache: 'no-store',
    });

    if (!response.ok) {
      return null;
    }

    const data = await response.json();
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

export interface CreateProjectData {
  title: string;
  shortDescription: string;
  longDescription: string;
  image?: File;
  startDate: string;
  endDate: string;
  featured: boolean;
  skills: string[];
}

export async function createProject(data: CreateProjectData): Promise<Project> {
  const formData = new FormData();
  formData.append('title', data.title);
  formData.append('shortDescription', data.shortDescription);
  formData.append('longDescription', data.longDescription);
  if (data.image) {
    formData.append('image', data.image);
  }
  formData.append('startDate', data.startDate);
  formData.append('endDate', data.endDate);
  formData.append('featured', data.featured.toString());
  formData.append('skills', data.skills.join(','));

  const response = await authFetch('/api/projects', {
    method: 'POST',
    body: formData,
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to create project');
  }

  return response.json();
}

export interface UpdateProjectData {
  title: string;
  shortDescription: string;
  longDescription: string;
  image?: File;
  startDate: string;
  endDate: string;
  featured: boolean;
  skills: string[];
}

export async function updateProject(id: string, data: UpdateProjectData): Promise<Project> {
  const formData = new FormData();
  formData.append('title', data.title);
  formData.append('shortDescription', data.shortDescription);
  formData.append('longDescription', data.longDescription);
  if (data.image) {
    formData.append('image', data.image);
  }
  formData.append('startDate', data.startDate);
  formData.append('endDate', data.endDate);
  formData.append('featured', data.featured.toString());
  formData.append('skills', data.skills.join(','));

  const response = await authFetch(`/api/projects/${id}`, {
    method: 'PUT',
    body: formData,
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to update project');
  }

  return response.json();
}

export async function deleteProject(id: string): Promise<void> {
  const response = await authFetch(`/api/projects/${id}`, {
    method: 'DELETE',
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to delete project');
  }
}

// ============================================
// CERTIFICATIONS CRUD
// ============================================

export async function fetchAllCertifications(): Promise<Certification[]> {
  try {
    const response = await fetch(`${getApiUrl()}/api/certifications`, {
      cache: 'no-store',
    });

    if (!response.ok) {
      console.error('Failed to fetch certifications:', response.statusText);
      return [];
    }

    const data = await response.json();
    const validationResult = z.array(CertificationSchema).safeParse(data);

    if (!validationResult.success) {
      console.error('Invalid certification data:', validationResult.error);
      return [];
    }

    return validationResult.data;
  } catch (error) {
    console.error('Error fetching certifications:', error);
    return [];
  }
}

export async function fetchCertificationById(id: string): Promise<Certification | null> {
  try {
    const response = await fetch(`${getApiUrl()}/api/certifications/${id}`, {
      cache: 'no-store',
    });

    if (!response.ok) {
      return null;
    }

    const data = await response.json();
    const validationResult = CertificationSchema.safeParse(data);

    if (!validationResult.success) {
      console.error('Invalid certification data:', validationResult.error);
      return null;
    }

    return validationResult.data;
  } catch (error) {
    console.error('Error fetching certification:', error);
    return null;
  }
}

export interface CreateCertificationData {
  title: string;
  issuer: string;
  issueDate: string;
  expiryDate?: string;
  credentialId?: string;
  credentialUrl?: string;
  description: string;
  image: File;
}

export async function createCertification(data: CreateCertificationData): Promise<Certification> {
  const formData = new FormData();
  formData.append('title', data.title);
  formData.append('issuer', data.issuer);
  formData.append('issueDate', data.issueDate);
  if (data.expiryDate) {
    formData.append('expiryDate', data.expiryDate);
  }
  if (data.credentialId) {
    formData.append('credentialId', data.credentialId);
  }
  if (data.credentialUrl) {
    formData.append('credentialUrl', data.credentialUrl);
  }
  formData.append('description', data.description);
  formData.append('image', data.image);

  const response = await authFetch('/api/certifications', {
    method: 'POST',
    body: formData,
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to create certification');
  }

  return response.json();
}

export interface UpdateCertificationData {
  title: string;
  issuer: string;
  issueDate: string;
  expiryDate?: string;
  credentialId?: string;
  credentialUrl?: string;
  description: string;
  image?: File;
}

export async function updateCertification(id: string, data: UpdateCertificationData): Promise<Certification> {
  const formData = new FormData();
  formData.append('title', data.title);
  formData.append('issuer', data.issuer);
  formData.append('issueDate', data.issueDate);
  if (data.expiryDate) {
    formData.append('expiryDate', data.expiryDate);
  }
  if (data.credentialId) {
    formData.append('credentialId', data.credentialId);
  }
  if (data.credentialUrl) {
    formData.append('credentialUrl', data.credentialUrl);
  }
  formData.append('description', data.description);
  if (data.image) {
    formData.append('image', data.image);
  }

  const response = await authFetch(`/api/certifications/${id}`, {
    method: 'PUT',
    body: formData,
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to update certification');
  }

  return response.json();
}

export async function deleteCertification(id: string): Promise<void> {
  const response = await authFetch(`/api/certifications/${id}`, {
    method: 'DELETE',
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to delete certification');
  }
}
