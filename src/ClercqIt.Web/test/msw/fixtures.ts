import type { Blog, Project } from "@/lib/api/baseApi";
import type { Certification } from "@/lib/api/adminApi";

// The URL the API client uses outside production (lib/api/baseApi.ts).
export const API = "http://localhost:5001";

export const project: Project = {
  id: "11111111-1111-4111-8111-111111111111",
  title: "Portfolio site",
  shortDescription: "This website",
  longDescription: "# Portfolio",
  image: "https://images.example.com/p.png",
  featured: true,
  startDate: "2024-01-01T00:00:00Z",
  endDate: "2024-06-01T00:00:00Z",
  skills: ["dotnet", "nextjs"],
};

export const blog: Blog = {
  id: "22222222-2222-4222-8222-222222222222",
  createdDate: "2024-02-01T00:00:00Z",
  publishDate: "2024-02-02T00:00:00Z",
  title: "Hello world",
  shortDescription: "Hello",
  longDescription: "# Hello",
  image: "",
  tags: ["intro"],
};

export const certification: Certification = {
  id: "33333333-3333-4333-8333-333333333333",
  title: "Azure Developer",
  issuer: "Microsoft",
  issueDate: "2023-05-01T00:00:00Z",
  expiryDate: null,
  credentialId: "ABC-123",
  credentialUrl: "https://learn.microsoft.com/cred",
  description: "AZ-204",
  image: "https://images.example.com/c.png",
};
