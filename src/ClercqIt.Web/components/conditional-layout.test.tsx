import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ConditionalLayout } from "./conditional-layout";

const pathname = vi.hoisted(() => ({ current: "/" }));

vi.mock("next/navigation", () => ({ usePathname: () => pathname.current }));
vi.mock("@/components/header", () => ({ Header: () => <header>site header</header> }));
vi.mock("@/components/footer", () => ({ Footer: () => <footer>site footer</footer> }));

describe("ConditionalLayout", () => {
  it("wraps public pages in the header and footer", () => {
    pathname.current = "/blogs";
    render(<ConditionalLayout>page</ConditionalLayout>);

    expect(screen.getByText("site header")).toBeInTheDocument();
    expect(screen.getByText("site footer")).toBeInTheDocument();
    expect(screen.getByText("page")).toBeInTheDocument();
  });

  it("renders admin pages without the site chrome", () => {
    pathname.current = "/admin/dashboard";
    render(<ConditionalLayout>admin</ConditionalLayout>);

    expect(screen.queryByText("site header")).not.toBeInTheDocument();
    expect(screen.getByText("admin")).toBeInTheDocument();
  });
});
