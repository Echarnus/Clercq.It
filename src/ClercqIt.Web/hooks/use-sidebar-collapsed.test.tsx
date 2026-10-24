import { act, renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { SIDEBAR_COLLAPSED_KEY, useSidebarCollapsed } from "./use-sidebar-collapsed";

describe("useSidebarCollapsed", () => {
  it("UseSidebarCollapsed_NothingStored_IsOpen", () => {
    const { result } = renderHook(() => useSidebarCollapsed());
    expect(result.current[0]).toBe(false);
  });

  it("UseSidebarCollapsed_Set_StoresAndUpdatesEveryUser", () => {
    // Arrange: two components read the same setting.
    const first = renderHook(() => useSidebarCollapsed());
    const second = renderHook(() => useSidebarCollapsed());

    // Act
    act(() => first.result.current[1](true));

    // Assert
    expect(window.localStorage.getItem(SIDEBAR_COLLAPSED_KEY)).toBe("true");
    expect(second.result.current[0]).toBe(true);
  });

  it("UseSidebarCollapsed_StorageBlocked_KeepsTheChoiceInMemory", () => {
    // Arrange
    vi.spyOn(Storage.prototype, "getItem").mockImplementation(() => {
      throw new Error("blocked");
    });
    vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => {
      throw new Error("blocked");
    });
    const { result } = renderHook(() => useSidebarCollapsed());

    // Act
    act(() => result.current[1](true));

    // Assert
    expect(result.current[0]).toBe(true);
    act(() => result.current[1](false));
    expect(result.current[0]).toBe(false);
  });
});
