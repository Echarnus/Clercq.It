import fs from "fs/promises";
import path from "path";
import matter from "gray-matter";

export type Project = {
  title: string;
  description: string;
  image: string;
  tags: string[];
  liveUrl?: string | null;
  githubUrl?: string | null;
  featured?: boolean;
};

export class ProjectsService {
  // At runtime prefer the public folder so files are available in production Docker image
  private publicProjectsDir = path.join(process.cwd(), "public", "content", "projects");
  // Fallback for local development where markdown lives in the source tree
  private srcProjectsDir = path.join(
    process.cwd(),
    "src",
    "ClercqIt.Web",
    "content",
    "projects"
  );

  private async dirExists(p: string) {
    try {
      const st = await fs.stat(p);
      return st.isDirectory();
    } catch {
      return false;
    }
  }

  private async readFilesFromDir(dir: string) {
    let files: string[] = [];
    try {
      files = await fs.readdir(dir);
    } catch {
      return [];
    }

    const mdFiles = files.filter((f) => f.endsWith(".md") || f.endsWith(".mdx"));

    const projects = await Promise.all(
      mdFiles.map(async (file) => {
        const filePath = path.join(dir, file);
        const raw = await fs.readFile(filePath, "utf8");
        const { data } = matter(raw);

        const tags = Array.isArray(data.tags)
          ? data.tags.map(String)
          : typeof data.tags === "string"
          ? data.tags.split(",").map((s: string) => s.trim())
          : [];

        const project: Project = {
          title: data.title ?? file.replace(/\.mdx?$/, ""),
          description: data.description ?? "",
          image: data.image ?? "/placeholder.svg",
          tags,
          liveUrl: data.liveUrl ?? null,
          githubUrl: data.githubUrl ?? null,
          featured: !!data.featured,
        };

        return project;
      })
    );

    projects.sort((a, b) => Number(b.featured) - Number(a.featured));
    return projects;
  }

  async getProjects(): Promise<Project[]> {
    // Prefer public dir at runtime (this folder is copied into the Docker runner image)
    if (await this.dirExists(this.publicProjectsDir)) {
      return await this.readFilesFromDir(this.publicProjectsDir);
    }

    // Fallback to reading from source content during local dev
    if (await this.dirExists(this.srcProjectsDir)) {
      return await this.readFilesFromDir(this.srcProjectsDir);
    }

    return [];
  }
}

// Note: no default/shared instance exported to allow per-request instances via the DI container
