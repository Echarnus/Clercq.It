import { renderHook } from "@testing-library/react";
import { renderToString } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { useIsClient } from "./use-is-client";

function Probe() {
  return <span>{useIsClient() ? "client" : "server"}</span>;
}

describe("useIsClient", () => {
  it("is true once rendering in the browser", () => {
    const { result } = renderHook(() => useIsClient());
    expect(result.current).toBe(true);
  });

  it("is false during server rendering", () => {
    expect(renderToString(<Probe />)).toContain("server");
  });
});
