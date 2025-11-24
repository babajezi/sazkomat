"use client";

import { useState } from "react";
import { cn } from "@/lib/utils";
import type { DataProvider, LogoSize } from "@/lib/api/types";
import { configApi } from "@/lib/api/client";

interface ProviderLogoProps {
  provider: DataProvider;
  size?: LogoSize;
  className?: string;
}

const sizeClasses = {
  sm: "w-16 h-16 text-xl",      // 64px
  md: "w-32 h-32 text-4xl",     // 128px
  lg: "w-64 h-64 text-8xl"      // 256px
};

/**
 * Generates a consistent color from a string using simple hash
 */
function stringToColor(str: string): string {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    hash = str.charCodeAt(i) + ((hash << 5) - hash);
  }

  const hue = Math.abs(hash % 360);
  return `hsl(${hue}, 70%, 60%)`;
}

/**
 * Extracts initials from provider code (first 2 uppercase characters)
 */
function getInitials(code: string): string {
  return code.substring(0, 2).toUpperCase();
}

export function ProviderLogo({ provider, size = "md", className }: ProviderLogoProps) {
  const [imageError, setImageError] = useState(false);
  const showFallback = !provider.hasLogo || imageError;

  if (showFallback) {
    // Fallback: Show initials with colored background
    const initials = getInitials(provider.code);
    const backgroundColor = stringToColor(provider.code);

    return (
      <div
        className={cn(
          "flex items-center justify-center rounded-lg font-bold select-none",
          sizeClasses[size],
          className
        )}
        style={{ backgroundColor, color: "white" }}
        title={provider.name}
      >
        {initials}
      </div>
    );
  }

  // Show logo image
  const logoUrl = configApi.getProviderLogoUrl(provider.id, size);

  return (
    <div
      className={cn(
        "relative rounded-lg overflow-hidden flex-shrink-0",
        sizeClasses[size],
        className
      )}
      title={provider.name}
    >
      {/* Use regular img tag instead of Next.js Image - supports SVG from external URLs */}
      <img
        src={logoUrl}
        alt={`${provider.name} logo`}
        className="w-full h-full object-contain"
        onError={() => setImageError(true)}
      />
    </div>
  );
}
