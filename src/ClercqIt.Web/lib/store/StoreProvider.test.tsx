import { render, screen } from "@testing-library/react";
import { useStore } from "react-redux";
import { describe, expect, it } from "vitest";
import { StoreProvider } from "./StoreProvider";

function StoreProbe() {
  const store = useStore();
  return <span>{typeof store.getState === "function" ? "has store" : "no store"}</span>;
}

describe("StoreProvider", () => {
  it("gives its children a Redux store", () => {
    render(
      <StoreProvider>
        <StoreProbe />
      </StoreProvider>,
    );
    expect(screen.getByText("has store")).toBeInTheDocument();
  });
});
