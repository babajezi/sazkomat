"use client";

import * as React from "react";
import { Check, ChevronsUpDown, Search } from "lucide-react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import type { Country } from "@/lib/api/types";

interface CountrySearchSelectProps {
  countries: Country[];
  value: string | null;
  onValueChange: (countryId: string | null) => void;
  placeholder?: string;
  disabled?: boolean;
}

export function CountrySearchSelect({
  countries,
  value,
  onValueChange,
  placeholder = "Vyberte zemi...",
  disabled = false,
}: CountrySearchSelectProps) {
  const [open, setOpen] = React.useState(false);
  const [searchQuery, setSearchQuery] = React.useState("");

  const selectedCountry = countries.find((c) => c.id === value);

  // Filter countries based on search query
  const filteredCountries = React.useMemo(() => {
    if (!searchQuery) return countries;

    const query = searchQuery.toLowerCase();
    return countries.filter((country) => {
      return (
        country.name.toLowerCase().includes(query) ||
        country.code.toLowerCase().includes(query) ||
        (country.nameCs?.toLowerCase().includes(query) ?? false) ||
        (country.isoCode?.toLowerCase().includes(query) ?? false)
      );
    });
  }, [countries, searchQuery]);

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          aria-expanded={open}
          className="w-full justify-between"
          disabled={disabled}
        >
          {selectedCountry ? (
            <span className="flex items-center gap-2">
              <span>{selectedCountry.flagEmoji}</span>
              <span>{selectedCountry.name}</span>
              <span className="text-muted-foreground text-xs">
                ({selectedCountry.code})
              </span>
            </span>
          ) : (
            <span className="text-muted-foreground">{placeholder}</span>
          )}
          <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[400px] p-0" align="start">
        <Command shouldFilter={false}>
          <CommandInput
            placeholder="Hledat podle jmena, kodu..."
            value={searchQuery}
            onValueChange={setSearchQuery}
          />
          <CommandList>
            <CommandEmpty>Zadna zeme nenalezena.</CommandEmpty>
            <CommandGroup className="max-h-[300px] overflow-auto">
              {filteredCountries.map((country) => (
                <CommandItem
                  key={country.id}
                  value={country.id}
                  onSelect={(currentValue) => {
                    onValueChange(currentValue === value ? null : currentValue);
                    setOpen(false);
                    setSearchQuery("");
                  }}
                >
                  <Check
                    className={cn(
                      "mr-2 h-4 w-4",
                      value === country.id ? "opacity-100" : "opacity-0"
                    )}
                  />
                  <span className="mr-2">{country.flagEmoji}</span>
                  <span className="flex-1">{country.name}</span>
                  {country.nameCs && country.nameCs !== country.name && (
                    <span className="text-muted-foreground text-sm mr-2">
                      ({country.nameCs})
                    </span>
                  )}
                  <span className="text-muted-foreground text-xs font-mono">
                    {country.code}
                  </span>
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  );
}
