import type { Metadata } from "next";
import { pageMetadata } from "@/lib/seo";
import Link from "next/link";
import { PageContainer } from "@/components/page-container";
import { Markdown } from "@/components/markdown";
import { Card, CardContent, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { BlurImage } from "@/components/ui/blur-image";
import { ExternalLink, Calendar, Building2 } from "lucide-react";
import { imageOrNone } from "@/lib/utils";
import { fetchAllCertifications } from "./lib/api";

export const metadata: Metadata = pageMetadata({
  title: "Certifications",
  description:
    "Professional certifications of Kenneth De Clercq in cloud technologies, software development and engineering practices.",
  path: "/certifications",
});

export default async function CertificationsPage() {
  const certifications = await fetchAllCertifications();

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("en-US", {
      month: "long",
      year: "numeric",
    });
  };

  return (
    <>
      {/* Certifications Header */}
      <PageContainer as="section" className="py-16">
        <div className="max-w-3xl">
          <h1 className="mb-6 text-4xl font-bold text-slate-900 dark:text-white">Certifications</h1>
          <p className="text-xl leading-relaxed text-slate-600 dark:text-slate-300">
            Professional certifications demonstrating expertise in cloud technologies, software
            development, and modern engineering practices.
          </p>
        </div>
      </PageContainer>

      {/* Certifications Grid */}
      <PageContainer as="section" className="pb-16">
        <div>
          <div className="grid gap-6 lg:grid-cols-2">
            {certifications.length === 0 ? (
              <div className="py-12 text-center text-slate-600 dark:text-slate-400">
                No certifications found. Check back soon!
              </div>
            ) : (
              certifications.map((cert) => (
                <Card
                  key={cert.id}
                  className="group h-full border-0 bg-white/80 shadow-lg backdrop-blur-xs transition-all duration-300 hover:shadow-xl md:flex md:flex-row dark:bg-slate-800/80 dark:shadow-slate-900/20"
                >
                  {imageOrNone(cert.image) && (
                    <BlurImage
                      src={cert.image.trim()}
                      alt={cert.title}
                      width={200}
                      height={200}
                      containerClassName="w-full md:w-48 h-48 md:h-auto md:min-h-48 shrink-0 rounded-t-lg md:rounded-l-lg md:rounded-tr-none"
                    />
                  )}

                  <CardContent className="flex flex-col justify-center p-6 md:flex-1">
                    <CardTitle className="mb-2 text-xl text-slate-900 dark:text-white">
                      {cert.title}
                    </CardTitle>

                    <div className="mb-2 flex items-center gap-2 text-sm text-slate-500 dark:text-slate-400">
                      <Building2 className="h-4 w-4" />
                      <span>{cert.issuer}</span>
                    </div>

                    <div className="mb-3 flex items-center gap-2 text-sm text-slate-500 dark:text-slate-400">
                      <Calendar className="h-4 w-4" />
                      <span>
                        Issued {formatDate(cert.issueDate)}
                        {cert.expiryDate && ` - Expires ${formatDate(cert.expiryDate)}`}
                      </span>
                    </div>

                    {cert.description && (
                      <Markdown className="prose-sm mb-4 text-slate-600 dark:text-slate-400">
                        {cert.description}
                      </Markdown>
                    )}

                    <div className="flex flex-wrap items-center gap-3">
                      {cert.credentialId && (
                        <Badge variant="secondary" className="text-xs">
                          ID: {cert.credentialId}
                        </Badge>
                      )}
                      {cert.credentialUrl && (
                        <Link
                          href={cert.credentialUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="inline-flex items-center gap-1 text-sm text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300"
                        >
                          <ExternalLink className="h-4 w-4" />
                          Verify Credential
                        </Link>
                      )}
                    </div>
                  </CardContent>
                </Card>
              ))
            )}
          </div>
        </div>
      </PageContainer>
    </>
  );
}
