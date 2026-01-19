import Link from "next/link";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { BlurImage } from "@/components/ui/blur-image";
import { ArrowLeft, ExternalLink, Calendar, Building2 } from "lucide-react";
import { fetchAllCertifications } from "./lib/api";

export default async function CertificationsPage() {
  const certifications = await fetchAllCertifications();

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("en-US", {
      month: "long",
      year: "numeric",
    });
  };

  return (
    <div
      className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800"
      style={{ fontFamily: "Arial, sans-serif" }}
    >
      {/* Certifications Header */}
      <section className="container mx-auto px-4 py-16">
        <div className="max-w-4xl mx-auto">
          <Button asChild variant="ghost" className="mb-8">
            <Link href="/">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Home
            </Link>
          </Button>

          <h2 className="text-4xl font-bold text-slate-900 mb-6 dark:text-white">
            Certifications
          </h2>
          <p className="text-xl text-slate-600 leading-relaxed dark:text-slate-300">
            Professional certifications demonstrating expertise in cloud
            technologies, software development, and modern engineering practices.
          </p>
        </div>
      </section>

      {/* Certifications Grid */}
      <section className="container mx-auto px-4 pb-16">
        <div className="max-w-4xl mx-auto">
          <div className="grid gap-6">
            {certifications.length === 0 ? (
              <div className="text-center text-slate-600 dark:text-slate-400 py-12">
                No certifications found. Check back soon!
              </div>
            ) : (
              certifications.map((cert) => (
                <Card
                  key={cert.id}
                  className="group hover:shadow-xl transition-all duration-300 border-0 shadow-lg bg-white/80 backdrop-blur-sm dark:bg-slate-800/80 dark:shadow-slate-900/20 md:flex md:flex-row"
                >
                  {cert.image ? (
                    <BlurImage
                      src={cert.image}
                      alt={cert.title}
                      width={200}
                      height={200}
                      containerClassName="w-full md:w-48 h-48 flex-shrink-0 rounded-t-lg md:rounded-l-lg md:rounded-tr-none"
                    />
                  ) : (
                    <div className="w-full md:w-48 h-48 flex-shrink-0 flex items-center justify-center bg-slate-100 dark:bg-slate-700/50 rounded-t-lg md:rounded-l-lg md:rounded-tr-none">
                      <Building2 className="w-12 h-12 text-slate-400" />
                    </div>
                  )}

                  <CardContent className="p-6 md:flex-1 flex flex-col justify-center">
                    <CardTitle className="text-slate-900 text-xl mb-2 dark:text-white">
                      {cert.title}
                    </CardTitle>

                    <div className="flex items-center gap-2 text-sm text-slate-500 dark:text-slate-400 mb-2">
                      <Building2 className="w-4 h-4" />
                      <span>{cert.issuer}</span>
                    </div>

                    <div className="flex items-center gap-2 text-sm text-slate-500 dark:text-slate-400 mb-3">
                      <Calendar className="w-4 h-4" />
                      <span>
                        Issued {formatDate(cert.issueDate)}
                        {cert.expiryDate && ` - Expires ${formatDate(cert.expiryDate)}`}
                      </span>
                    </div>

                    {cert.description && (
                      <CardDescription className="text-slate-600 mb-4 leading-relaxed dark:text-slate-400">
                        {cert.description}
                      </CardDescription>
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
                          <ExternalLink className="w-4 h-4" />
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
      </section>
    </div>
  );
}
