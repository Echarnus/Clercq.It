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

  it("shows the existing image until it is removed", async () => {
    const onChange = vi.fn();
    render(
      <ImageUpload
        onChange={onChange}
        existingImageUrl="https://images.example.com/x.png"
        label="Header"
      />,
    );

    expect(screen.getByAltText("Preview")).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: "Remove image" }));
    expect(onChange).toHaveBeenCalledWith(null);
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
