"use client";

import { useState, KeyboardEvent } from "react";
import { X, Plus } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";

interface TagInputProps {
  value: string[];
  onChange: (tags: string[]) => void;
  placeholder?: string;
  maxTags?: number;
  label?: string;
}

export function TagInput({
  value,
  onChange,
  placeholder = "Add a tag...",
  maxTags = 10,
  label = "Tags",
}: TagInputProps) {
  const [inputValue, setInputValue] = useState("");

  const addTag = () => {
    const trimmedValue = inputValue.trim();
    if (
      trimmedValue &&
      !value.includes(trimmedValue) &&
      value.length < maxTags
    ) {
      onChange([...value, trimmedValue]);
      setInputValue("");
    }
  };

  const removeTag = (tagToRemove: string) => {
    onChange(value.filter((tag) => tag !== tagToRemove));
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      e.preventDefault();
      addTag();
    } else if (e.key === "Backspace" && !inputValue && value.length > 0) {
      removeTag(value[value.length - 1]);
    }
  };

  return (
    <div className="space-y-2">
      <label className="text-sm font-medium">{label}</label>
      <div className="flex flex-wrap gap-2 p-2 border rounded-md min-h-[42px] bg-background">
        {value.map((tag) => (
          <Badge key={tag} variant="secondary" className="flex items-center gap-1">
            {tag}
            <button
              type="button"
              onClick={() => removeTag(tag)}
              className="ml-1 rounded-full hover:bg-muted-foreground/20 p-0.5"
            >
              <X className="h-3 w-3" />
            </button>
          </Badge>
        ))}
        {value.length < maxTags && (
          <div className="flex items-center gap-1 flex-1 min-w-[150px]">
            <Input
              type="text"
              value={inputValue}
              onChange={(e) => setInputValue(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder={placeholder}
              className="border-0 shadow-none focus-visible:ring-0 p-0 h-auto"
            />
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={addTag}
              disabled={!inputValue.trim()}
              className="h-6 w-6 p-0"
            >
              <Plus className="h-4 w-4" />
            </Button>
          </div>
        )}
      </div>
      <p className="text-xs text-muted-foreground">
        {value.length}/{maxTags} tags. Press Enter to add.
      </p>
    </div>
  );
}
