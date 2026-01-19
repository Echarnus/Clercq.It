"use client";

import Image from "next/image";
import { cn } from "@/lib/utils";

interface BlurImageProps {
  src: string;
  alt: string;
  width?: number;
  height?: number;
  className?: string;
  containerClassName?: string;
  priority?: boolean;
}

export function BlurImage({
  src,
  alt,
  width = 400,
  height = 300,
  className,
  containerClassName,
  priority = false,
}: BlurImageProps) {
  return (
    <div
      className={cn(
        "relative overflow-hidden",
        containerClassName
      )}
    >
      {/* Blurred background image */}
      <div className="absolute inset-0">
        <Image
          src={src}
          alt=""
          fill
          className="object-cover scale-110 blur-xl opacity-60"
          sizes="(max-width: 768px) 100vw, 33vw"
          priority={priority}
        />
      </div>

      {/* Main image */}
      <div className="relative h-full w-full flex items-center justify-center">
        <Image
          src={src}
          alt={alt}
          width={width}
          height={height}
          className={cn("object-contain max-h-[98%] max-w-[98%] relative z-10", className)}
          priority={priority}
        />
      </div>
    </div>
  );
}
