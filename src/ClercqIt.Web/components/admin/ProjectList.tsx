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
import { Pencil, Trash2, Plus, FolderKanban, Loader2, Star } from "lucide-react";
import { useToast } from "@/hooks/use-toast";
import { ProjectForm } from "./ProjectForm";
import {
  fetchAllProjects,
  createProject,
  updateProject,
  deleteProject,
} from "@/lib/api/adminApi";
import type { Project } from "@/lib/api/baseApi";
import Image from "next/image";

export function ProjectList() {
  const [projects, setProjects] = useState<Project[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [selectedProject, setSelectedProject] = useState<Project | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [projectToDelete, setProjectToDelete] = useState<Project | null>(null);
  const { toast } = useToast();

  const loadProjects = async () => {
    setIsLoading(true);
    try {
      const data = await fetchAllProjects();
      setProjects(data);
    } catch {
      toast({
        title: "Error",
        description: "Failed to load projects",
        variant: "destructive",
      });
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadProjects();
  }, []);

  const handleCreate = () => {
    setSelectedProject(null);
    setIsFormOpen(true);
  };

  const handleEdit = (project: Project) => {
    setSelectedProject(project);
    setIsFormOpen(true);
  };

  const handleDeleteClick = (project: Project) => {
    setProjectToDelete(project);
    setIsDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (!projectToDelete) return;

    setIsSubmitting(true);
    try {
      await deleteProject(projectToDelete.id);
      toast({
        title: "Success",
        description: "Project deleted successfully",
      });
      await loadProjects();
    } catch (error) {
      toast({
        title: "Error",
        description:
          error instanceof Error ? error.message : "Failed to delete project",
        variant: "destructive",
      });
    } finally {
      setIsSubmitting(false);
      setIsDeleteDialogOpen(false);
      setProjectToDelete(null);
    }
  };

  const handleFormSubmit = async (data: {
    title: string;
    shortDescription: string;
    longDescription: string;
    image: File | null;
    startDate: string;
    endDate: string;
    featured: boolean;
    skills: string[];
  }) => {
    setIsSubmitting(true);
    try {
      if (selectedProject) {
        await updateProject(selectedProject.id, {
          title: data.title,
          shortDescription: data.shortDescription,
          longDescription: data.longDescription,
          image: data.image || undefined,
          startDate: data.startDate,
          endDate: data.endDate,
          featured: data.featured,
          skills: data.skills,
        });
        toast({
          title: "Success",
          description: "Project updated successfully",
        });
      } else {
        await createProject({
          title: data.title,
          shortDescription: data.shortDescription,
          longDescription: data.longDescription,
          image: data.image || undefined,
          startDate: data.startDate,
          endDate: data.endDate,
          featured: data.featured,
          skills: data.skills,
        });
        toast({
          title: "Success",
          description: "Project created successfully",
        });
      }
      setIsFormOpen(false);
      setSelectedProject(null);
      await loadProjects();
    } catch (error) {
      toast({
        title: "Error",
        description:
          error instanceof Error ? error.message : "Failed to save project",
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
    });
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
        <h3 className="text-lg font-semibold">All Projects ({projects.length})</h3>
        <Button onClick={handleCreate}>
          <Plus className="h-4 w-4 mr-2" />
          Create Project
        </Button>
      </div>

      {projects.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-12 text-center border rounded-lg">
          <FolderKanban className="h-12 w-12 text-muted-foreground mb-4" />
          <h3 className="text-lg font-semibold mb-2">No projects yet</h3>
          <p className="text-sm text-muted-foreground mb-6">
            Add your first project to showcase your work
          </p>
          <Button onClick={handleCreate}>
            <Plus className="mr-2 h-4 w-4" />
            Add Project
          </Button>
        </div>
      ) : (
        <div className="border rounded-lg">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-[80px]">Image</TableHead>
                <TableHead>Title</TableHead>
                <TableHead>Skills</TableHead>
                <TableHead>Period</TableHead>
                <TableHead className="w-[80px]">Featured</TableHead>
                <TableHead className="w-[100px]">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {projects.map((project) => (
                <TableRow key={project.id}>
                  <TableCell>
                    <div className="relative w-16 h-10 rounded overflow-hidden bg-muted">
                      {project.image ? (
                        <Image
                          src={project.image}
                          alt={project.title}
                          fill
                          className="object-cover"
                        />
                      ) : (
                        <div className="w-full h-full flex items-center justify-center bg-gradient-to-br from-blue-500/20 to-purple-500/20">
                          <FolderKanban className="h-5 w-5 text-muted-foreground" />
                        </div>
                      )}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div>
                      <p className="font-medium">{project.title}</p>
                      <p className="text-sm text-muted-foreground line-clamp-1">
                        {project.shortDescription}
                      </p>
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex flex-wrap gap-1">
                      {project.skills.slice(0, 3).map((skill) => (
                        <Badge
                          key={skill}
                          variant="secondary"
                          className="text-xs"
                        >
                          {skill}
                        </Badge>
                      ))}
                      {project.skills.length > 3 && (
                        <Badge variant="outline" className="text-xs">
                          +{project.skills.length - 3}
                        </Badge>
                      )}
                    </div>
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    {formatDate(project.startDate)} -{" "}
                    {formatDate(project.endDate)}
                  </TableCell>
                  <TableCell>
                    {project.featured && (
                      <Star className="h-4 w-4 text-yellow-500 fill-yellow-500" />
                    )}
                  </TableCell>
                  <TableCell>
                    <div className="flex gap-2">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleEdit(project)}
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleDeleteClick(project)}
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
              {selectedProject ? "Edit Project" : "Create Project"}
            </DialogTitle>
            <DialogDescription>
              {selectedProject
                ? "Update the project details below."
                : "Fill in the details to create a new project."}
            </DialogDescription>
          </DialogHeader>
          <ProjectForm
            project={selectedProject || undefined}
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
              project &quot;{projectToDelete?.title}&quot;.
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
