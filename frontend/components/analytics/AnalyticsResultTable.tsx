"use client";

import { useMemo, useState } from "react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { ArrowDown, ArrowUp, ArrowUpDown } from "lucide-react";
import type { AnalyticsResult } from "@/lib/api/types";

interface Props {
  result: AnalyticsResult;
}

type SortDirection = "asc" | "desc";

function formatValue(value: unknown, type: string): string {
  if (value === null || value === undefined) return "—";
  if (type === "number") {
    const num = Number(value);
    if (Number.isInteger(num)) return num.toLocaleString();
    return num.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }
  if (type === "date") return new Date(String(value)).toLocaleDateString();
  return String(value);
}

function compareValues(a: unknown, b: unknown, type: string): number {
  if (a == null && b == null) return 0;
  if (a == null) return 1;
  if (b == null) return -1;

  if (type === "number") return Number(a) - Number(b);
  if (type === "date") return new Date(String(a)).getTime() - new Date(String(b)).getTime();
  return String(a).localeCompare(String(b));
}

export function AnalyticsResultTable({ result }: Props) {
  const initialSort = result.spec.sort;
  const [sortColumn, setSortColumn] = useState<string | null>(initialSort?.column ?? null);
  const [sortDirection, setSortDirection] = useState<SortDirection>(
    (initialSort?.direction as SortDirection) ?? "asc"
  );

  const columnTypeMap = useMemo(() => {
    const map = new Map<string, string>();
    for (const col of result.columns) {
      map.set(col.name, col.type);
    }
    return map;
  }, [result.columns]);

  const sortedRows = useMemo(() => {
    if (!sortColumn) return result.rows;
    const type = columnTypeMap.get(sortColumn) ?? "string";
    const dir = sortDirection === "asc" ? 1 : -1;
    return [...result.rows].sort((a, b) => dir * compareValues(a[sortColumn], b[sortColumn], type));
  }, [result.rows, sortColumn, sortDirection, columnTypeMap]);

  function handleSort(columnName: string) {
    if (sortColumn === columnName) {
      setSortDirection((d) => (d === "asc" ? "desc" : "asc"));
    } else {
      setSortColumn(columnName);
      setSortDirection("asc");
    }
  }

  if (result.rows.length === 0) {
    return (
      <div className="text-center py-8 text-gray-500">
        Žádné výsledky pro zadané filtry.
      </div>
    );
  }

  return (
    <div className="overflow-x-auto">
      <Table>
        <TableHeader>
          <TableRow>
            {result.columns.map((col) => {
              const isActive = sortColumn === col.name;
              return (
                <TableHead
                  key={col.name}
                  className={`whitespace-nowrap cursor-pointer select-none hover:bg-muted/50 ${col.type === "number" ? "text-right" : ""}`}
                  onClick={() => handleSort(col.name)}
                >
                  <div className={`flex items-center gap-1 ${col.type === "number" ? "justify-end" : ""}`}>
                    <span>{col.alias || col.name}</span>
                    {isActive ? (
                      sortDirection === "asc" ? (
                        <ArrowUp className="h-3.5 w-3.5" />
                      ) : (
                        <ArrowDown className="h-3.5 w-3.5" />
                      )
                    ) : (
                      <ArrowUpDown className="h-3.5 w-3.5 text-muted-foreground/40" />
                    )}
                  </div>
                </TableHead>
              );
            })}
          </TableRow>
        </TableHeader>
        <TableBody>
          {sortedRows.map((row, i) => (
            <TableRow key={i}>
              {result.columns.map((col) => (
                <TableCell
                  key={col.name}
                  className={`whitespace-nowrap ${col.type === "number" ? "text-right tabular-nums" : ""}`}
                >
                  {formatValue(row[col.name], col.type)}
                </TableCell>
              ))}
            </TableRow>
          ))}
        </TableBody>
      </Table>
      <div className="text-xs text-gray-500 mt-2 px-2">
        {result.totalRows} řádků · {result.executionMs}ms
      </div>
    </div>
  );
}
