import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import { ImageUpload } from "./ImageUpload";

function Controlled({ existingImageUrl }: { existingImageUrl?: string }) {
  const [file, setFile] = useState<File | null>(null);
  return (
    <ImageUpload
      value={file}
      onChange={setFile}
      existingImageUrl={existingImageUrl}
      label="Header"
    />
  );
}

describe("ImageUpload", () => {
  it("previews a chosen file and clears it again", async () => {
    const user = userEvent.setup();
    render(<Controlled />);
    expect(screen.getByText("Click to upload image")).toBeInTheDocument();

    await user.upload(
      screen.getByLabelText("Header"),
      new File(["png"], "a.png", { type: "image/png" }),
    );

    expect(await screen.findByAltText("Preview")).toBeInTheDocument();
    expect(screen.getByText("Selected: a.png")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Remove image" }));

    expect(screen.queryByAltText("Preview")).not.toBeInTheDocument();
    expect(screen.getByText("Click to upload image")).toBeInTheDocument();
  });

  it("removes an existing image when the form allows it", async () => {
    const onChange = vi.fn();
    const onRemoveExisting = vi.fn();
    render(
      <ImageUpload
        onChange={onChange}
        existingImageUrl="https://images.example.com/x.png"
        onRemoveExisting={onRemoveExisting}
        label="Header"
      />,
    );

    expect(screen.getByAltText("Preview")).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: "Remove image" }));

    expect(onRemoveExisting).toHaveBeenCalledOnce();
    expect(screen.queryByAltText("Preview")).not.toBeInTheDocument();
    expect(screen.getByText("Click to upload image")).toBeInTheDocument();
  });

  it("only offers to replace a required existing image", () => {
    render(
      <ImageUpload
        onChange={vi.fn()}
        existingImageUrl="https://images.example.com/x.png"
        label="Header"
      />,
    );

    expect(screen.getByAltText("Preview")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Remove image" })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Replace image" })).toBeInTheDocument();
  });

  it("shows the existing image again when a newly chosen file is removed", async () => {
    const user = userEvent.setup();
    render(<Controlled existingImageUrl="https://images.example.com/x.png" />);

    await user.upload(
      screen.getByLabelText("Header"),
      new File(["png"], "a.png", { type: "image/png" }),
    );
    expect(await screen.findByText("Selected: a.png")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Remove image" }));

    expect(screen.queryByText("Selected: a.png")).not.toBeInTheDocument();
    expect(screen.getByAltText("Preview")).toHaveAttribute("src", expect.stringContaining("x.png"));
  });

  it("treats the import placeholder as no image", () => {
    render(<ImageUpload onChange={vi.fn()} existingImageUrl="/placeholder.svg" label="Header" />);

    expect(screen.queryByAltText("Preview")).not.toBeInTheDocument();
    expect(screen.getByText("Click to upload image")).toBeInTheDocument();
  });

  it("opens the file picker from both buttons", async () => {
    render(<Controlled />);
    const input = screen.getByLabelText<HTMLInputElement>("Header");
    const click = vi.spyOn(input, "click");

    await userEvent.click(screen.getByText("Click to upload image"));
    await userEvent.click(screen.getByRole("button", { name: "Choose File" }));

    expect(click).toHaveBeenCalledTimes(2);
  });
});
