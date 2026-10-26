import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { project } from "@/test/msw/fixtures";
import { metadata } from "../layout";
import HomePage from "./page";

const fetchFeaturedProjects = vi.hoisted(() => vi.fn(() => Promise.resolve<unknown[]>([])));
vi.mock("../portfolio/lib/api", () => ({ fetchFeaturedProjects }));

describe("HomePage", () => {
  it("introduces Kenneth as developer and architect", async () => {
    render(await HomePage());

    expect(screen.getByRole("heading", { level: 1 })).toHaveTextContent("Developer & Architect");
    expect(screen.queryByText(/full stack developer/i)).not.toBeInTheDocument();
  });

  it("uses the same title in the site description", () => {
    expect(metadata.description).toMatch(/^Developer and Solution Architect/);
  });

  it("shows the company as a subtitle on a featured project", async () => {
    fetchFeaturedProjects.mockResolvedValueOnce([project]);

    render(await HomePage());

    expect(screen.getByText("Portfolio site")).toBeInTheDocument();
    expect(screen.getByText("Clercq.It")).toBeInTheDocument();
  });

  it("shows no subtitle when a project has no company", async () => {
    fetchFeaturedProjects.mockResolvedValueOnce([{ ...project, company: "" }]);

    render(await HomePage());

    expect(screen.getByText("Portfolio site")).toBeInTheDocument();
    expect(screen.queryByText("Clercq.It")).not.toBeInTheDocument();
  });
});
