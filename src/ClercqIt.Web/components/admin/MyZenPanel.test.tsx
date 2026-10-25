import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as api from "@/lib/api/myzenApi";
import { MyZenPanel, monthOf, shiftMonth } from "./MyZenPanel";

vi.mock("@/lib/api/myzenApi", async (importOriginal) => {
  const actual = await importOriginal<typeof api>();
  return {
    ...actual,
    fetchMyZenStatus: vi.fn(),
    startMyZenLogin: vi.fn(),
    submitMyZenLogin: vi.fn(),
    submitMyZenMfaCode: vi.fn(),
    disconnectMyZen: vi.fn(),
    fetchMyZenTimeEntries: vi.fn(),
    fetchMyZenTimeEntry: vi.fn(),
    searchMyZenProjects: vi.fn(),
    createMyZenTimeEntry: vi.fn(),
    updateMyZenTimeEntry: vi.fn(),
    deleteMyZenTimeEntry: vi.fn(),
  };
});

const disconnected: api.MyZenStatus = {
  state: "Disconnected",
  connectedAt: null,
  lastSeenAlive: null,
};
const connected: api.MyZenStatus = {
  state: "Connected",
  connectedAt: "2026-10-23T20:00:00Z",
  lastSeenAlive: null,
};
const entry: api.MyZenTimeEntry = {
  id: 7,
  date: "2026-10-01",
  description: "Sprint review",
  projectId: null,
  project: "Website",
  customerId: null,
  customer: "Acme",
  duration: "07:30:00",
  invoiced: true,
};
const today = new Date(2026, 9, 23);

describe("monthOf", () => {
  it("returns the first and last day of the month", () => {
    expect(monthOf(new Date(2026, 1, 10))).toEqual({ from: "2026-02-01", to: "2026-02-28" });
    expect(monthOf(today)).toEqual({ from: "2026-10-01", to: "2026-10-31" });
  });
});

describe("shiftMonth", () => {
  it("moves across year ends", () => {
    expect(shiftMonth("2026-12-01", 1)).toEqual({ from: "2027-01-01", to: "2027-01-31" });
    expect(shiftMonth("2026-01-01", -1)).toEqual({ from: "2025-12-01", to: "2025-12-31" });
  });
});

describe("MyZenPanel", () => {
  beforeEach(() => {
    vi.mocked(api.fetchMyZenTimeEntries).mockResolvedValue([entry]);
  });

  it("connects with the CAPTCHA and the MFA code, then shows this month's entries", async () => {
    const user = userEvent.setup();
    vi.mocked(api.fetchMyZenStatus)
      .mockResolvedValueOnce(disconnected)
      .mockResolvedValue(connected);
    vi.mocked(api.startMyZenLogin).mockResolvedValue({
      step: "Credentials",
      captchaImage: "data:image/png;base64,AA==",
      message: null,
    });
    vi.mocked(api.submitMyZenLogin).mockResolvedValue({
      step: "Mfa",
      captchaImage: null,
      message: null,
    });
    vi.mocked(api.submitMyZenMfaCode).mockResolvedValue({
      step: "Connected",
      captchaImage: null,
      message: null,
    });
    render(<MyZenPanel today={today} />);

    await user.click(await screen.findByRole("button", { name: "Connect MyZen" }));
    expect(await screen.findByAltText("MyZen CAPTCHA")).toHaveAttribute(
      "src",
      "data:image/png;base64,AA==",
    );
    await user.type(screen.getByLabelText("CAPTCHA"), "ab12");
    await user.click(screen.getByRole("button", { name: "Sign in to MyZen" }));

    const verify = await screen.findByRole("button", { name: "Verify" });
    expect(verify).toBeDisabled();
    await user.type(screen.getByLabelText("Code from your authenticator app"), "123456");
    await user.click(verify);

    expect(await screen.findByText("Sprint review")).toBeInTheDocument();
    expect(screen.getByText("7:30")).toBeInTheDocument();
    expect(api.submitMyZenLogin).toHaveBeenCalledWith("ab12");
    expect(api.submitMyZenMfaCode).toHaveBeenCalledWith("123456");
    expect(api.fetchMyZenTimeEntries).toHaveBeenCalledWith("2026-10-01", "2026-10-31");
  });

  it("offers a reconnect when expired and shows MyZen's message without a CAPTCHA", async () => {
    const user = userEvent.setup();
    vi.mocked(api.fetchMyZenStatus).mockResolvedValue({ ...disconnected, state: "Expired" });
    vi.mocked(api.startMyZenLogin).mockResolvedValue({
      step: "Credentials",
      captchaImage: null,
      message: "Wrong password",
    });
    render(<MyZenPanel today={today} />);

    await user.click(await screen.findByRole("button", { name: "Reconnect MyZen" }));

    expect(await screen.findByText("Wrong password")).toBeInTheDocument();
    expect(screen.getByText(/MyZen shows no CAPTCHA/)).toBeInTheDocument();
  });

  it("shows an error when a step fails", async () => {
    const user = userEvent.setup();
    vi.mocked(api.fetchMyZenStatus).mockResolvedValue(disconnected);
    vi.mocked(api.startMyZenLogin).mockRejectedValue(new Error("502"));
    render(<MyZenPanel today={today} />);

    await user.click(await screen.findByRole("button", { name: "Connect MyZen" }));

    expect(await screen.findByText("That did not work. Try again.")).toBeInTheDocument();
  });

  it("shows when MyZen is not available", async () => {
    vi.mocked(api.fetchMyZenStatus).mockRejectedValue(new Error("404"));
    render(<MyZenPanel today={today} />);

    expect(await screen.findByText("MyZen is not available right now.")).toBeInTheDocument();
  });

  it("disconnects a connected session", async () => {
    const user = userEvent.setup();
    vi.mocked(api.fetchMyZenStatus)
      .mockResolvedValueOnce(connected)
      .mockResolvedValue(disconnected);
    vi.mocked(api.fetchMyZenTimeEntries).mockResolvedValue([]);
    render(<MyZenPanel today={today} />);

    expect(await screen.findByText("No time entries this month.")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Disconnect" }));

    expect(await screen.findByRole("button", { name: "Connect MyZen" })).toBeInTheDocument();
    expect(api.disconnectMyZen).toHaveBeenCalled();
  });

  it("re-reads the state when the session expired, and reports other load errors", async () => {
    vi.mocked(api.fetchMyZenStatus)
      .mockResolvedValueOnce(connected)
      .mockResolvedValue({ ...disconnected, state: "Expired" });
    vi.mocked(api.fetchMyZenTimeEntries).mockRejectedValue(new api.MyZenNotConnectedError());
    const { unmount } = render(<MyZenPanel />);
    expect(await screen.findByRole("button", { name: "Reconnect MyZen" })).toBeInTheDocument();
    unmount();

    vi.mocked(api.fetchMyZenStatus).mockResolvedValue(connected);
    vi.mocked(api.fetchMyZenTimeEntries).mockRejectedValue(new Error("502"));
    render(<MyZenPanel today={today} />);
    await waitFor(() =>
      expect(
        screen.getByText("Could not load the time entries. Try again in a moment."),
      ).toBeInTheDocument(),
    );
  });

  it("shows the API's reason when the entries do not load, and clears it once a month loads", async () => {
    const user = userEvent.setup();
    vi.mocked(api.fetchMyZenStatus).mockResolvedValue(connected);
    vi.mocked(api.fetchMyZenTimeEntries)
      .mockRejectedValueOnce(
        new api.MyZenRequestError("MyZen's time registration page has no request token.", 502),
      )
      .mockResolvedValue([entry]);
    render(<MyZenPanel today={today} />);

    expect(
      await screen.findByText(
        "Could not load the time entries. MyZen's time registration page has no request token.",
      ),
    ).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Previous month" }));

    expect(await screen.findByText("Sprint review")).toBeInTheDocument();
    expect(screen.queryByText(/Could not load the time entries/)).not.toBeInTheDocument();
  });

  describe("when connected", () => {
    beforeEach(() => {
      vi.mocked(api.fetchMyZenStatus).mockResolvedValue(connected);
    });

    it("moves between months", async () => {
      const user = userEvent.setup();
      render(<MyZenPanel today={today} />);

      await screen.findByText("Sprint review");
      await user.click(screen.getByRole("button", { name: "Next month" }));
      await waitFor(() =>
        expect(api.fetchMyZenTimeEntries).toHaveBeenCalledWith("2026-11-01", "2026-11-30"),
      );
      await user.click(screen.getByRole("button", { name: "Previous month" }));
      await user.click(screen.getByRole("button", { name: "Previous month" }));
      await waitFor(() =>
        expect(api.fetchMyZenTimeEntries).toHaveBeenCalledWith("2026-09-01", "2026-09-30"),
      );
    });

    it("adds an entry and reloads the month", async () => {
      const user = userEvent.setup();
      vi.mocked(api.searchMyZenProjects).mockResolvedValue([
        { id: 5, name: "Website", customerId: 9, customer: "Acme" },
      ]);
      vi.mocked(api.createMyZenTimeEntry).mockResolvedValue();
      render(<MyZenPanel today={today} />);

      await user.click(await screen.findByRole("button", { name: "Add entry" }));
      const form = screen.getByRole("form", { name: "New time entry" });
      await user.type(within(form).getByLabelText("Project"), "web");
      await user.click(await within(form).findByRole("button", { name: /Website/ }));
      await user.type(within(form).getByLabelText("Description"), "Review");
      await user.click(within(form).getByRole("button", { name: "Add entry" }));

      await waitFor(() =>
        expect(api.createMyZenTimeEntry).toHaveBeenCalledWith({
          date: "2026-10-01",
          durationMinutes: 480,
          projectId: 5,
          customerId: 9,
          description: "Review",
          invoiced: false,
        }),
      );
      await waitFor(() =>
        expect(screen.queryByRole("form", { name: "New time entry" })).not.toBeInTheDocument(),
      );
      expect(api.fetchMyZenTimeEntries).toHaveBeenCalledTimes(2);
    });

    it("edits an entry read on its own", async () => {
      const user = userEvent.setup();
      vi.mocked(api.fetchMyZenTimeEntry).mockResolvedValue({
        ...entry,
        projectId: 5,
        customerId: 9,
      });
      vi.mocked(api.updateMyZenTimeEntry).mockResolvedValue();
      render(<MyZenPanel today={today} />);

      await user.click(await screen.findByRole("button", { name: "Edit entry of 2026-10-01" }));
      const form = await screen.findByRole("form", { name: "Edit time entry" });
      const duration = within(form).getByLabelText("Duration (h:mm)");
      await user.clear(duration);
      await user.type(duration, "6,5");
      await user.click(within(form).getByRole("button", { name: "Save changes" }));

      await waitFor(() =>
        expect(api.updateMyZenTimeEntry).toHaveBeenCalledWith(7, {
          date: "2026-10-01",
          durationMinutes: 390,
          projectId: 5,
          customerId: 9,
          description: "Sprint review",
          invoiced: true,
        }),
      );
    });

    it("shows MyZen's reason when it refuses a change", async () => {
      const user = userEvent.setup();
      vi.mocked(api.fetchMyZenTimeEntry).mockResolvedValue({ ...entry, projectId: 5 });
      vi.mocked(api.updateMyZenTimeEntry).mockRejectedValue(
        new api.MyZenRejectedError("Project is closed"),
      );
      render(<MyZenPanel today={today} />);

      await user.click(await screen.findByRole("button", { name: "Edit entry of 2026-10-01" }));
      await user.click(await screen.findByRole("button", { name: "Save changes" }));

      expect(await screen.findByText("Project is closed")).toBeInTheDocument();
    });

    it("deletes an entry only after confirming", async () => {
      const user = userEvent.setup();
      vi.mocked(api.deleteMyZenTimeEntry).mockResolvedValue();
      render(<MyZenPanel today={today} />);

      await user.click(await screen.findByRole("button", { name: "Delete entry of 2026-10-01" }));
      expect(await screen.findByText("Delete this time entry?")).toBeInTheDocument();
      await user.click(screen.getByRole("button", { name: "Cancel" }));
      expect(api.deleteMyZenTimeEntry).not.toHaveBeenCalled();

      await user.click(screen.getByRole("button", { name: "Delete entry of 2026-10-01" }));
      await user.click(await screen.findByRole("button", { name: "Delete" }));

      await waitFor(() => expect(api.deleteMyZenTimeEntry).toHaveBeenCalledWith(7));
      await waitFor(() => expect(api.fetchMyZenTimeEntries).toHaveBeenCalledTimes(2));
    });

    it("offers a reconnect when the session expired during a change", async () => {
      const user = userEvent.setup();
      vi.mocked(api.fetchMyZenStatus)
        .mockReset()
        .mockResolvedValueOnce(connected)
        .mockResolvedValue({ ...disconnected, state: "Expired" });
      vi.mocked(api.deleteMyZenTimeEntry).mockRejectedValue(new api.MyZenNotConnectedError());
      render(<MyZenPanel today={today} />);

      await user.click(await screen.findByRole("button", { name: "Delete entry of 2026-10-01" }));
      await user.click(await screen.findByRole("button", { name: "Delete" }));

      expect(await screen.findByRole("button", { name: "Reconnect MyZen" })).toBeInTheDocument();
    });

    it("reports an entry that cannot be opened", async () => {
      const user = userEvent.setup();
      vi.mocked(api.fetchMyZenTimeEntry).mockRejectedValue(new Error("502"));
      render(<MyZenPanel today={today} />);

      await user.click(await screen.findByRole("button", { name: "Edit entry of 2026-10-01" }));

      expect(
        await screen.findByText("Could not open the time entry. Try again in a moment."),
      ).toBeInTheDocument();
    });
  });
});
