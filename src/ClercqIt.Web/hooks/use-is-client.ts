import { useSyncExternalStore } from "react";

const subscribe = () => () => {};

/** False during server rendering and hydration, true once running in the browser. */
export function useIsClient() {
  return useSyncExternalStore(
    subscribe,
    () => true,
    () => false,
  );
}
