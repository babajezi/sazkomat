"use client";

import { useState, useMemo } from "react";
import {
  BarChart,
  Bar,
  LineChart,
  Line,
  PieChart,
  Pie,
  Cell,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from "recharts";
import type { AnalyticsResult } from "@/lib/api/types";

interface Props {
  result: AnalyticsResult;
  type: string;
}

const COLORS = [
  "#2563eb", "#dc2626", "#16a34a", "#ca8a04", "#9333ea",
  "#0891b2", "#e11d48", "#65a30d", "#d97706", "#7c3aed",
];

export function AnalyticsChart({ result, type }: Props) {
  const [hiddenMetrics, setHiddenMetrics] = useState<Set<string>>(new Set());

  if (result.rows.length === 0) {
    return (
      <div className="text-center py-8 text-gray-500">
        Žádná data k zobrazení.
      </div>
    );
  }

  // Determine label column (first dimension) and value columns (metrics)
  const labelCol = result.columns[0];
  const allValueColumns = result.columns.slice(1).filter((c) => c.type === "number");

  if (allValueColumns.length === 0) {
    return <div className="text-center py-8 text-gray-500">Žádné numerické sloupce k zobrazení.</div>;
  }

  const valueColumns = allValueColumns.filter((c) => !hiddenMetrics.has(c.name));

  function toggleMetric(name: string) {
    setHiddenMetrics((prev) => {
      const next = new Set(prev);
      if (next.has(name)) {
        next.delete(name);
      } else {
        // Don't allow hiding all metrics
        if (allValueColumns.length - next.size <= 1) return prev;
        next.add(name);
      }
      return next;
    });
  }

  const chartData = result.rows.map((row) => {
    const item: Record<string, unknown> = {
      name: String(row[labelCol.name] ?? ""),
    };
    valueColumns.forEach((col) => {
      item[col.alias || col.name] = Number(row[col.name]) || 0;
    });
    return item;
  });

  // Color map: stable color per metric regardless of visibility
  const colorMap = new Map<string, string>();
  allValueColumns.forEach((col, i) => {
    colorMap.set(col.name, COLORS[i % COLORS.length]);
  });

  const metricSelector = allValueColumns.length > 1 && (
    <div className="flex flex-wrap gap-1.5 mb-3">
      {allValueColumns.map((col) => {
        const isVisible = !hiddenMetrics.has(col.name);
        const color = colorMap.get(col.name)!;
        return (
          <button
            key={col.name}
            onClick={() => toggleMetric(col.name)}
            className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium transition-colors border ${
              isVisible
                ? "border-transparent text-white"
                : "border-gray-300 text-gray-400 bg-white"
            }`}
            style={isVisible ? { backgroundColor: color } : undefined}
          >
            {col.alias || col.name}
          </button>
        );
      })}
    </div>
  );

  if (type === "pieChart") {
    const valueCol = valueColumns[0] ?? allValueColumns[0];
    const valueKey = valueCol.alias || valueCol.name;
    return (
      <div>
        {metricSelector}
        <ResponsiveContainer width="100%" height={400}>
          <PieChart>
            <Pie
              data={chartData}
              dataKey={valueKey}
              nameKey="name"
              cx="50%"
              cy="50%"
              outerRadius={150}
              label={({ name, percent }) =>
                `${name}: ${(percent * 100).toFixed(1)}%`
              }
            >
              {chartData.map((_, index) => (
                <Cell key={index} fill={COLORS[index % COLORS.length]} />
              ))}
            </Pie>
            <Tooltip />
            <Legend />
          </PieChart>
        </ResponsiveContainer>
      </div>
    );
  }

  const showLegend = valueColumns.length > 1;

  if (type === "lineChart") {
    return (
      <div>
        {metricSelector}
        <ResponsiveContainer width="100%" height={400}>
          <LineChart data={chartData}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="name" angle={-45} textAnchor="end" height={80} fontSize={12} />
            <YAxis />
            <Tooltip />
            {showLegend && <Legend />}
            {valueColumns.map((col) => (
              <Line
                key={col.name}
                type="monotone"
                dataKey={col.alias || col.name}
                stroke={colorMap.get(col.name)}
                strokeWidth={2}
              />
            ))}
          </LineChart>
        </ResponsiveContainer>
      </div>
    );
  }

  // Default: barChart
  return (
    <div>
      {metricSelector}
      <ResponsiveContainer width="100%" height={400}>
        <BarChart data={chartData}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="name" angle={-45} textAnchor="end" height={80} fontSize={12} />
          <YAxis />
          <Tooltip />
          {showLegend && <Legend />}
          {valueColumns.map((col) => (
            <Bar
              key={col.name}
              dataKey={col.alias || col.name}
              fill={colorMap.get(col.name)}
            />
          ))}
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}
