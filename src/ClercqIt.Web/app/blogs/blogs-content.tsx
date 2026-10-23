"use client";

import { useState, useMemo } from "react";
import Link from "next/link";
import { Card, CardContent, CardDescription, CardTitle } from "@/components/ui/card";
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
import type { Blog } from "./lib/api";
import { getArchive, matchesFilters, MONTH_NAMES } from "./lib/filters";

interface BlogsContentProps {
  blogs: Blog[];
}

const ITEMS_PER_PAGE = 10;

export function BlogsContent({ blogs }: BlogsContentProps) {
  const [currentPage, setCurrentPage] = useState(1);
  const [titleFilter, setTitleFilter] = useState("");
  const [selectedTags, setSelectedTags] = useState<string[]>([]);
  const [yearFilter, setYearFilter] = useState("");
  const [monthFilter, setMonthFilter] = useState("");

  // Get all unique tags from blogs
  const allTags = useMemo(() => {
    const tags = new Set<string>();
    blogs.forEach((blog) => blog.tags.forEach((tag) => tags.add(tag)));
    return Array.from(tags).sort();
  }, [blogs]);

  // Only offer the years and months that have posts.
  const archive = useMemo(() => getArchive(blogs), [blogs]);
  const monthsOfYear = archive.find((entry) => entry.year === yearFilter)?.months ?? [];

  const filteredBlogs = useMemo(
    () =>
      blogs.filter((blog) =>
        matchesFilters(blog, {
          search: titleFilter,
          tags: selectedTags,
          year: yearFilter,
          month: monthFilter,
        }),
      ),
    [blogs, titleFilter, selectedTags, yearFilter, monthFilter],
  );

  // Paginate blogs
  const totalPages = Math.ceil(filteredBlogs.length / ITEMS_PER_PAGE);
  const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
  const endIndex = startIndex + ITEMS_PER_PAGE;
  const paginatedBlogs = filteredBlogs.slice(startIndex, endIndex);

  // A filter change starts again on page 1. This happens in the handlers, not during render:
  // resetting during render pinned the list to page 1 while any filter was set.
  const updateTitleFilter = (value: string) => {
    setTitleFilter(value);
    setCurrentPage(1);
  };

  // A new year clears the month: that month may have no posts in the new year.
  const updateYearFilter = (year: string) => {
    setYearFilter(year);
    setMonthFilter("");
    setCurrentPage(1);
  };

  const updateMonthFilter = (month: string) => {
    setMonthFilter(month);
    setCurrentPage(1);
  };

  const updateSelectedTags = (update: (prev: string[]) => string[]) => {
    setSelectedTags(update);
    setCurrentPage(1);
  };

  const handleTagToggle = (tag: string) => {
    updateSelectedTags((prev) =>
      prev.includes(tag) ? prev.filter((t) => t !== tag) : [...prev, tag],
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
    <div className="grid grid-cols-1 gap-8 lg:grid-cols-4">
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
                <Card className="group cursor-pointer border-0 bg-white/80 shadow-lg backdrop-blur-xs transition-all duration-300 hover:shadow-xl md:flex md:flex-row dark:bg-slate-800/80 dark:shadow-slate-900/20">
                  <BlurImage
                    src={blog.image || "/placeholder.svg"}
                    alt={blog.title}
                    width={200}
                    height={200}
                    containerClassName="w-full md:w-48 h-48 shrink-0 rounded-t-lg md:rounded-l-lg md:rounded-tr-none"
                  />

                  <CardContent className="flex flex-col justify-center p-6 md:flex-1">
                    <div className="mb-2 text-sm text-slate-500 dark:text-slate-400">
                      {formatDate(blog.publishDate)}
                    </div>

                    <CardTitle className="mb-2 text-xl text-slate-900 dark:text-white">
                      {blog.title}
                    </CardTitle>

                    <CardDescription className="mb-4 leading-relaxed text-slate-600 dark:text-slate-400">
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
                      currentPage === 1 ? "pointer-events-none opacity-50" : "cursor-pointer"
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
        <div className="space-y-6 lg:sticky lg:top-28">
          <Card className="bg-white/80 p-6 backdrop-blur-xs dark:bg-slate-800/80">
            <h3 className="mb-4 text-lg font-semibold text-slate-900 dark:text-white">Filters</h3>

            {/* Title Filter */}
            <div className="mb-6">
              <Label htmlFor="title-filter" className="text-slate-700 dark:text-slate-300">
                Search
              </Label>
              <Input
                id="title-filter"
                type="text"
                placeholder="Search posts..."
                value={titleFilter}
                onChange={(e) => updateTitleFilter(e.target.value)}
                className="mt-2"
              />
            </div>

            {/* Date Filter */}
            {archive.length > 0 && (
              <div className="mb-6">
                <Label className="text-slate-700 dark:text-slate-300">Filter by date</Label>
                <div className="mt-2 grid grid-cols-2 gap-2">
                  <Select value={yearFilter} onValueChange={updateYearFilter}>
                    <SelectTrigger aria-label="Year">
                      <SelectValue placeholder="Year" />
                    </SelectTrigger>
                    <SelectContent>
                      {archive.map(({ year }) => (
                        <SelectItem key={year} value={year}>
                          {year}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <Select
                    value={monthFilter}
                    onValueChange={updateMonthFilter}
                    disabled={!yearFilter}
                  >
                    <SelectTrigger aria-label="Month">
                      <SelectValue placeholder="Any month" />
                    </SelectTrigger>
                    <SelectContent>
                      {monthsOfYear.map((month) => (
                        <SelectItem key={month} value={month}>
                          {MONTH_NAMES[Number(month) - 1]}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                {yearFilter && (
                  <button
                    onClick={() => updateYearFilter("")}
                    className="mt-2 text-sm text-slate-600 underline hover:text-slate-900 dark:text-slate-400 dark:hover:text-white"
                  >
                    Clear date filter
                  </button>
                )}
              </div>
            )}

            {/* Tags Filter */}
            <div>
              <Label className="mb-2 block text-slate-700 dark:text-slate-300">
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
                  onClick={() => updateSelectedTags(() => [])}
                  className="mt-2 text-sm text-slate-600 underline hover:text-slate-900 dark:text-slate-400 dark:hover:text-white"
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
