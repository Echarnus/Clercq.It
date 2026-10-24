import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import * as api from "@/lib/api/myzenApi";
import { formatDuration, MyZenEntryForm, parseDuration } from "./MyZenEntryForm";

vi.mock("@/lib/api/myzenApi", async (importOriginal) => {
  const actual = await importOriginal<typeof api>();
  return { ...actual, searchMyZenProjects: vi.fn() };
});

describe("parseDuration", () => {
  it.each([
    ["8:00", 480],
    ["7:30", 450],
    [" 0:45 ", 45],
    ["7.5", 450],
    ["7,25", 435],
    ["24:00", 1440],
  ])("reads %s as %i minutes", (text, minutes) => {
    expect(parseDuration(text)).toBe(minutes);
  });

  it.each(["", "0:00", "24:01", "7:60", "abc", "1.2.3", "25"])("rejects %s", (text) => {
    expect(parseDuration(text)).toBeNull();
  });
});

describe("formatDuration", () => {
  it("drops the seconds and the leading zero", () => {
    expect(formatDuration("07:30:00")).toBe("7:30");
    expect(formatDuration("10:05")).toBe("10:05");
  });
});

describe("MyZenEntryForm", () => {
  it("needs a project and a valid duration before it can be added", async () => {
    const user = userEvent.setup();
    vi.mocked(api.searchMyZenProjects).mockResolvedValue([
      { id: 23200, name: "Vakantie", customerId: null, customer: "" },
    ]);
    const onSave = vi.fn(() => Promise.resolve());
    render(<MyZenEntryForm defaultDate="2026-10-01" onSave={onSave} onCancel={vi.fn()} />);

    const add = screen.getByRole("button", { name: "Add entry" });
    expect(add).toBeDisabled();
    await user.type(screen.getByLabelText("Project"), "va");
    await user.click(await screen.findByRole("button", { name: "Vakantie" }));
    expect(add).toBeEnabled();

    const duration = screen.getByLabelText("Duration (h:mm)");
    await user.clear(duration);
    await user.type(duration, "nope");
    expect(add).toBeDisabled();
    await user.clear(duration);
    await user.type(duration, "4:00");
    await user.click(screen.getByLabelText("Invoiced"));
    await user.click(add);

    expect(onSave).toHaveBeenCalledWith({
      date: "2026-10-01",
      durationMinutes: 240,
      projectId: 23200,
      customerId: null,
      description: "",
      invoiced: true,
    });
  });

  it("searches only from two letters and lets the project be changed", async () => {
    const user = userEvent.setup();
    vi.mocked(api.searchMyZenProjects).mockClear();
    render(
      <MyZenEntryForm
        entry={{
          id: 7,
          date: "2026-10-02",
          description: "Work",
          projectId: 5,
          project: "Website",
          customerId: 9,
          customer: "Acme",
          duration: "08:00:00",
          invoiced: false,
        }}
        defaultDate="2026-10-01"
        onSave={vi.fn(() => Promise.resolve())}
        onCancel={vi.fn()}
      />,
    );

    expect(screen.getByText("Website")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Change" }));
    await user.type(screen.getByLabelText("Project"), "w");
    await new Promise((resolve) => setTimeout(resolve, 400));
    expect(api.searchMyZenProjects).not.toHaveBeenCalled();
    expect(screen.getByRole("button", { name: "Save changes" })).toBeDisabled();
  });

  it("shows a search failure and a generic save failure", async () => {
    const user = userEvent.setup();
    vi.mocked(api.searchMyZenProjects).mockRejectedValue(new Error("502"));
    const onCancel = vi.fn();
    render(
      <MyZenEntryForm
        entry={{
          id: 7,
          date: "2026-10-02",
          description: "Work",
          projectId: 5,
          project: "Website",
          customerId: 9,
          customer: "Acme",
          duration: "08:00:00",
          invoiced: false,
        }}
        defaultDate="2026-10-01"
        onSave={() => Promise.reject(new Error("502"))}
        onCancel={onCancel}
      />,
    );

    await user.click(screen.getByRole("button", { name: "Save changes" }));
    expect(await screen.findByText("MyZen did not save the entry.")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Change" }));
    await user.type(screen.getByLabelText("Project"), "web");
    await waitFor(() =>
      expect(screen.getByText("Could not search MyZen's projects.")).toBeInTheDocument(),
    );
    await user.click(screen.getByRole("button", { name: "Cancel" }));
    expect(onCancel).toHaveBeenCalled();
  });
});
