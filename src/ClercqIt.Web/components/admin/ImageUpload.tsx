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
            <div className="relative aspect-video rounded-lg overflow-hidden border bg-muted">
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
              className="absolute top-2 right-2"
              onClick={handleRemove}
            >
              <X className="h-4 w-4" />
            </Button>
          </div>
        ) : (
          <div
            className="flex flex-col items-center justify-center w-full max-w-sm aspect-video border-2 border-dashed rounded-lg cursor-pointer hover:bg-muted/50 transition-colors"
            onClick={() => inputRef.current?.click()}
          >
            <ImageIcon className="h-10 w-10 text-muted-foreground mb-2" />
            <p className="text-sm text-muted-foreground">Click to upload image</p>
            <p className="text-xs text-muted-foreground mt-1">
              JPG, PNG, GIF, or WebP
            </p>
          </div>
        )}
        <input
          ref={inputRef}
          type="file"
          accept={accept}
          onChange={handleFileChange}
          className="hidden"
        />
        {!displayImage && (
          <Button
            type="button"
            variant="outline"
            onClick={() => inputRef.current?.click()}
            className="w-fit"
          >
            <Upload className="h-4 w-4 mr-2" />
            Choose File
          </Button>
        )}
      </div>
      {value && (
        <p className="text-xs text-muted-foreground">
          Selected: {value.name}
        </p>
      )}
    </div>
  );
}
