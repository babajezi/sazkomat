"use client";

import { useMemo } from "react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { ArrowDown, ArrowUp, ArrowUpDown, Loader2 } from "lucide-react";
import { ColumnFilterPopover } from "@/components/analytics/ColumnFilterPopover";
import type { AnalyticsResult, ViewSpec } from "@/lib/api/types";

interface Props {
  result: AnalyticsResult;
  onSort?: (column: string, direction: "asc" | "desc") => void;
  isSorting?: boolean;
  onFilter?: (column: string, values: string[]) => void;
}

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

export function AnalyticsResultTable({ result, onSort, isSorting, onFilter }: Props) {
  const sortColumn = result.spec.sort?.column ?? null;
  const sortDirection = (result.spec.sort?.direction as "asc" | "desc") ?? "asc";
  const columnFilters = result.spec.columnFilters ?? {};
  const excludeFilterColumns = new Set(result.spec.excludeFilterColumns ?? []);
  const hasCustomSql = !!result.spec.customSql;

  const rows = useMemo(() => result.rows, [result.rows]);

  function handleSort(columnName: string) {
    if (!onSort) return;
    if (sortColumn === columnName) {
      onSort(columnName, sortDirection === "asc" ? "desc" : "asc");
    } else {
      onSort(columnName, "desc");
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
              const showFilter = hasCustomSql && onFilter && !excludeFilterColumns.has(col.name);
              const activeFiltersForCol = columnFilters[col.name] ?? [];
              return (
                <TableHead
                  key={col.name}
                  className={`whitespace-nowrap select-none ${onSort ? "cursor-pointer hover:bg-muted/50" : ""} ${col.type === "number" ? "text-right" : ""}`}
                  onClick={() => handleSort(col.name)}
                >
                  <div className={`flex items-center gap-1 ${col.type === "number" ? "justify-end" : ""}`}>
                    <span>{col.alias || col.name}</span>
                    {onSort && (
                      isActive ? (
                        sortDirection === "asc" ? (
                          <ArrowUp className="h-3.5 w-3.5" />
                        ) : (
                          <ArrowDown className="h-3.5 w-3.5" />
                        )
                      ) : (
                        <ArrowUpDown className="h-3.5 w-3.5 text-muted-foreground/40" />
                      )
                    )}
                    {showFilter && (
                      <ColumnFilterPopover
                        column={col.name}
                        spec={result.spec}
                        activeFilters={activeFiltersForCol}
                        onApply={onFilter}
                      />
                    )}
                  </div>
                </TableHead>
              );
            })}
          </TableRow>
        </TableHeader>
        <TableBody className={isSorting ? "opacity-50 transition-opacity" : "transition-opacity"}>
          {rows.map((row, i) => (
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
      <div className="flex items-center gap-2 text-xs text-gray-500 mt-2 px-2">
        {isSorting && <Loader2 className="h-3 w-3 animate-spin" />}
        <span>{result.totalRows} řádků · {result.executionMs}ms</span>
      </div>
    </div>
  );
}
