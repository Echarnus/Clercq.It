import { render, screen } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { project } from "@/test/msw/fixtures";
import { server } from "@/test/msw/server";
import { Footer } from "./footer";

describe("Footer", () => {
  it("lists featured projects above a link to all projects", async () => {
    server.use(http.get("*/version.json", () => new HttpResponse(null, { status: 404 })));
    render(<Footer />);

    expect(screen.getByRole("link", { name: "View all projects" })).toHaveAttribute(
      "href",
      "/portfolio",
    );
    expect(await screen.findByRole("link", { name: project.title })).toHaveAttribute(
      "href",
      `/portfolio/${project.id}`,
    );
    expect(screen.getByRole("link", { name: "View all projects" })).toBeInTheDocument();
  });
});
