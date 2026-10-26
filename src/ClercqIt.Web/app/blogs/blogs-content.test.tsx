import { fireEvent, render, screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { Blog } from "./lib/api";
import { BlogsContent } from "./blogs-content";

// 12 "devops" posts and 3 "intro" posts: the devops filter spans two pages of 10.
const blogs: Blog[] = Array.from({ length: 15 }, (_, i) => ({
  id: `22222222-2222-4222-8222-2222222222${String(i).padStart(2, "0")}`,
  createdDate: "2024-01-01T00:00:00Z",
  publishDate: "2024-01-02T00:00:00Z",
  title: `Post ${i + 1}`,
  shortDescription: `Summary ${i + 1}`,
  longDescription: "Body",
  image: "",
  tags: [i < 12 ? "devops" : "intro"],
}));

describe("BlogsContent", () => {
  it("pages through filtered posts", () => {
    render(<BlogsContent blogs={blogs} />);

    const filters = screen.getByText("Filters").parentElement as HTMLElement;
    fireEvent.click(within(filters).getByText("devops"));
    expect(screen.getByText("Post 1")).toBeInTheDocument();
    expect(screen.queryByText("Post 11")).not.toBeInTheDocument();

    fireEvent.click(screen.getByText("2"));

    expect(screen.getByText("Post 11")).toBeInTheDocument();
    expect(screen.getByText("Post 12")).toBeInTheDocument();
    expect(screen.queryByText("Post 1")).not.toBeInTheDocument();
  });

  it("goes back to page 1 when a filter changes", () => {
    render(<BlogsContent blogs={blogs} />);

    fireEvent.click(screen.getByText("2"));
    expect(screen.getByText("Post 11")).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText("Search"), { target: { value: "Post" } });

    expect(screen.getByText("Post 1")).toBeInTheDocument();
    expect(screen.queryByText("Post 11")).not.toBeInTheDocument();
  });

  it("shows an image only for posts that have one", () => {
    const withImage = { ...blogs[0], id: "with-image", title: "Has image", image: "/a.png" };
    const placeholder = {
      ...blogs[1],
      id: "placeholder",
      title: "Placeholder",
      image: "/placeholder.svg",
    };
    render(<BlogsContent blogs={[withImage, placeholder, blogs[2]]} />);

    expect(screen.getAllByRole("img", { name: "Has image" }).length).toBeGreaterThan(0);
    expect(screen.queryByRole("img", { name: "Placeholder" })).not.toBeInTheDocument();
    expect(screen.queryByRole("img", { name: blogs[2].title })).not.toBeInTheDocument();
  });
});
