import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ThemeToggle, toThemeChoice } from "./theme-toggle";

const themeState = vi.hoisted(() => ({ theme: "system", setTheme: vi.fn() }));

vi.mock("next-themes", () => ({ useTheme: () => themeState }));

describe("toThemeChoice", () => {
  it.each([
    ["light", "light"],
    ["dark", "dark"],
    ["system", "system"],
    [undefined, "system"],
    ["sepia", "system"],
  ])("maps %s to %s", (theme, choice) => {
    expect(toThemeChoice(theme)).toBe(choice);
  });
});

describe("ThemeToggle", () => {
  it("names the current theme on the button", () => {
    themeState.theme = "dark";
    render(<ThemeToggle />);

    expect(
      screen.getByRole("button", { name: "Choose theme (current: Dark)" }),
    ).toBeInTheDocument();
  });

  it("offers Light, Dark and System and checks the current one", async () => {
    themeState.theme = "system";
    const user = userEvent.setup();
    render(<ThemeToggle />);

    await user.click(screen.getByRole("button", { name: /choose theme/i }));

    const items = screen.getAllByRole("menuitemradio");
    expect(items.map((item) => item.textContent)).toEqual(["Light", "Dark", "System"]);
    expect(screen.getByRole("menuitemradio", { name: "System" })).toHaveAttribute(
      "aria-checked",
      "true",
    );
  });

  it("sets the theme that is picked", async () => {
    themeState.theme = "light";
    themeState.setTheme.mockClear();
    const user = userEvent.setup();
    render(<ThemeToggle />);

    await user.click(screen.getByRole("button", { name: /choose theme/i }));
    await user.click(screen.getByRole("menuitemradio", { name: "System" }));

    expect(themeState.setTheme).toHaveBeenCalledWith("system");
  });
});
