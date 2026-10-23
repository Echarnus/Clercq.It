import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as api from "@/lib/api/myzenApi";
import { MyZenPanel, monthOf } from "./MyZenPanel";

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
    expect(screen.getByText("07:30")).toBeInTheDocument();
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
      expect(screen.getByText("Could not load the time entries.")).toBeInTheDocument(),
    );
  });
});
