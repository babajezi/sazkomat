"use client";

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
  if (result.rows.length === 0) {
    return (
      <div className="text-center py-8 text-gray-500">
        Žádná data k zobrazení.
      </div>
    );
  }

  // Determine label column (first dimension) and value columns (metrics)
  const labelCol = result.columns[0];
  const valueColumns = result.columns.slice(1).filter((c) => c.type === "number");

  if (valueColumns.length === 0) {
    return <div className="text-center py-8 text-gray-500">Žádné numerické sloupce k zobrazení.</div>;
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

  if (type === "pieChart") {
    const valueCol = valueColumns[0];
    const valueKey = valueCol.alias || valueCol.name;
    return (
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
    );
  }

  if (type === "lineChart") {
    return (
      <ResponsiveContainer width="100%" height={400}>
        <LineChart data={chartData}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="name" angle={-45} textAnchor="end" height={80} fontSize={12} />
          <YAxis />
          <Tooltip />
          <Legend />
          {valueColumns.map((col, i) => (
            <Line
              key={col.name}
              type="monotone"
              dataKey={col.alias || col.name}
              stroke={COLORS[i % COLORS.length]}
              strokeWidth={2}
            />
          ))}
        </LineChart>
      </ResponsiveContainer>
    );
  }

  // Default: barChart
  return (
    <ResponsiveContainer width="100%" height={400}>
      <BarChart data={chartData}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="name" angle={-45} textAnchor="end" height={80} fontSize={12} />
        <YAxis />
        <Tooltip />
        <Legend />
        {valueColumns.map((col, i) => (
          <Bar
            key={col.name}
            dataKey={col.alias || col.name}
            fill={COLORS[i % COLORS.length]}
          />
        ))}
      </BarChart>
    </ResponsiveContainer>
  );
}
