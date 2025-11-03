"use client";

import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";

interface PaginationControlsProps {
  page: number;
  pageSize: number;
  totalCount: number;
  displayedCount: number;
  itemName: string; // "sportů", "zemí", "lig", "providerů"
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
  className?: string;
}

export function PaginationControls({
  page,
  pageSize,
  totalCount,
  displayedCount,
  itemName,
  onPageChange,
  onPageSizeChange,
  className = "",
}: PaginationControlsProps) {
  const totalPages = Math.ceil(totalCount / pageSize);
  const startIndex = page * pageSize + 1;
  const endIndex = Math.min((page + 1) * pageSize, totalCount);

  return (
    <Card className={className}>
      <CardContent className="pt-6">
        <div className="flex items-center justify-between flex-wrap gap-4">
          <div className="flex items-center gap-4 flex-wrap">
            <div className="flex items-center gap-2">
              <label className="text-sm font-medium">Na stránku:</label>
              <select
                value={pageSize}
                onChange={(e) => {
                  onPageSizeChange(Number(e.target.value));
                  onPageChange(0); // Reset to first page
                }}
                className="border rounded px-3 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="10">10</option>
                <option value="20">20</option>
                <option value="50">50</option>
                <option value="100">100</option>
              </select>
            </div>
            {totalCount > 0 && (
              <>
                <div className="text-sm text-gray-600 font-medium">
                  Stránka {page + 1} z {totalPages}
                </div>
                <div className="text-sm text-gray-500">
                  (Zobrazeno {startIndex}-{endIndex} z {displayedCount} {itemName})
                </div>
                {displayedCount < totalCount && (
                  <div className="text-sm text-gray-400">
                    - celkem {totalCount} {itemName}
                  </div>
                )}
              </>
            )}
            {totalCount === 0 && (
              <div className="text-sm text-gray-500">
                Žádné záznamy k zobrazení
              </div>
            )}
          </div>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(Math.max(0, page - 1))}
              disabled={page === 0 || totalCount === 0}
            >
              ← Předchozí
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(page + 1)}
              disabled={page >= totalPages - 1 || totalCount === 0}
            >
              Další →
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
