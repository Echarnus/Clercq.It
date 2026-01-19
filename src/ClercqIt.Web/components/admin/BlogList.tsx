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
import { Pencil, Trash2, Plus, FileText, Loader2 } from "lucide-react";
import { useToast } from "@/hooks/use-toast";
import { BlogForm } from "./BlogForm";
import {
  fetchAllBlogs,
  createBlog,
  updateBlog,
  deleteBlog,
} from "@/lib/api/adminApi";
import type { Blog } from "@/lib/api/baseApi";
import Image from "next/image";

export function BlogList() {
  const [blogs, setBlogs] = useState<Blog[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [selectedBlog, setSelectedBlog] = useState<Blog | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [blogToDelete, setBlogToDelete] = useState<Blog | null>(null);
  const { toast } = useToast();

  const loadBlogs = async () => {
    setIsLoading(true);
    try {
      const data = await fetchAllBlogs();
      setBlogs(data);
    } catch {
      toast({
        title: "Error",
        description: "Failed to load blogs",
        variant: "destructive",
      });
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadBlogs();
  }, []);

  const handleCreate = () => {
    setSelectedBlog(null);
    setIsFormOpen(true);
  };

  const handleEdit = (blog: Blog) => {
    setSelectedBlog(blog);
    setIsFormOpen(true);
  };

  const handleDeleteClick = (blog: Blog) => {
    setBlogToDelete(blog);
    setIsDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (!blogToDelete) return;

    setIsSubmitting(true);
    try {
      await deleteBlog(blogToDelete.id);
      toast({
        title: "Success",
        description: "Blog deleted successfully",
      });
      await loadBlogs();
    } catch (error) {
      toast({
        title: "Error",
        description: error instanceof Error ? error.message : "Failed to delete blog",
        variant: "destructive",
      });
    } finally {
      setIsSubmitting(false);
      setIsDeleteDialogOpen(false);
      setBlogToDelete(null);
    }
  };

  const handleFormSubmit = async (data: {
    shortDescription: string;
    longDescription: string;
    tags: string[];
    image: File | null;
  }) => {
    setIsSubmitting(true);
    try {
      if (selectedBlog) {
        await updateBlog(selectedBlog.id, {
          shortDescription: data.shortDescription,
          longDescription: data.longDescription,
          tags: data.tags,
          image: data.image || undefined,
        });
        toast({
          title: "Success",
          description: "Blog updated successfully",
        });
      } else {
        if (!data.image) {
          throw new Error("Image is required");
        }
        await createBlog({
          shortDescription: data.shortDescription,
          longDescription: data.longDescription,
          tags: data.tags,
          image: data.image,
        });
        toast({
          title: "Success",
          description: "Blog created successfully",
        });
      }
      setIsFormOpen(false);
      setSelectedBlog(null);
      await loadBlogs();
    } catch (error) {
      toast({
        title: "Error",
        description: error instanceof Error ? error.message : "Failed to save blog",
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
        <h3 className="text-lg font-semibold">All Blogs ({blogs.length})</h3>
        <Button onClick={handleCreate}>
          <Plus className="h-4 w-4 mr-2" />
          Create Blog
        </Button>
      </div>

      {blogs.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-12 text-center border rounded-lg">
          <FileText className="h-12 w-12 text-muted-foreground mb-4" />
          <h3 className="text-lg font-semibold mb-2">No blogs yet</h3>
          <p className="text-sm text-muted-foreground mb-6">
            Create your first blog post to get started
          </p>
          <Button onClick={handleCreate}>
            <Plus className="mr-2 h-4 w-4" />
            Create Blog Post
          </Button>
        </div>
      ) : (
        <div className="border rounded-lg">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-[80px]">Image</TableHead>
                <TableHead>Description</TableHead>
                <TableHead>Tags</TableHead>
                <TableHead>Published</TableHead>
                <TableHead className="w-[100px]">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {blogs.map((blog) => (
                <TableRow key={blog.id}>
                  <TableCell>
                    <div className="relative w-16 h-10 rounded overflow-hidden bg-muted">
                      {blog.image ? (
                        <Image
                          src={blog.image}
                          alt="Blog thumbnail"
                          fill
                          className="object-cover"
                        />
                      ) : (
                        <div className="w-full h-full flex items-center justify-center bg-gradient-to-br from-green-500/20 to-teal-500/20">
                          <FileText className="h-5 w-5 text-muted-foreground" />
                        </div>
                      )}
                    </div>
                  </TableCell>
                  <TableCell>
                    <p className="line-clamp-2 text-sm">
                      {blog.shortDescription}
                    </p>
                  </TableCell>
                  <TableCell>
                    <div className="flex flex-wrap gap-1">
                      {blog.tags.slice(0, 3).map((tag) => (
                        <Badge key={tag} variant="secondary" className="text-xs">
                          {tag}
                        </Badge>
                      ))}
                      {blog.tags.length > 3 && (
                        <Badge variant="outline" className="text-xs">
                          +{blog.tags.length - 3}
                        </Badge>
                      )}
                    </div>
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    {formatDate(blog.publishDate)}
                  </TableCell>
                  <TableCell>
                    <div className="flex gap-2">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleEdit(blog)}
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleDeleteClick(blog)}
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
              {selectedBlog ? "Edit Blog" : "Create Blog"}
            </DialogTitle>
            <DialogDescription>
              {selectedBlog
                ? "Update the blog post details below."
                : "Fill in the details to create a new blog post."}
            </DialogDescription>
          </DialogHeader>
          <BlogForm
            blog={selectedBlog || undefined}
            onSubmit={handleFormSubmit}
            onCancel={() => setIsFormOpen(false)}
            isSubmitting={isSubmitting}
          />
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={isDeleteDialogOpen} onOpenChange={setIsDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Are you sure?</AlertDialogTitle>
            <AlertDialogDescription>
              This action cannot be undone. This will permanently delete the blog
              post.
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
