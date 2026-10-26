import { describe, expect, it } from "vitest";
import { cn, imageOrNone } from "./utils";

describe("cn", () => {
  it("joins class names and drops falsy values", () => {
    expect(cn("a", false, undefined, "b")).toBe("a b");
  });

  it("lets the last conflicting Tailwind class win", () => {
    expect(cn("p-2", "p-4")).toBe("p-4");
  });
});

describe("imageOrNone", () => {
  it("returns the image when there is one", () => {
    expect(imageOrNone("https://images.test/a.png")).toBe("https://images.test/a.png");
  });

  it.each([undefined, null, "", "   ", "/placeholder.svg"])("treats %j as no image", (image) => {
    expect(imageOrNone(image)).toBeUndefined();
  });
});
