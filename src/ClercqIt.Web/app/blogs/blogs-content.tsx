"use client";

import { useState, useMemo } from "react";
import Link from "next/link";
import { Card, CardContent, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { BlurImage } from "@/components/ui/blur-image";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Pagination,
  PaginationContent,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "@/components/ui/pagination";
import type { Blog } from "../lib/api";

interface BlogsContentProps {
  blogs: Blog[];
}

const ITEMS_PER_PAGE = 10;

export function BlogsContent({ blogs }: BlogsContentProps) {
  const [currentPage, setCurrentPage] = useState(1);
  const [titleFilter, setTitleFilter] = useState("");
  const [selectedTags, setSelectedTags] = useState<string[]>([]);
  const [dateFilter, setDateFilter] = useState("");

  // Get all unique tags from blogs
  const allTags = useMemo(() => {
    const tags = new Set<string>();
    blogs.forEach((blog) => blog.tags.forEach((tag) => tags.add(tag)));
    return Array.from(tags).sort();
  }, [blogs]);

  // Filter blogs
  const filteredBlogs = useMemo(() => {
    return blogs.filter((blog) => {
      // Filter by title
      if (titleFilter && !blog.shortDescription.toLowerCase().includes(titleFilter.toLowerCase())) {
        return false;
      }

      // Filter by tags
      if (selectedTags.length > 0) {
        const hasTag = selectedTags.some((tag) => blog.tags.includes(tag));
        if (!hasTag) return false;
      }

      // Filter by date (year/month)
      if (dateFilter) {
        const blogDate = new Date(blog.publishDate);
        const filterDate = new Date(dateFilter);
        if (
          blogDate.getFullYear() !== filterDate.getFullYear() ||
          blogDate.getMonth() !== filterDate.getMonth()
        ) {
          return false;
        }
      }

      return true;
    });
  }, [blogs, titleFilter, selectedTags, dateFilter]);

  // Paginate blogs
  const totalPages = Math.ceil(filteredBlogs.length / ITEMS_PER_PAGE);
  const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
  const endIndex = startIndex + ITEMS_PER_PAGE;
  const paginatedBlogs = filteredBlogs.slice(startIndex, endIndex);

  // Reset to page 1 when filters change
  if (titleFilter || selectedTags.length > 0 || dateFilter) {
    if (currentPage !== 1) {
      setCurrentPage(1);
    }
  }

  const handleTagToggle = (tag: string) => {
    setSelectedTags((prev) =>
      prev.includes(tag) ? prev.filter((t) => t !== tag) : [...prev, tag]
    );
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("en-US", {
      month: "long",
      day: "numeric",
      year: "numeric",
    });
  };

  return (
    <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
      {/* Main Content */}
      <div className="lg:col-span-3">
        <div className="grid gap-8">
          {paginatedBlogs.length === 0 ? (
            <div className="text-center text-slate-600 dark:text-slate-400">
              No blogs found matching your filters.
            </div>
          ) : (
            paginatedBlogs.map((blog) => (
              <Link key={blog.id} href={`/blogs/${blog.id}`} className="block">
                <Card className="group hover:shadow-xl transition-all duration-300 border-0 shadow-lg bg-white/80 backdrop-blur-sm dark:bg-slate-800/80 dark:shadow-slate-900/20 cursor-pointer md:flex md:flex-row">
                  <BlurImage
                    src={blog.image || "/placeholder.svg"}
                    alt={blog.shortDescription}
                    width={200}
                    height={200}
                    containerClassName="w-full md:w-48 h-48 flex-shrink-0 rounded-t-lg md:rounded-l-lg md:rounded-tr-none"
                  />

                  <CardContent className="p-6 md:flex-1 flex flex-col justify-center">
                    <div className="mb-2 text-sm text-slate-500 dark:text-slate-400">
                      {formatDate(blog.publishDate)}
                    </div>

                    <CardDescription className="text-slate-600 mb-4 leading-relaxed dark:text-slate-400">
                      {blog.shortDescription}
                    </CardDescription>

                    <div className="flex flex-wrap gap-2">
                      {blog.tags.map((tag) => (
                        <Badge key={tag} variant="secondary" className="text-xs">
                          {tag}
                        </Badge>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              </Link>
            ))
          )}
        </div>

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="mt-8">
            <Pagination>
              <PaginationContent>
                <PaginationItem>
                  <PaginationPrevious
                    onClick={() => setCurrentPage((prev) => Math.max(prev - 1, 1))}
                    className={
                      currentPage === 1
                        ? "pointer-events-none opacity-50"
                        : "cursor-pointer"
                    }
                  />
                </PaginationItem>

                {Array.from({ length: totalPages }, (_, i) => i + 1).map((page) => (
                  <PaginationItem key={page}>
                    <PaginationLink
                      onClick={() => setCurrentPage(page)}
                      isActive={currentPage === page}
                      className="cursor-pointer"
                    >
                      {page}
                    </PaginationLink>
                  </PaginationItem>
                ))}

                <PaginationItem>
                  <PaginationNext
                    onClick={() => setCurrentPage((prev) => Math.min(prev + 1, totalPages))}
                    className={
                      currentPage === totalPages
                        ? "pointer-events-none opacity-50"
                        : "cursor-pointer"
                    }
                  />
                </PaginationItem>
              </PaginationContent>
            </Pagination>
          </div>
        )}
      </div>

      {/* Sidebar Filters */}
      <div className="lg:col-span-1">
        <div className="sticky top-4 space-y-6">
          <Card className="p-6 bg-white/80 backdrop-blur-sm dark:bg-slate-800/80">
            <h3 className="text-lg font-semibold mb-4 text-slate-900 dark:text-white">
              Filters
            </h3>

            {/* Title Filter */}
            <div className="mb-6">
              <Label htmlFor="title-filter" className="text-slate-700 dark:text-slate-300">
                Search
              </Label>
              <Input
                id="title-filter"
                type="text"
                placeholder="Search blogs..."
                value={titleFilter}
                onChange={(e) => setTitleFilter(e.target.value)}
                className="mt-2"
              />
            </div>

            {/* Date Filter */}
            <div className="mb-6">
              <Label className="text-slate-700 dark:text-slate-300">
                Filter by Month
              </Label>
              <div className="grid grid-cols-2 gap-2 mt-2">
                <Select
                  value={dateFilter ? dateFilter.split("-")[0] : ""}
                  onValueChange={(year) => {
                    if (year) {
                      const month = dateFilter ? dateFilter.split("-")[1] : "01";
                      setDateFilter(`${year}-${month}`);
                    } else {
                      setDateFilter("");
                    }
                  }}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Year" />
                  </SelectTrigger>
                  <SelectContent>
                    {Array.from({ length: 10 }, (_, i) => {
                      const year = new Date().getFullYear() - i;
                      return (
                        <SelectItem key={year} value={year.toString()}>
                          {year}
                        </SelectItem>
                      );
                    })}
                  </SelectContent>
                </Select>
                <Select
                  value={dateFilter ? dateFilter.split("-")[1] : ""}
                  onValueChange={(month) => {
                    if (month && dateFilter) {
                      const year = dateFilter.split("-")[0];
                      setDateFilter(`${year}-${month}`);
                    }
                  }}
                  disabled={!dateFilter}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Month" />
                  </SelectTrigger>
                  <SelectContent>
                    {["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"].map((month, i) => (
                      <SelectItem key={month} value={String(i + 1).padStart(2, "0")}>
                        {month}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              {dateFilter && (
                <button
                  onClick={() => setDateFilter("")}
                  className="text-sm text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white mt-2 underline"
                >
                  Clear date filter
                </button>
              )}
            </div>

            {/* Tags Filter */}
            <div>
              <Label className="text-slate-700 dark:text-slate-300 mb-2 block">
                Filter by Tags
              </Label>
              <div className="flex flex-wrap gap-2">
                {allTags.map((tag) => (
                  <Badge
                    key={tag}
                    variant={selectedTags.includes(tag) ? "default" : "outline"}
                    className={`cursor-pointer transition-all ${
                      selectedTags.includes(tag)
                        ? "bg-slate-900 text-white hover:bg-slate-800 dark:bg-white dark:text-slate-900 dark:hover:bg-slate-200"
                        : "border-slate-300 text-slate-700 hover:bg-slate-100 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700"
                    }`}
                    onClick={() => handleTagToggle(tag)}
                  >
                    {tag}
                  </Badge>
                ))}
              </div>
              {selectedTags.length > 0 && (
                <button
                  onClick={() => setSelectedTags([])}
                  className="text-sm text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white mt-2 underline"
                >
                  Clear tags
                </button>
              )}
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}
