interface CountryFlagProps {
  isoCode: string;
  className?: string;
}

export function CountryFlag({ isoCode, className = "" }: CountryFlagProps) {
  // flag-icons uses pattern: fi fi-{isoCode}
  // Example: "gb" -> "fi fi-gb", "de" -> "fi fi-de"

  if (!isoCode || isoCode.length !== 2) {
    // Fallback for invalid ISO codes
    return <span className={className}>🏳️</span>;
  }

  // Combine flag-icons classes with custom className
  const flagClasses = `fi fi-${isoCode.toLowerCase()} ${className}`.trim();

  return <span className={flagClasses} title={isoCode.toUpperCase()} />;
}
