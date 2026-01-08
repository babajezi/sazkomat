"use client";

import { useLanguage } from "@/contexts/UserContext";
import { LanguagePreference } from "@/lib/api/types";

interface LanguageSelectorProps {
  /** Visual variant */
  variant?: "buttons" | "dropdown";
  /** Size variant */
  size?: "sm" | "md";
  /** Show labels */
  showLabels?: boolean;
  /** Custom class name */
  className?: string;
}

export function LanguageSelector({
  variant = "buttons",
  size = "md",
  showLabels = false,
  className = "",
}: LanguageSelectorProps) {
  const { language, changeLanguage, isAuthenticated } = useLanguage();

  const handleLanguageChange = async (newLanguage: LanguagePreference) => {
    if (newLanguage === language) return;

    if (isAuthenticated) {
      await changeLanguage(newLanguage);
    } else {
      // For non-authenticated users, store in localStorage
      localStorage.setItem("preferredLanguage", newLanguage);
      // Dispatch custom event so components can react
      window.dispatchEvent(new CustomEvent("languageChange", { detail: newLanguage }));
    }
  };

  const sizeClasses = {
    sm: "px-2 py-1 text-xs",
    md: "px-3 py-1.5 text-sm",
  };

  if (variant === "dropdown") {
    return (
      <select
        value={language}
        onChange={(e) => handleLanguageChange(e.target.value as LanguagePreference)}
        className={`
          rounded-md border border-gray-300 bg-white text-gray-700
          focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500
          ${sizeClasses[size]}
          ${className}
        `}
      >
        <option value={LanguagePreference.Czech}>
          {showLabels ? "Cestina" : "CZ"}
        </option>
        <option value={LanguagePreference.English}>
          {showLabels ? "English" : "EN"}
        </option>
      </select>
    );
  }

  // Buttons variant (default)
  return (
    <div className={`flex gap-1 ${className}`}>
      <button
        onClick={() => handleLanguageChange(LanguagePreference.Czech)}
        className={`
          ${sizeClasses[size]} rounded-md transition-colors
          ${
            language === LanguagePreference.Czech
              ? "bg-blue-100 text-blue-700 font-medium"
              : "bg-gray-100 text-gray-600 hover:bg-gray-200"
          }
        `}
        title="Cestina"
      >
        {showLabels ? "Cestina" : "CZ"}
      </button>
      <button
        onClick={() => handleLanguageChange(LanguagePreference.English)}
        className={`
          ${sizeClasses[size]} rounded-md transition-colors
          ${
            language === LanguagePreference.English
              ? "bg-blue-100 text-blue-700 font-medium"
              : "bg-gray-100 text-gray-600 hover:bg-gray-200"
          }
        `}
        title="English"
      >
        {showLabels ? "English" : "EN"}
      </button>
    </div>
  );
}
