"use client";

import { useState, useRef, useEffect } from "react";
import { Button } from "@/components/ui/button";
import { useUser, useLanguage } from "@/contexts/UserContext";
import { LanguagePreference } from "@/lib/api/types";

export function UserMenu() {
  const { user, logout, isLoading } = useUser();
  const { language, changeLanguage } = useLanguage();
  const [isOpen, setIsOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  // Close menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  if (!user) return null;

  const handleLogout = async () => {
    setIsOpen(false);
    await logout();
  };

  const handleLanguageChange = async (newLanguage: LanguagePreference) => {
    await changeLanguage(newLanguage);
  };

  const displayName = user.displayName || user.email.split("@")[0];
  const initials = displayName
    .split(" ")
    .map((n) => n[0])
    .join("")
    .toUpperCase()
    .slice(0, 2);

  return (
    <div className="relative" ref={menuRef}>
      {/* Trigger button */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center gap-2 rounded-full bg-gray-100 hover:bg-gray-200 px-3 py-1.5 transition-colors"
        aria-expanded={isOpen}
        aria-haspopup="true"
      >
        {/* Avatar */}
        <div className="w-8 h-8 rounded-full bg-blue-600 text-white flex items-center justify-center text-sm font-medium">
          {initials}
        </div>
        <span className="text-sm font-medium text-gray-700 hidden sm:inline">
          {displayName}
        </span>
        {/* Chevron */}
        <svg
          className={`w-4 h-4 text-gray-500 transition-transform ${
            isOpen ? "rotate-180" : ""
          }`}
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M19 9l-7 7-7-7"
          />
        </svg>
      </button>

      {/* Dropdown menu */}
      {isOpen && (
        <div className="absolute right-0 mt-2 w-64 bg-white rounded-lg shadow-lg border border-gray-200 py-2 z-50">
          {/* User info */}
          <div className="px-4 py-2 border-b border-gray-100">
            <p className="text-sm font-medium text-gray-900">{displayName}</p>
            <p className="text-xs text-gray-500">{user.email}</p>
          </div>

          {/* Language selector */}
          <div className="px-4 py-3 border-b border-gray-100">
            <p className="text-xs font-medium text-gray-500 uppercase mb-2">
              Jazyk / Language
            </p>
            <div className="flex gap-2">
              <button
                onClick={() => handleLanguageChange(LanguagePreference.Czech)}
                className={`flex-1 px-3 py-1.5 text-sm rounded-md transition-colors ${
                  language === LanguagePreference.Czech
                    ? "bg-blue-100 text-blue-700 font-medium"
                    : "bg-gray-100 text-gray-600 hover:bg-gray-200"
                }`}
              >
                Cz
              </button>
              <button
                onClick={() => handleLanguageChange(LanguagePreference.English)}
                className={`flex-1 px-3 py-1.5 text-sm rounded-md transition-colors ${
                  language === LanguagePreference.English
                    ? "bg-blue-100 text-blue-700 font-medium"
                    : "bg-gray-100 text-gray-600 hover:bg-gray-200"
                }`}
              >
                En
              </button>
            </div>
          </div>

          {/* Logout */}
          <div className="px-2 pt-2">
            <button
              onClick={handleLogout}
              disabled={isLoading}
              className="w-full px-4 py-2 text-sm text-left text-red-600 hover:bg-red-50 rounded-md transition-colors disabled:opacity-50"
            >
              {isLoading ? "Odhlašování..." : "Odhlásit se"}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
