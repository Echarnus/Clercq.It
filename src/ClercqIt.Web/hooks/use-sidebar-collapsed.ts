import { useCallback, useSyncExternalStore } from "react";

// Whether the admin sidebar is folded to an icon rail (issue #449). Kept in
// localStorage so the choice survives a reload. When storage is blocked, the
// choice lives in memory until the next reload.
export const SIDEBAR_COLLAPSED_KEY = "admin-sidebar-collapsed";

const listeners = new Set<() => void>();
let inMemory = false;

function subscribe(onChange: () => void) {
  listeners.add(onChange);
  return () => {
    listeners.delete(onChange);
  };
}

function read(): boolean {
  try {
    const stored = window.localStorage.getItem(SIDEBAR_COLLAPSED_KEY);
    return stored === null ? inMemory : stored === "true";
  } catch {
    return inMemory;
  }
}

export function useSidebarCollapsed(): [boolean, (collapsed: boolean) => void] {
  const collapsed = useSyncExternalStore(subscribe, read, () => false);
  const setCollapsed = useCallback((value: boolean) => {
    inMemory = value;
    try {
      window.localStorage.setItem(SIDEBAR_COLLAPSED_KEY, String(value));
    } catch {
      // Blocked storage: keep the in-memory value only.
    }
    listeners.forEach((listener) => listener());
  }, []);
  return [collapsed, setCollapsed];
}
