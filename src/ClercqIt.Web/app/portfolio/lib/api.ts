import { fetchProjects, fetchProject, type Project } from "@/lib/api/baseApi";
import { resolveItem } from "@/lib/seo";

// Re-export Project type for convenience
export type { Project };

export async function fetchAllProjects(): Promise<Project[]> {
  const projects = await fetchProjects("");

  // Sort projects by startDate, newest first
  return projects.sort(
    (a: Project, b: Project) => new Date(b.startDate).getTime() - new Date(a.startDate).getTime(),
  );
}

export async function fetchFeaturedProjects(): Promise<Project[]> {
  return fetchProjects("/featured");
}

export async function fetchProjectById(id: string): Promise<Project | null> {
  return fetchProject(`/${id}`);
}

// Resolves a detail URL segment: `<slug>-<short id>` or an old full GUID (docs/seo.md).
export async function fetchProjectBySegment(segment: string): Promise<Project | null> {
  return resolveItem(segment, fetchProjectById, fetchAllProjects);
}
