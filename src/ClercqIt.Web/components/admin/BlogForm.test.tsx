import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { blog } from "@/test/msw/fixtures";
import { BlogForm } from "./BlogForm";

describe("BlogForm", () => {
  it("shows validation errors and does not submit an empty form", async () => {
    const onSubmit = vi.fn();
    render(<BlogForm onSubmit={onSubmit} onCancel={vi.fn()} />);

    await userEvent.click(screen.getByRole("button", { name: "Create Blog" }));

    expect(await screen.findByText("Short description is required")).toBeInTheDocument();
    expect(screen.getByText("Long description is required")).toBeInTheDocument();
    expect(screen.getByText("At least one tag is required")).toBeInTheDocument();
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("requires an image for a new blog", async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(<BlogForm onSubmit={onSubmit} onCancel={vi.fn()} />);

    await user.type(screen.getByPlaceholderText(/brief summary/), "Short");
    await user.type(screen.getByPlaceholderText(/full blog post/), "Long");
    await user.type(screen.getByPlaceholderText("Add a tag..."), "tag{Enter}");
    await user.click(screen.getByRole("button", { name: "Create Blog" }));

    expect(await screen.findByText("Image is required for new blogs")).toBeInTheDocument();
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("submits an edited blog without a new image", async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<BlogForm blog={blog} onSubmit={onSubmit} onCancel={vi.fn()} />);

    await userEvent.click(screen.getByRole("button", { name: "Update Blog" }));

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith({
        shortDescription: blog.shortDescription,
        longDescription: blog.longDescription,
        tags: blog.tags,
        image: null,
      }),
    );
  });

  it("calls onCancel and disables submit while submitting", async () => {
    const onCancel = vi.fn();
    render(<BlogForm blog={blog} onSubmit={vi.fn()} onCancel={onCancel} isSubmitting />);

    await userEvent.click(screen.getByRole("button", { name: "Cancel" }));

    expect(onCancel).toHaveBeenCalled();
    expect(screen.getByRole("button", { name: "Update Blog" })).toBeDisabled();
  });
});
