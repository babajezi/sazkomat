"use client";

import { useState, KeyboardEvent } from "react";
import { X } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

interface TagInputProps {
  value: string[];
  onChange: (value: string[]) => void;
  placeholder?: string;
  className?: string;
}

export function TagInput({
  value,
  onChange,
  placeholder = "Type and press Enter",
  className,
}: TagInputProps) {
  const [inputValue, setInputValue] = useState("");

  const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    // Add tag on Enter or Comma
    if (e.key === "Enter" || e.key === ",") {
      e.preventDefault();
      addTag();
    }
    // Remove last tag on Backspace if input is empty
    else if (e.key === "Backspace" && !inputValue && value.length > 0) {
      removeTag(value[value.length - 1]);
    }
  };

  const addTag = () => {
    const trimmedValue = inputValue.trim().replace(/,$/, ""); // Remove trailing comma
    if (trimmedValue && !value.includes(trimmedValue)) {
      onChange([...value, trimmedValue]);
      setInputValue("");
    }
  };

  const removeTag = (tagToRemove: string) => {
    onChange(value.filter((tag) => tag !== tagToRemove));
  };

  return (
    <div
      className={cn(
        "flex min-h-[40px] w-full flex-wrap gap-2 rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-within:ring-2 focus-within:ring-ring focus-within:ring-offset-2",
        className
      )}
    >
      {value.map((tag) => (
        <Badge
          key={tag}
          variant="secondary"
          className="gap-1 pr-1 hover:bg-secondary/80"
        >
          <span>{tag}</span>
          <button
            type="button"
            onClick={() => removeTag(tag)}
            className="rounded-full hover:bg-muted p-0.5 transition-colors"
          >
            <X className="h-3 w-3" />
          </button>
        </Badge>
      ))}
      <Input
        type="text"
        value={inputValue}
        onChange={(e) => setInputValue(e.target.value)}
        onKeyDown={handleKeyDown}
        onBlur={addTag} // Add tag when input loses focus
        placeholder={value.length === 0 ? placeholder : ""}
        className="flex-1 min-w-[120px] border-0 p-0 h-auto focus-visible:ring-0 focus-visible:ring-offset-0"
      />
    </div>
  );
}
