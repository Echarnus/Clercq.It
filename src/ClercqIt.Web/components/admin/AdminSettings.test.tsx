import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import * as api from "@/lib/api/identityProviderApi";
import { AdminSettings } from "./AdminSettings";

vi.mock("@/lib/api/identityProviderApi", async (importOriginal) => {
  const actual = await importOriginal<typeof api>();
  return { ...actual, fetchIdentityProvider: vi.fn() };
});

const setup: api.IdentityProvider = {
  provider: "Cloud IAM (hosted Keycloak)",
  baseUrl: "https://lemur-16.cloud-iam.com/auth",
  realm: "clercqit",
  authority: "https://lemur-16.cloud-iam.com/auth/realms/clercqit",
  adminConsoleUrl: "https://lemur-16.cloud-iam.com/auth/admin/clercqit/console/",
  accountConsoleUrl: "https://lemur-16.cloud-iam.com/auth/realms/clercqit/account/",
  providerConsoleUrl: "https://www.cloud-iam.com",
  webClientId: "clercqit-web",
  mcpClientId: "clercqit-mcp",
  roles: [
    { name: "Admin.View", description: "Open the admin dashboard" },
    { name: "MyZen.Owner", description: "Use the owner's MyZen account" },
  ],
};

const context = { username: "kenneth", roles: ["Admin.View"], nav: [] };

describe("AdminSettings", () => {
  it("shows the Cloud IAM setup with links to its consoles", async () => {
    vi.mocked(api.fetchIdentityProvider).mockResolvedValue(setup);

    render(<AdminSettings {...context} />);

    const console = await screen.findByRole("link", { name: /Keycloak admin console/ });
    expect(console).toHaveAttribute("href", setup.adminConsoleUrl);
    expect(console).toHaveAttribute("target", "_blank");
    expect(screen.getByRole("link", { name: /Open Cloud IAM/ })).toHaveAttribute(
      "href",
      "https://www.cloud-iam.com",
    );
    expect(screen.getByText(setup.authority)).toBeInTheDocument();
    expect(screen.getByText("clercqit-web")).toBeInTheDocument();
    expect(screen.getByText("clercqit-mcp")).toBeInTheDocument();
    expect(screen.getByText("MyZen.Owner")).toBeInTheDocument();
    expect(screen.getByText("kenneth")).toBeInTheDocument();
  });

  it("leaves out the Cloud IAM link for a local Keycloak", async () => {
    vi.mocked(api.fetchIdentityProvider).mockResolvedValue({ ...setup, providerConsoleUrl: null });

    render(<AdminSettings {...context} />);

    await screen.findByRole("link", { name: /Keycloak admin console/ });
    expect(screen.queryByRole("link", { name: /Open Cloud IAM/ })).not.toBeInTheDocument();
  });

  it("shows an error when the setup cannot be loaded", async () => {
    vi.mocked(api.fetchIdentityProvider).mockRejectedValue(
      new Error("Identity provider request failed (403)"),
    );

    render(<AdminSettings {...context} />);

    expect(await screen.findByText("Identity provider request failed (403)")).toBeInTheDocument();
  });
});
