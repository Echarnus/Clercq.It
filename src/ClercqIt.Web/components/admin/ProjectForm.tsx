"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  FormDescription,
} from "@/components/ui/form";
import { TagInput } from "./TagInput";
import { ImageUpload } from "./ImageUpload";
import { Loader2 } from "lucide-react";
import type { Project } from "@/lib/api/baseApi";

const projectFormSchema = z.object({
  title: z
    .string()
    .min(1, "Title is required")
    .max(200, "Title must not exceed 200 characters"),
  shortDescription: z
    .string()
    .min(1, "Short description is required")
    .max(500, "Short description must not exceed 500 characters"),
  longDescription: z
    .string()
    .min(1, "Long description is required")
    .max(50000, "Long description must not exceed 50,000 characters"),
  image: z.instanceof(File).nullable(),
  startDate: z.string().min(1, "Start date is required"),
  endDate: z.string().min(1, "End date is required"),
  featured: z.boolean(),
  skills: z
    .array(z.string())
    .min(1, "At least one skill is required")
    .max(20, "Maximum 20 skills allowed"),
});

type ProjectFormValues = z.infer<typeof projectFormSchema>;

interface ProjectFormProps {
  project?: Project;
  onSubmit: (data: ProjectFormValues) => Promise<void>;
  onCancel: () => void;
  isSubmitting?: boolean;
}

export function ProjectForm({
  project,
  onSubmit,
  onCancel,
  isSubmitting = false,
}: ProjectFormProps) {
  const isEditing = !!project;

  const form = useForm<ProjectFormValues>({
    resolver: zodResolver(projectFormSchema),
    defaultValues: {
      title: project?.title || "",
      shortDescription: project?.shortDescription || "",
      longDescription: project?.longDescription || "",
      image: null,
      startDate: project?.startDate
        ? new Date(project.startDate).toISOString().split("T")[0]
        : "",
      endDate: project?.endDate
        ? new Date(project.endDate).toISOString().split("T")[0]
        : "",
      featured: project?.featured || false,
      skills: project?.skills || [],
    },
  });

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
        <FormField
          control={form.control}
          name="title"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Title</FormLabel>
              <FormControl>
                <Input placeholder="Project title..." {...field} />
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
          name="shortDescription"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Short Description</FormLabel>
              <FormControl>
                <Textarea
                  placeholder="A brief summary of your project..."
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
                  placeholder="Write your full project description here. Markdown is supported..."
                  className="resize-none font-mono"
                  rows={10}
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
          name="image"
          render={({ field }) => (
            <FormItem>
              <FormControl>
                <ImageUpload
                  value={field.value}
                  onChange={field.onChange}
                  existingImageUrl={project?.image || undefined}
                  label="Project Image (Optional)"
                />
              </FormControl>
              <FormDescription>
                Leave empty to use a placeholder image.
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="startDate"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Start Date</FormLabel>
                <FormControl>
                  <Input type="date" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="endDate"
            render={({ field }) => (
              <FormItem>
                <FormLabel>End Date</FormLabel>
                <FormControl>
                  <Input type="date" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <FormField
          control={form.control}
          name="featured"
          render={({ field }) => (
            <FormItem className="flex flex-row items-start space-x-3 space-y-0 rounded-md border p-4">
              <FormControl>
                <Checkbox
                  checked={field.value}
                  onCheckedChange={field.onChange}
                />
              </FormControl>
              <div className="space-y-1 leading-none">
                <FormLabel>Featured Project</FormLabel>
                <FormDescription>
                  This project will be highlighted on the homepage.
                </FormDescription>
              </div>
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="skills"
          render={({ field }) => (
            <FormItem>
              <FormControl>
                <TagInput
                  value={field.value}
                  onChange={field.onChange}
                  placeholder="Add a skill..."
                  maxTags={20}
                  label="Skills"
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
            {isEditing ? "Update Project" : "Create Project"}
          </Button>
        </div>
      </form>
    </Form>
  );
}
