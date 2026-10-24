import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

// The content import stores this path for items without a picture.
const PLACEHOLDER_IMAGE = "/placeholder.svg";

/** The image to show for an item, or undefined when it has no picture. */
export function imageOrNone(image: string | null | undefined): string | undefined {
  const src = image?.trim();
  return src && src !== PLACEHOLDER_IMAGE ? src : undefined;
}
