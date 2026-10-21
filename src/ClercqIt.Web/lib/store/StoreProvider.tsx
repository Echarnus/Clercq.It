"use client";

import { useState } from "react";
import { Provider } from "react-redux";
import { makeStore } from "@/lib/store";

interface StoreProviderProps {
  children: React.ReactNode;
}

export function StoreProvider({ children }: StoreProviderProps) {
  const [store] = useState(makeStore);

  return <Provider store={store}>{children}</Provider>;
}
