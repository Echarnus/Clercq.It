import { http, HttpResponse } from "msw";
import { API, blog, certification, project } from "./fixtures";

// Default handlers: the public read endpoints return one item each. Tests
// override them with server.use(...) for error and write cases.
export const handlers = [
  http.get(`${API}/api/projects`, () => HttpResponse.json([project])),
  http.get(`${API}/api/projects/featured`, () => HttpResponse.json([project])),
  http.get(`${API}/api/projects/:id`, ({ params }) =>
    params.id === project.id ? HttpResponse.json(project) : new HttpResponse(null, { status: 404 }),
  ),
  http.get(`${API}/api/blogs`, () => HttpResponse.json([blog])),
  http.get(`${API}/api/blogs/:id`, ({ params }) =>
    params.id === blog.id ? HttpResponse.json(blog) : new HttpResponse(null, { status: 404 }),
  ),
  http.get(`${API}/api/certifications`, () => HttpResponse.json([certification])),
  http.get(`${API}/api/certifications/:id`, ({ params }) =>
    params.id === certification.id
      ? HttpResponse.json(certification)
      : new HttpResponse(null, { status: 404 }),
  ),
  // Page views from the site's tracker are accepted and dropped.
  http.post(`${API}/api/stats/pageview`, () => new HttpResponse(null, { status: 204 })),
  // Every feature flag is off unless a test turns it on.
  http.get(`${API}/api/features`, () => HttpResponse.json({})),
  // The Next.js route that keeps the admin session cookie (lib/adminSession.ts).
  http.post("http://localhost:3000/admin/session", () => new HttpResponse(null, { status: 204 })),
  http.delete("http://localhost:3000/admin/session", () => new HttpResponse(null, { status: 204 })),
  http.get(`${API}/api/auth/config`, () =>
    HttpResponse.json({
      authority: "http://localhost:8080/realms/clercqit",
      clientId: "clercqit-web",
    }),
  ),
];
