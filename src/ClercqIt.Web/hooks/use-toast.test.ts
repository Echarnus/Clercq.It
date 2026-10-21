import { act, renderHook } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { reducer, toast, useToast } from "./use-toast";

// Ids differ from the ones toast() generates: dismissing queues a module-level removal timer per id.
const base = { id: "r1", title: "Saved", open: true };

describe("toast reducer", () => {
  it("adds, updates, dismisses and removes toasts", () => {
    let state = reducer({ toasts: [] }, { type: "ADD_TOAST", toast: base });
    expect(state.toasts).toHaveLength(1);

    state = reducer(state, { type: "UPDATE_TOAST", toast: { id: "r1", title: "Updated" } });
    expect(state.toasts[0].title).toBe("Updated");

    state = reducer(state, { type: "DISMISS_TOAST", toastId: "r1" });
    expect(state.toasts[0].open).toBe(false);

    state = reducer(state, { type: "REMOVE_TOAST", toastId: "r1" });
    expect(state.toasts).toHaveLength(0);
  });

  it("dismisses and removes every toast without an id", () => {
    const state = { toasts: [base, { ...base, id: "r2" }] };
    expect(reducer(state, { type: "DISMISS_TOAST" }).toasts.every((t) => !t.open)).toBe(true);
    expect(reducer(state, { type: "REMOVE_TOAST" }).toasts).toEqual([]);
  });

  it("keeps only the newest toast", () => {
    const state = reducer({ toasts: [base] }, { type: "ADD_TOAST", toast: { ...base, id: "r2" } });
    expect(state.toasts.map((t) => t.id)).toEqual(["r2"]);
  });
});

describe("useToast", () => {
  afterEach(() => vi.useRealTimers());

  it("shows, updates and dismisses a toast", () => {
    vi.useFakeTimers();
    const { result } = renderHook(() => useToast());

    let handle: ReturnType<typeof toast>;
    act(() => {
      handle = toast({ title: "Hello" });
    });
    expect(result.current.toasts[0].title).toBe("Hello");

    act(() => handle.update({ id: handle.id, title: "Changed" }));
    expect(result.current.toasts[0].title).toBe("Changed");

    act(() => result.current.dismiss(handle.id));
    expect(result.current.toasts[0].open).toBe(false);

    act(() => {
      vi.runAllTimers();
    });
    expect(result.current.toasts).toHaveLength(0);
  });
});
