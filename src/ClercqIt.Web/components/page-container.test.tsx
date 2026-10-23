import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { PageContainer } from "./page-container";

describe("PageContainer", () => {
  it("applies the one site width and gutter", () => {
    render(<PageContainer>content</PageContainer>);

    const container = screen.getByText("content");
    expect(container.tagName).toBe("DIV");
    expect(container).toHaveClass("mx-auto", "max-w-6xl", "px-4", "sm:px-6", "lg:px-8");
  });

  it("renders the requested element and merges extra classes", () => {
    render(
      <PageContainer as="section" className="py-16">
        section
      </PageContainer>,
    );

    const container = screen.getByText("section");
    expect(container.tagName).toBe("SECTION");
    expect(container).toHaveClass("max-w-6xl", "py-16");
  });
});
