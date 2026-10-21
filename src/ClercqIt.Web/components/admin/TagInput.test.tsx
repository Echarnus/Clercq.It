import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import { TagInput } from "./TagInput";

function Controlled({ initial = [] as string[], maxTags = 3 }) {
  const [tags, setTags] = useState(initial);
  return <TagInput value={tags} onChange={setTags} maxTags={maxTags} />;
}

describe("TagInput", () => {
  it("adds a trimmed tag on Enter and ignores duplicates", async () => {
    const user = userEvent.setup();
    render(<Controlled />);
    const input = screen.getByPlaceholderText("Add a tag...");

    await user.type(input, "  react  {Enter}");
    await user.type(input, "react{Enter}");

    expect(screen.getByText("react")).toBeInTheDocument();
    expect(screen.getByText("1/3 tags. Press Enter to add.")).toBeInTheDocument();
  });

  it("removes the last tag on Backspace in an empty input", async () => {
    const user = userEvent.setup();
    render(<Controlled initial={["a", "b"]} />);

    await user.click(screen.getByPlaceholderText("Add a tag..."));
    await user.keyboard("{Backspace}");

    expect(screen.queryByText("b")).not.toBeInTheDocument();
    expect(screen.getByText("a")).toBeInTheDocument();
  });

  it("removes a tag with its remove button", async () => {
    const onChange = vi.fn();
    render(<TagInput value={["a", "b"]} onChange={onChange} />);

    await userEvent.click(screen.getByRole("button", { name: "Remove a" }));

    expect(onChange).toHaveBeenCalledWith(["b"]);
  });

  it("adds a tag with the add button, which is disabled for blank input", async () => {
    const user = userEvent.setup();
    render(<Controlled />);
    const add = screen.getByRole("button", { name: "Add tag" });
    expect(add).toBeDisabled();

    await user.type(screen.getByPlaceholderText("Add a tag..."), "vitest");
    await user.click(add);

    expect(screen.getByText("vitest")).toBeInTheDocument();
  });

  it("hides the input once the maximum is reached", () => {
    render(<TagInput value={["a", "b"]} onChange={vi.fn()} maxTags={2} label="Skills" />);

    expect(screen.queryByPlaceholderText("Add a tag...")).not.toBeInTheDocument();
    expect(screen.getByText("Skills")).toBeInTheDocument();
  });
});
