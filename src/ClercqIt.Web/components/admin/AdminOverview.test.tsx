import { render, screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ADMIN_NAV } from "./admin-sections";
import { AdminOverview } from "./AdminOverview";

describe("AdminOverview", () => {
  it("AdminOverview_Loaded_ShowsCountsAndLinksToEachSection", async () => {
    // Arrange: the MSW handlers return one blog, project and certification.
    render(<AdminOverview username="kenneth" roles={[]} nav={ADMIN_NAV} />);

    // Assert
    expect(await screen.findAllByText("1")).toHaveLength(3);
    const list = screen.getByRole("list");
    expect(within(list).getByRole("link", { name: "Blogs" })).toHaveAttribute(
      "href",
      "/admin/dashboard/blogs",
    );
    // The overview does not link to itself.
    expect(within(list).queryByRole("link", { name: "Overview" })).not.toBeInTheDocument();
    expect(within(list).getAllByRole("link")).toHaveLength(6);
  });
});
