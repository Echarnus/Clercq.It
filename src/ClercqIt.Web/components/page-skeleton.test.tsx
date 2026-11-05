import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { LoadingSpinner } from "./loading-spinner";
import { PageSkeleton } from "./page-skeleton";

describe("PageSkeleton", () => {
  it("announces what is loading, as a grid of cards by default", () => {
    const { container } = render(<PageSkeleton label="Loading the blog posts" />);
    expect(screen.getByRole("status", { name: "Loading the blog posts" })).toBeInTheDocument();
    expect(container.querySelectorAll(".aspect-video")).toHaveLength(6);
  });

  it("shows one block of text for a detail page", () => {
    const { container } = render(<PageSkeleton label="Loading the project" variant="article" />);
    expect(screen.getByRole("status", { name: "Loading the project" })).toBeInTheDocument();
    expect(container.querySelectorAll(".aspect-video")).toHaveLength(1);
  });
});

describe("LoadingSpinner", () => {
  it("keeps its label for screen readers unless asked to show it", () => {
    const { rerender } = render(<LoadingSpinner label="Loading statistics" />);
    expect(screen.getByRole("status")).toHaveTextContent("Loading statistics");
    expect(screen.getByText("Loading statistics")).toHaveClass("sr-only");

    rerender(<LoadingSpinner label="Loading statistics" showLabel />);
    expect(screen.getByText("Loading statistics")).not.toHaveClass("sr-only");
  });
});
