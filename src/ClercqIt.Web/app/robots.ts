import type { MetadataRoute } from "next";
import { SITE_URL } from "@/lib/seo";

// /robots.txt (docs/seo.md). The admin and the API are not for search engines.
export default function robots(): MetadataRoute.Robots {
  return {
    rules: { userAgent: "*", allow: "/", disallow: ["/admin", "/api/"] },
    sitemap: `${SITE_URL}/sitemap.xml`,
    host: SITE_URL,
  };
}
