import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Markdown } from "./markdown";

describe("Markdown", () => {
  it("renders headings, emphasis, lists and code", () => {
    const { container } = render(
      <Markdown>{"## Goals\n\nSome **bold** text.\n\n- one\n- two\n\n`code`"}</Markdown>,
    );

    expect(screen.getByRole("heading", { level: 2, name: "Goals" })).toBeInTheDocument();
    expect(screen.getByText("bold").tagName).toBe("STRONG");
    expect(screen.getAllByRole("listitem")).toHaveLength(2);
    expect(container.querySelector("code")).toHaveTextContent("code");
  });

  it("renders GitHub Flavored Markdown tables", () => {
    render(<Markdown>{"| a | b |\n| - | - |\n| 1 | 2 |"}</Markdown>);

    expect(screen.getByRole("table")).toBeInTheDocument();
  });

  it("opens external links in a new tab and keeps internal links in place", () => {
    render(<Markdown>{"[site](https://github.com/Echarnus) and [home](/portfolio)"}</Markdown>);

    expect(screen.getByRole("link", { name: "site" })).toHaveAttribute("target", "_blank");
    expect(screen.getByRole("link", { name: "site" })).toHaveAttribute(
      "rel",
      "noopener noreferrer",
    );
    expect(screen.getByRole("link", { name: "home" })).not.toHaveAttribute("target");
  });

  it("does not render raw HTML", () => {
    const { container } = render(
      <Markdown>{'<script>alert("x")</script><img src=x onerror="alert(1)">'}</Markdown>,
    );

    expect(container.querySelector("script")).toBeNull();
    expect(container.querySelector("img")).toBeNull();
  });
});
