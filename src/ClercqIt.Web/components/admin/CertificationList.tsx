"use client";

import { useState, useEffect } from "react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Pencil, Trash2, Plus, Award, Loader2, ExternalLink } from "lucide-react";
import { useToast } from "@/hooks/use-toast";
import { CertificationForm } from "./CertificationForm";
import {
  fetchAllCertifications,
  createCertification,
  updateCertification,
  deleteCertification,
  type Certification,
} from "@/lib/api/adminApi";
import Image from "next/image";

export function CertificationList() {
  const [certifications, setCertifications] = useState<Certification[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [selectedCertification, setSelectedCertification] = useState<Certification | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [certificationToDelete, setCertificationToDelete] = useState<Certification | null>(null);
  const { toast } = useToast();

  const loadCertifications = async () => {
    setIsLoading(true);
    try {
      const data = await fetchAllCertifications();
      setCertifications(data);
    } catch {
      toast({
        title: "Error",
        description: "Failed to load certifications",
        variant: "destructive",
      });
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadCertifications();
  }, []);

  const handleCreate = () => {
    setSelectedCertification(null);
    setIsFormOpen(true);
  };

  const handleEdit = (certification: Certification) => {
    setSelectedCertification(certification);
    setIsFormOpen(true);
  };

  const handleDeleteClick = (certification: Certification) => {
    setCertificationToDelete(certification);
    setIsDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (!certificationToDelete) return;

    setIsSubmitting(true);
    try {
      await deleteCertification(certificationToDelete.id);
      toast({
        title: "Success",
        description: "Certification deleted successfully",
      });
      await loadCertifications();
    } catch (error) {
      toast({
        title: "Error",
        description:
          error instanceof Error ? error.message : "Failed to delete certification",
        variant: "destructive",
      });
    } finally {
      setIsSubmitting(false);
      setIsDeleteDialogOpen(false);
      setCertificationToDelete(null);
    }
  };

  const handleFormSubmit = async (data: {
    title: string;
    issuer: string;
    issueDate: string;
    expiryDate?: string;
    credentialId?: string;
    credentialUrl?: string;
    description: string;
    image: File | null;
  }) => {
    setIsSubmitting(true);
    try {
      if (selectedCertification) {
        await updateCertification(selectedCertification.id, {
          title: data.title,
          issuer: data.issuer,
          issueDate: data.issueDate,
          expiryDate: data.expiryDate || undefined,
          credentialId: data.credentialId || undefined,
          credentialUrl: data.credentialUrl || undefined,
          description: data.description,
          image: data.image || undefined,
        });
        toast({
          title: "Success",
          description: "Certification updated successfully",
        });
      } else {
        if (!data.image) {
          throw new Error("Image is required");
        }
        await createCertification({
          title: data.title,
          issuer: data.issuer,
          issueDate: data.issueDate,
          expiryDate: data.expiryDate || undefined,
          credentialId: data.credentialId || undefined,
          credentialUrl: data.credentialUrl || undefined,
          description: data.description,
          image: data.image,
        });
        toast({
          title: "Success",
          description: "Certification created successfully",
        });
      }
      setIsFormOpen(false);
      setSelectedCertification(null);
      await loadCertifications();
    } catch (error) {
      toast({
        title: "Error",
        description:
          error instanceof Error ? error.message : "Failed to save certification",
        variant: "destructive",
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
  };

  const isExpired = (expiryDate: string | null) => {
    if (!expiryDate) return false;
    return new Date(expiryDate) < new Date();
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h3 className="text-lg font-semibold">
          All Certifications ({certifications.length})
        </h3>
        <Button onClick={handleCreate}>
          <Plus className="h-4 w-4 mr-2" />
          Add Certification
        </Button>
      </div>

      {certifications.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-12 text-center border rounded-lg">
          <Award className="h-12 w-12 text-muted-foreground mb-4" />
          <h3 className="text-lg font-semibold mb-2">No certifications yet</h3>
          <p className="text-sm text-muted-foreground mb-6">
            Add your first certification to showcase your expertise
          </p>
          <Button onClick={handleCreate}>
            <Plus className="mr-2 h-4 w-4" />
            Add Certification
          </Button>
        </div>
      ) : (
        <div className="border rounded-lg">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-[80px]">Image</TableHead>
                <TableHead>Title</TableHead>
                <TableHead>Issuer</TableHead>
                <TableHead>Issue Date</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="w-[100px]">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {certifications.map((cert) => (
                <TableRow key={cert.id}>
                  <TableCell>
                    <div className="relative w-16 h-16 rounded overflow-hidden bg-muted">
                      {cert.image ? (
                        <Image
                          src={cert.image}
                          alt={cert.title}
                          fill
                          className="object-contain"
                        />
                      ) : (
                        <div className="w-full h-full flex items-center justify-center bg-gradient-to-br from-amber-500/20 to-orange-500/20">
                          <Award className="h-6 w-6 text-muted-foreground" />
                        </div>
                      )}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div>
                      <p className="font-medium">{cert.title}</p>
                      {cert.credentialUrl && (
                        <a
                          href={cert.credentialUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-xs text-primary hover:underline flex items-center gap-1"
                        >
                          Verify <ExternalLink className="h-3 w-3" />
                        </a>
                      )}
                    </div>
                  </TableCell>
                  <TableCell className="text-sm">{cert.issuer}</TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    {formatDate(cert.issueDate)}
                  </TableCell>
                  <TableCell>
                    {cert.expiryDate ? (
                      isExpired(cert.expiryDate) ? (
                        <Badge variant="destructive">Expired</Badge>
                      ) : (
                        <Badge variant="secondary">
                          Expires {formatDate(cert.expiryDate)}
                        </Badge>
                      )
                    ) : (
                      <Badge variant="default">No Expiry</Badge>
                    )}
                  </TableCell>
                  <TableCell>
                    <div className="flex gap-2">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleEdit(cert)}
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleDeleteClick(cert)}
                      >
                        <Trash2 className="h-4 w-4 text-destructive" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Create/Edit Dialog */}
      <Dialog open={isFormOpen} onOpenChange={setIsFormOpen}>
        <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>
              {selectedCertification ? "Edit Certification" : "Add Certification"}
            </DialogTitle>
            <DialogDescription>
              {selectedCertification
                ? "Update the certification details below."
                : "Fill in the details to add a new certification."}
            </DialogDescription>
          </DialogHeader>
          <CertificationForm
            certification={selectedCertification || undefined}
            onSubmit={handleFormSubmit}
            onCancel={() => setIsFormOpen(false)}
            isSubmitting={isSubmitting}
          />
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <AlertDialog
        open={isDeleteDialogOpen}
        onOpenChange={setIsDeleteDialogOpen}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Are you sure?</AlertDialogTitle>
            <AlertDialogDescription>
              This action cannot be undone. This will permanently delete the
              certification &quot;{certificationToDelete?.title}&quot;.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDeleteConfirm}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {isSubmitting ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                "Delete"
              )}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
