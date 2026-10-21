import { act, renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useIsMobile } from "./use-mobile";

function mockMatchMedia() {
  const listeners = new Set<() => void>();
  vi.stubGlobal(
    "matchMedia",
    vi.fn(() => ({
      addEventListener: (_: string, l: () => void) => listeners.add(l),
      removeEventListener: (_: string, l: () => void) => listeners.delete(l),
    })),
  );
  return listeners;
}

describe("useIsMobile", () => {
  it("follows the window width across the 768px breakpoint", () => {
    const listeners = mockMatchMedia();
    window.innerWidth = 1024;
    const { result, unmount } = renderHook(() => useIsMobile());
    expect(result.current).toBe(false);

    act(() => {
      window.innerWidth = 500;
      listeners.forEach((l) => l());
    });
    expect(result.current).toBe(true);

    unmount();
    expect(listeners.size).toBe(0);
    vi.unstubAllGlobals();
  });
});
