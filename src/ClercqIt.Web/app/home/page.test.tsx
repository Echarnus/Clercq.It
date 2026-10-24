import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { metadata } from "../layout";
import HomePage from "./page";

vi.mock("../portfolio/lib/api", () => ({ fetchFeaturedProjects: () => Promise.resolve([]) }));

describe("HomePage", () => {
  it("introduces Kenneth as developer and architect", async () => {
    render(await HomePage());

    expect(screen.getByRole("heading", { level: 1 })).toHaveTextContent("Developer & Architect");
    expect(screen.queryByText(/full stack developer/i)).not.toBeInTheDocument();
  });

  it("uses the same title in the site description", () => {
    expect(metadata.description).toMatch(/^Developer and Solution Architect/);
  });
});
