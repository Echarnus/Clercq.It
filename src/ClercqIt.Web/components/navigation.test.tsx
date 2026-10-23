import { render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { getActiveNavKey, Navigation } from "./navigation";

const pathname = vi.hoisted(() => ({ current: "/" }));

vi.mock("next/navigation", () => ({ usePathname: () => pathname.current }));

describe("getActiveNavKey", () => {
  it.each([
    ["/", "home"],
    ["/portfolio", "portfolio"],
    ["/portfolio/11111111-1111-4111-8111-111111111111", "portfolio"],
    ["/blogs/some-post", "blogs"],
    ["/certifications", "certifications"],
  ])("highlights the section for %s", (path, key) => {
    expect(getActiveNavKey(path)).toBe(key);
  });

  it.each([["/unknown"], ["/blogsx"], [null]])("highlights nothing for %s", (path) => {
    expect(getActiveNavKey(path)).toBeNull();
  });
});

describe("Navigation", () => {
  it("marks the section of a detail page as the current page", () => {
    pathname.current = "/blogs/some-post";
    render(<Navigation />);

    const desktopNav = screen.getAllByRole("navigation")[0];
    expect(within(desktopNav).getByRole("link", { name: "Blogs" })).toHaveAttribute(
      "aria-current",
      "page",
    );
    expect(within(desktopNav).getByRole("link", { name: "Home" })).not.toHaveAttribute(
      "aria-current",
    );
  });
});
