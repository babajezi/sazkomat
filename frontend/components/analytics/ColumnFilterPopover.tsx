"use client";

import { useState, useEffect } from "react";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Button } from "@/components/ui/button";
import { Command, CommandInput, CommandList, CommandEmpty, CommandGroup, CommandItem } from "@/components/ui/command";
import { Filter, Loader2, Check } from "lucide-react";
import { analyticsApi } from "@/lib/api/client";
import type { ViewSpec } from "@/lib/api/types";

interface DistinctValue {
  value: string;
  count: number;
}

interface Props {
  column: string;
  spec: ViewSpec;
  activeFilters: string[];
  onApply: (column: string, values: string[]) => void;
}

export function ColumnFilterPopover({ column, spec, activeFilters, onApply }: Props) {
  const [open, setOpen] = useState(false);
  const [items, setItems] = useState<DistinctValue[]>([]);
  const [selected, setSelected] = useState<Set<string>>(new Set(activeFilters));
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isActive = activeFilters.length > 0;

  useEffect(() => {
    if (open) {
      setSelected(new Set(activeFilters));
      setError(null);
      loadValues();
    }
  }, [open]);

  async function loadValues() {
    setLoading(true);
    try {
      // Send spec WITHOUT columnFilters so we always get all possible values
      const cleanSpec: ViewSpec = { ...spec, columnFilters: undefined };
      const result = await analyticsApi.getDistinctValues(cleanSpec, column);
      setItems(result);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Chyba při načítání hodnot");
    } finally {
      setLoading(false);
    }
  }

  function toggleValue(value: string) {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(value)) {
        next.delete(value);
      } else {
        next.add(value);
      }
      return next;
    });
  }

  function selectAll() {
    setSelected(new Set(items.map((i) => i.value)));
  }

  function selectNone() {
    setSelected(new Set());
  }

  function handleApply() {
    onApply(column, Array.from(selected));
    setOpen(false);
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          className={`inline-flex items-center justify-center rounded p-0.5 transition-colors hover:bg-muted ${
            isActive ? "text-blue-600" : "text-muted-foreground/40 hover:text-muted-foreground"
          }`}
          onClick={(e) => {
            e.stopPropagation();
            setOpen(!open);
          }}
          title={isActive ? `Filtr aktivní (${activeFilters.length})` : "Filtrovat"}
        >
          <Filter className="h-3.5 w-3.5" />
        </button>
      </PopoverTrigger>
      <PopoverContent className="w-72 p-0" align="start" onClick={(e) => e.stopPropagation()}>
        {loading ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
          </div>
        ) : error ? (
          <div className="p-4 text-sm text-red-500">{error}</div>
        ) : (
          <>
            <Command shouldFilter={true}>
              <CommandInput placeholder={`Hledat v ${column}...`} />
              <CommandList className="max-h-48">
                <CommandEmpty>Nic nenalezeno</CommandEmpty>
                <CommandGroup>
                  {items.map((item) => (
                    <CommandItem
                      key={item.value}
                      value={item.value}
                      onSelect={() => toggleValue(item.value)}
                      className="cursor-pointer"
                    >
                      <div className={`mr-2 flex h-4 w-4 shrink-0 items-center justify-center rounded-sm border ${
                        selected.has(item.value) ? "bg-primary border-primary text-primary-foreground" : "border-muted-foreground/30"
                      }`}>
                        {selected.has(item.value) && <Check className="h-3 w-3" />}
                      </div>
                      <span className="truncate text-sm flex-1">{item.value}</span>
                      <span className="text-xs text-muted-foreground tabular-nums ml-2">{item.count.toLocaleString()}</span>
                    </CommandItem>
                  ))}
                </CommandGroup>
              </CommandList>
            </Command>
            <div className="flex items-center gap-1 border-t p-2">
              <Button variant="ghost" size="sm" className="h-7 text-xs" onClick={selectAll}>
                Vše
              </Button>
              <Button variant="ghost" size="sm" className="h-7 text-xs" onClick={selectNone}>
                Nic
              </Button>
              <div className="flex-1" />
              <Button size="sm" className="h-7 text-xs" onClick={handleApply}>
                Použít {selected.size > 0 && `(${selected.size})`}
              </Button>
            </div>
          </>
        )}
      </PopoverContent>
    </Popover>
  );
}
