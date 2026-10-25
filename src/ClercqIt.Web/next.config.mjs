/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "standalone",
  images: {
    unoptimized: true,
  },
  // Never publish source maps of the browser code (issue #454); the public-bundle check
  // (scripts/check-public-bundle.mjs) fails the build if any .map file ships.
  productionBrowserSourceMaps: false,
  // Bank-grade security headers
  async headers() {
    const isDevelopment = process.env.NODE_ENV === "development";

    // The API is same-origin in production. The admin sign-in (issue #257) calls
    // Keycloak's token endpoint: Cloud IAM in production, the Aspire container locally.
    const connectSrc = isDevelopment
      ? "'self' http://localhost:* ws://localhost:* wss://localhost:*"
      : "'self' https://*.cloud-iam.com";

    return [
      {
        source: "/:path*",
        headers: [
          // Strict Transport Security - Force HTTPS for 1 year (skip in development)
          ...(isDevelopment
            ? []
            : [
                {
                  key: "Strict-Transport-Security",
                  value: "max-age=31536000; includeSubDomains; preload",
                },
              ]),
          // Content Security Policy - Prevent XSS attacks
          {
            key: "Content-Security-Policy",
            value: [
              "default-src 'self'",
              "script-src 'self' 'unsafe-inline' 'unsafe-eval'", // Next.js requires unsafe-inline/eval
              "style-src 'self' 'unsafe-inline'",
              "img-src 'self' data: https: http://localhost:*",
              "font-src 'self' data:",
              `connect-src ${connectSrc}`,
              "frame-ancestors 'none'",
              "base-uri 'self'",
              "form-action 'self'",
            ].join("; "),
          },
          // Prevent clickjacking attacks
          {
            key: "X-Frame-Options",
            value: "DENY",
          },
          // Prevent MIME type sniffing
          {
            key: "X-Content-Type-Options",
            value: "nosniff",
          },
          // XSS Protection (legacy browsers)
          {
            key: "X-XSS-Protection",
            value: "1; mode=block",
          },
          // Referrer Policy - Limit referrer information
          {
            key: "Referrer-Policy",
            value: "strict-origin-when-cross-origin",
          },
          // Permissions Policy - Disable unnecessary browser features
          {
            key: "Permissions-Policy",
            value: "camera=(), microphone=(), geolocation=(), interest-cohort=()",
          },
          // Remove X-Powered-By header for security through obscurity
          {
            key: "X-DNS-Prefetch-Control",
            value: "on",
          },
        ],
      },
    ];
  },
};

export default nextConfig;
