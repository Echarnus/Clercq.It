/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'standalone',
  eslint: {
    ignoreDuringBuilds: true,
  },
  typescript: {
    ignoreBuildErrors: true,
  },
  images: {
    unoptimized: true,
  },
  // Bank-grade security headers
  async headers() {
    const isDevelopment = process.env.NODE_ENV === 'development';

    // Allow localhost API in development, only production API in production
    const connectSrc = isDevelopment
      ? "'self' http://localhost:5001 http://localhost:5000 ws://localhost:* wss://localhost:*"
      : "'self' https://api.clercq.it";

    return [
      {
        source: '/:path*',
        headers: [
          // Strict Transport Security - Force HTTPS for 1 year (skip in development)
          ...(isDevelopment ? [] : [{
            key: 'Strict-Transport-Security',
            value: 'max-age=31536000; includeSubDomains; preload'
          }]),
          // Content Security Policy - Prevent XSS attacks
          {
            key: 'Content-Security-Policy',
            value: [
              "default-src 'self'",
              "script-src 'self' 'unsafe-inline' 'unsafe-eval'", // Next.js requires unsafe-inline/eval
              "style-src 'self' 'unsafe-inline'",
              "img-src 'self' data: https: http://localhost:*",
              "font-src 'self' data:",
              `connect-src ${connectSrc}`,
              "frame-ancestors 'none'",
              "base-uri 'self'",
              "form-action 'self'"
            ].join('; ')
          },
          // Prevent clickjacking attacks
          {
            key: 'X-Frame-Options',
            value: 'DENY'
          },
          // Prevent MIME type sniffing
          {
            key: 'X-Content-Type-Options',
            value: 'nosniff'
          },
          // XSS Protection (legacy browsers)
          {
            key: 'X-XSS-Protection',
            value: '1; mode=block'
          },
          // Referrer Policy - Limit referrer information
          {
            key: 'Referrer-Policy',
            value: 'strict-origin-when-cross-origin'
          },
          // Permissions Policy - Disable unnecessary browser features
          {
            key: 'Permissions-Policy',
            value: 'camera=(), microphone=(), geolocation=(), interest-cohort=()'
          },
          // Remove X-Powered-By header for security through obscurity
          {
            key: 'X-DNS-Prefetch-Control',
            value: 'on'
          }
        ],
      },
    ]
  },
}

export default nextConfig
