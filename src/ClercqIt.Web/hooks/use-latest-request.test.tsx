import { renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { isAbortError, useLatestRequest } from "./use-latest-request";

describe("useLatestRequest", () => {
  it("aborts the previous signal when a new one is taken", () => {
    const { result } = renderHook(() => useLatestRequest());
    const first = result.current();
    const second = result.current();
    expect(first.aborted).toBe(true);
    expect(second.aborted).toBe(false);
  });

  it("aborts the last signal on unmount", () => {
    const { result, unmount } = renderHook(() => useLatestRequest());
    const signal = result.current();
    unmount();
    expect(signal.aborted).toBe(true);
  });

  it("returns the same function on every render", () => {
    const { result, rerender } = renderHook(() => useLatestRequest());
    const before = result.current;
    rerender();
    expect(result.current).toBe(before);
  });
});

describe("isAbortError", () => {
  it("recognises abort and timeout errors only", () => {
    expect(isAbortError(new DOMException("aborted", "AbortError"))).toBe(true);
    expect(isAbortError(new DOMException("timed out", "TimeoutError"))).toBe(true);
    expect(isAbortError(new Error("boom"))).toBe(false);
    expect(isAbortError("AbortError")).toBe(false);
  });
});
