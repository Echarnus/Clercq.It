import { cleanup, render } from "@testing-library/react";
import fc from "fast-check";
import { afterEach, describe, expect, it } from "vitest";
import { Markdown } from "./markdown";

// Property-based tests (issue #402): content bodies are Markdown written through
// the admin UI or the content MCP server. Whatever they contain, rendering must
// not produce active markup or script links.

afterEach(cleanup);

const fragment = fc.oneof(
  fc.string(),
  fc.constantFrom(
    "<script>alert(1)</script>",
    '<img src=x onerror="alert(1)">',
    '<iframe src="https://example.com"></iframe>',
    '<a href="javascript:alert(1)">x</a>',
    "[click](javascript:alert(1))",
    "[click](JaVaScRiPt:alert(1))",
    "[click]( javascript:alert(1))",
    "[data](data:text/html;base64,PHNjcmlwdD4=)",
    "![img](javascript:alert(1))",
    '<svg onload="alert(1)">',
    "<style>body{display:none}</style>",
    "\n\n| a | b |\n| - | - |\n| 1 | 2 |\n\n",
    "**bold** _em_ `code`",
    "\n- item\n",
    "<",
    ">",
    "](",
  ),
);

const markdownArb = fc.array(fragment, { maxLength: 8 }).map((parts) => parts.join(""));

const BLOCKED_TAGS = ["script", "iframe", "object", "embed", "style", "svg", "form", "input"];
const SCRIPT_URL = /^\s*(javascript|vbscript|data):/i;

describe("Markdown (property)", () => {
  it("never renders active elements, event handlers or script URLs", () => {
    fc.assert(
      fc.property(markdownArb, (source) => {
        const { container } = render(<Markdown>{source}</Markdown>);

        for (const tag of BLOCKED_TAGS) {
          expect(container.querySelector(tag)).toBeNull();
        }
        for (const element of container.querySelectorAll("*")) {
          for (const attribute of element.attributes) {
            expect(attribute.name.toLowerCase().startsWith("on")).toBe(false);
          }
        }
        for (const element of container.querySelectorAll("[href], [src]")) {
          const url = element.getAttribute("href") ?? element.getAttribute("src") ?? "";
          expect(url).not.toMatch(SCRIPT_URL);
        }
        cleanup();
      }),
      { numRuns: 200 },
    );
  });
});
