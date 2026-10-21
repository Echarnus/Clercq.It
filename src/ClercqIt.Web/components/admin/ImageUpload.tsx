"use client";

import { useState, useRef, ChangeEvent } from "react";
import { Upload, X, ImageIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import Image from "next/image";

interface ImageUploadProps {
  value?: File | null;
  onChange: (file: File | null) => void;
  existingImageUrl?: string;
  label?: string;
  accept?: string;
}

export function ImageUpload({
  value,
  onChange,
  existingImageUrl,
  label = "Image",
  accept = "image/jpeg,image/png,image/gif,image/webp",
}: ImageUploadProps) {
  const [preview, setPreview] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      onChange(file);
      const reader = new FileReader();
      reader.onloadend = () => {
        setPreview(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleRemove = () => {
    onChange(null);
    setPreview(null);
    if (inputRef.current) {
      inputRef.current.value = "";
    }
  };

  const displayImage = preview || (existingImageUrl && !value ? existingImageUrl : null);

  return (
    <div className="space-y-2">
      <label className="text-sm font-medium">{label}</label>
      <div className="flex flex-col gap-4">
        {displayImage ? (
          <div className="relative w-full max-w-sm">
            <div className="relative aspect-video overflow-hidden rounded-lg border bg-muted">
              <Image
                src={displayImage}
                alt="Preview"
                fill
                className="object-cover"
                unoptimized={preview !== null}
              />
            </div>
            <Button
              type="button"
              variant="destructive"
              size="sm"
              className="absolute right-2 top-2"
              onClick={handleRemove}
              aria-label="Remove image"
            >
              <X className="h-4 w-4" />
            </Button>
          </div>
        ) : (
          <button
            type="button"
            className="flex aspect-video w-full max-w-sm cursor-pointer flex-col items-center justify-center rounded-lg border-2 border-dashed transition-colors hover:bg-muted/50"
            onClick={() => inputRef.current?.click()}
          >
            <ImageIcon className="mb-2 h-10 w-10 text-muted-foreground" />
            <p className="text-sm text-muted-foreground">Click to upload image</p>
            <p className="mt-1 text-xs text-muted-foreground">JPG, PNG, GIF, or WebP</p>
          </button>
        )}
        <input
          ref={inputRef}
          type="file"
          accept={accept}
          onChange={handleFileChange}
          className="hidden"
          aria-label={label}
        />
        {!displayImage && (
          <Button
            type="button"
            variant="outline"
            onClick={() => inputRef.current?.click()}
            className="w-fit"
          >
            <Upload className="mr-2 h-4 w-4" />
            Choose File
          </Button>
        )}
      </div>
      {value && <p className="text-xs text-muted-foreground">Selected: {value.name}</p>}
    </div>
  );
}
