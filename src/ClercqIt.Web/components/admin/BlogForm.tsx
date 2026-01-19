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
} from "@/components/ui/form";
import { TagInput } from "./TagInput";
import { ImageUpload } from "./ImageUpload";
import { Loader2 } from "lucide-react";
import type { Blog } from "@/lib/api/baseApi";

const blogFormSchema = z.object({
  shortDescription: z
    .string()
    .min(1, "Short description is required")
    .max(500, "Short description must not exceed 500 characters"),
  longDescription: z
    .string()
    .min(1, "Long description is required")
    .max(50000, "Long description must not exceed 50,000 characters"),
  tags: z
    .array(z.string())
    .min(1, "At least one tag is required")
    .max(10, "Maximum 10 tags allowed"),
  image: z.instanceof(File).nullable(),
});

type BlogFormValues = z.infer<typeof blogFormSchema>;

interface BlogFormProps {
  blog?: Blog;
  onSubmit: (data: BlogFormValues) => Promise<void>;
  onCancel: () => void;
  isSubmitting?: boolean;
}

export function BlogForm({ blog, onSubmit, onCancel, isSubmitting = false }: BlogFormProps) {
  const isEditing = !!blog;

  const form = useForm<BlogFormValues>({
    resolver: zodResolver(blogFormSchema),
    defaultValues: {
      shortDescription: blog?.shortDescription || "",
      longDescription: blog?.longDescription || "",
      tags: blog?.tags || [],
      image: null,
    },
  });

  const handleSubmit = async (values: BlogFormValues) => {
    if (!isEditing && !values.image) {
      form.setError("image", { message: "Image is required for new blogs" });
      return;
    }
    await onSubmit(values);
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-6">
        <FormField
          control={form.control}
          name="shortDescription"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Short Description</FormLabel>
              <FormControl>
                <Textarea
                  placeholder="A brief summary of your blog post..."
                  className="resize-none"
                  rows={3}
                  {...field}
                />
              </FormControl>
              <FormMessage />
              <p className="text-xs text-muted-foreground">
                {field.value.length}/500 characters
              </p>
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="longDescription"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Long Description (Markdown supported)</FormLabel>
              <FormControl>
                <Textarea
                  placeholder="Write your full blog post content here. Markdown is supported..."
                  className="resize-none font-mono"
                  rows={15}
                  {...field}
                />
              </FormControl>
              <FormMessage />
              <p className="text-xs text-muted-foreground">
                {field.value.length}/50,000 characters
              </p>
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="tags"
          render={({ field }) => (
            <FormItem>
              <FormControl>
                <TagInput
                  value={field.value}
                  onChange={field.onChange}
                  placeholder="Add a tag..."
                  maxTags={10}
                />
              </FormControl>
              <FormMessage />
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
                  existingImageUrl={blog?.image}
                  label="Blog Header Image"
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
            {isEditing ? "Update Blog" : "Create Blog"}
          </Button>
        </div>
      </form>
    </Form>
  );
}
