"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  FormDescription,
} from "@/components/ui/form";
import { ImageUpload } from "./ImageUpload";
import { Loader2 } from "lucide-react";
import type { Certification } from "@/lib/api/adminApi";

const certificationFormSchema = z.object({
  title: z
    .string()
    .min(1, "Title is required")
    .max(200, "Title must not exceed 200 characters"),
  issuer: z
    .string()
    .min(1, "Issuer is required")
    .max(200, "Issuer must not exceed 200 characters"),
  issueDate: z.string().min(1, "Issue date is required"),
  expiryDate: z.string().optional(),
  credentialId: z.string().max(200, "Credential ID must not exceed 200 characters").optional(),
  credentialUrl: z.string().max(500, "Credential URL must not exceed 500 characters").optional(),
  description: z
    .string()
    .min(1, "Description is required")
    .max(2000, "Description must not exceed 2,000 characters"),
  image: z.instanceof(File).nullable(),
});

type CertificationFormValues = z.infer<typeof certificationFormSchema>;

interface CertificationFormProps {
  certification?: Certification;
  onSubmit: (data: CertificationFormValues) => Promise<void>;
  onCancel: () => void;
  isSubmitting?: boolean;
}

export function CertificationForm({
  certification,
  onSubmit,
  onCancel,
  isSubmitting = false,
}: CertificationFormProps) {
  const isEditing = !!certification;

  const form = useForm<CertificationFormValues>({
    resolver: zodResolver(certificationFormSchema),
    defaultValues: {
      title: certification?.title || "",
      issuer: certification?.issuer || "",
      issueDate: certification?.issueDate
        ? new Date(certification.issueDate).toISOString().split("T")[0]
        : "",
      expiryDate: certification?.expiryDate
        ? new Date(certification.expiryDate).toISOString().split("T")[0]
        : "",
      credentialId: certification?.credentialId || "",
      credentialUrl: certification?.credentialUrl || "",
      description: certification?.description || "",
      image: null,
    },
  });

  const handleSubmit = async (values: CertificationFormValues) => {
    if (!isEditing && !values.image) {
      form.setError("image", { message: "Image is required for new certifications" });
      return;
    }
    await onSubmit(values);
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-6">
        <FormField
          control={form.control}
          name="title"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Title</FormLabel>
              <FormControl>
                <Input placeholder="Certification title..." {...field} />
              </FormControl>
              <FormMessage />
              <p className="text-xs text-muted-foreground">
                {field.value.length}/200 characters
              </p>
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="issuer"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Issuer</FormLabel>
              <FormControl>
                <Input placeholder="Issuing organization..." {...field} />
              </FormControl>
              <FormMessage />
              <p className="text-xs text-muted-foreground">
                {field.value.length}/200 characters
              </p>
            </FormItem>
          )}
        />

        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="issueDate"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Issue Date</FormLabel>
                <FormControl>
                  <Input type="date" max={new Date().toISOString().split("T")[0]} {...field} />
                </FormControl>
                <FormDescription>Cannot be in the future</FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="expiryDate"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Expiry Date (Optional)</FormLabel>
                <FormControl>
                  <Input type="date" {...field} />
                </FormControl>
                <FormDescription>Leave empty if no expiry</FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="credentialId"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Credential ID (Optional)</FormLabel>
                <FormControl>
                  <Input placeholder="ABC123XYZ..." {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="credentialUrl"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Credential URL (Optional)</FormLabel>
                <FormControl>
                  <Input placeholder="https://verify.example.com/..." {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <FormField
          control={form.control}
          name="description"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Description</FormLabel>
              <FormControl>
                <Textarea
                  placeholder="Describe what this certification covers..."
                  className="resize-none"
                  rows={5}
                  {...field}
                />
              </FormControl>
              <FormMessage />
              <p className="text-xs text-muted-foreground">
                {field.value.length}/2,000 characters
              </p>
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="image"
          render={({ field }) => (
            <FormItem>
              <FormControl>
                <ImageUpload
                  value={field.value}
                  onChange={field.onChange}
                  existingImageUrl={certification?.image}
                  label="Certification Badge/Image"
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="flex justify-end gap-3 pt-4">
          <Button type="button" variant="outline" onClick={onCancel}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isEditing ? "Update Certification" : "Create Certification"}
          </Button>
        </div>
      </form>
    </Form>
  );
}
