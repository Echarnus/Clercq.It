import { useCallback, useEffect, useRef } from "react";

/** True for the error `fetch` throws when its signal aborts the request. */
export function isAbortError(error: unknown): boolean {
  return (
    (error instanceof DOMException || error instanceof Error) &&
    (error.name === "AbortError" || error.name === "TimeoutError")
  );
}

/**
 * Hands out an `AbortSignal` per load and aborts the previous one, so only the
 * newest request of a component can land (issue #490). The last signal is also
 * aborted on unmount. Check `signal.aborted` before setting state from a result.
 */
export function useLatestRequest(): () => AbortSignal {
  const current = useRef<AbortController | null>(null);

  useEffect(
    () => () => {
      current.current?.abort();
    },
    [],
  );

  return useCallback(() => {
    current.current?.abort();
    const controller = new AbortController();
    current.current = controller;
    return controller.signal;
  }, []);
}
