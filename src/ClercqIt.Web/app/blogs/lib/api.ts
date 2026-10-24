import { fetchBlogs, fetchBlog, type Blog } from "@/lib/api/baseApi";
import { resolveItem } from "@/lib/seo";

// Re-export Blog type for convenience
export type { Blog };

export async function fetchAllBlogs(): Promise<Blog[]> {
  const blogs = await fetchBlogs("");

  // Sort blogs by publishDate, newest first
  return blogs.sort(
    (a: Blog, b: Blog) => new Date(b.publishDate).getTime() - new Date(a.publishDate).getTime(),
  );
}

export async function fetchBlogById(id: string): Promise<Blog | null> {
  return fetchBlog(`/${id}`);
}

// Resolves a detail URL segment: `<slug>-<short id>` or an old full GUID (docs/seo.md).
export async function fetchBlogBySegment(segment: string): Promise<Blog | null> {
  return resolveItem(segment, fetchBlogById, fetchAllBlogs);
}
