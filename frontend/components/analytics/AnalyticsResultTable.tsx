"use client";

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import type { AnalyticsResult } from "@/lib/api/types";

interface Props {
  result: AnalyticsResult;
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

export function AnalyticsResultTable({ result }: Props) {
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
            {result.columns.map((col) => (
              <TableHead key={col.name} className="whitespace-nowrap">
                {col.alias || col.name}
              </TableHead>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {result.rows.map((row, i) => (
            <TableRow key={i}>
              {result.columns.map((col) => (
                <TableCell key={col.name} className="whitespace-nowrap">
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
