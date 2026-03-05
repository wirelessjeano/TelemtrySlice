"use client"

import { appRouterClient } from "@telemetryslice/trpc/react";
import { keepPreviousData } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { ResponsiveContainer, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip } from "recharts";
import { WiThermometer } from "react-icons/wi";
import { ApiUnavailable } from "./ApiUnavailable";
import AppProvider from "../_providers/AppProvider";

function DeviceDetailsContent({ customerId, deviceId }: { customerId: string; deviceId: string }) {
    const { data: device, isLoading, isError } = appRouterClient.device.useQuery({ customerId, deviceId });

    if (isLoading) {
        return (
            <div className="flex justify-center mt-6">
                <span className="loading loading-spinner loading-md" />
            </div>
        );
    }

    if (isError || !device) {
        return <p className="text-center mt-6 text-error">Failed to load device.</p>;
    }

    return (
        <div className="card bg-base-200 shadow-sm">
            <div className="card-body">
                <div className="flex items-center gap-3">
                    <WiThermometer className="h-10 w-10 opacity-50" />
                    <h2 className="card-title">{device.label}</h2>
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mt-4">
                    <div>
                        <p className="text-sm opacity-70">Device ID</p>
                        <p>{device.deviceId}</p>
                    </div>
                    <div>
                        <p className="text-sm opacity-70">Customer ID</p>
                        <p>{device.customerId}</p>
                    </div>
                    <div>
                        <p className="text-sm opacity-70">Location</p>
                        <p>{device.location}</p>
                    </div>
                </div>
            </div>
        </div>
    );
}

function DeviceMetricsContent({ customerId, deviceId }: { customerId: string; deviceId: string }) {
    const { data: metrics, isLoading, isError } = appRouterClient.deviceMetrics.useQuery({ customerId, deviceId });

    if (isLoading) {
        return (
            <div className="flex justify-center mt-6">
                <span className="loading loading-spinner loading-md" />
            </div>
        );
    }

    if (isError || !metrics) {
        return <p className="text-center mt-6 text-error">Failed to load metrics.</p>;
    }

    return (
        <div className="card bg-base-200 shadow-sm mt-4">
            <div className="card-body">
                <h3 className="card-title text-base">Metrics</h3>
                <div className="stats stats-vertical sm:stats-horizontal shadow">
                    <div className="stat">
                        <div className="stat-title">Latest</div>
                        <div className="stat-value text-lg">{metrics.latest}°C</div>
                    </div>
                    <div className="stat">
                        <div className="stat-title">Average</div>
                        <div className="stat-value text-lg">{metrics.average.toFixed(2)}°C</div>
                    </div>
                    <div className="stat">
                        <div className="stat-title">Min</div>
                        <div className="stat-value text-lg">{metrics.minimum}°C</div>
                    </div>
                    <div className="stat">
                        <div className="stat-title">Max</div>
                        <div className="stat-value text-lg">{metrics.maximum}°C</div>
                    </div>
                    <div className="stat">
                        <div className="stat-title">Total Readings</div>
                        <div className="stat-value text-lg">{metrics.totalCount.toLocaleString()}</div>
                    </div>
                </div>
            </div>
        </div>
    );
}

function DeviceChartContent({ customerId, deviceId }: { customerId: string; deviceId: string }) {
    const { data: chartData, isLoading, isError } = appRouterClient.deviceChart.useQuery({ customerId, deviceId });

    const formattedData = useMemo(() =>
        chartData?.map((item) => ({
            date: new Date(item.recordedAt).toLocaleDateString(undefined, { month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" }),
            value: item.value,
        })),
        [chartData],
    );

    if (isLoading) {
        return (
            <div className="flex justify-center mt-6">
                <span className="loading loading-spinner loading-md" />
            </div>
        );
    }

    if (isError || !formattedData) {
        return <p className="text-center mt-6 text-error">Failed to load chart data.</p>;
    }

    return (
        <div className="card bg-base-200 shadow-sm mt-4">
            <div className="card-body">
                <h3 className="card-title text-base">Temperature Over Time</h3>
                <ResponsiveContainer width="100%" height={300}>
                    <LineChart data={formattedData}>
                        <CartesianGrid strokeDasharray="3 3" opacity={0.3} />
                        <XAxis dataKey="date" tick={{ fontSize: 12 }} ticks={formattedData.length > 1 ? [formattedData[0].date, formattedData[formattedData.length - 1].date] : undefined} />
                        <YAxis unit="°C" tick={{ fontSize: 12 }} />
                        <Tooltip
                            contentStyle={{ backgroundColor: "#1d232a", borderColor: "#374151", borderRadius: "0.5rem" }}
                            labelStyle={{ color: "#a6adbb" }}
                            itemStyle={{ color: "#f9fafb" }}
                            labelFormatter={(label) => label}
                            formatter={(value) => [`${value}°C`, "Temperature"]}
                        />
                        <Line type="monotone" dataKey="value" stroke="#6366f1" dot={false} strokeWidth={2} />
                    </LineChart>
                </ResponsiveContainer>
            </div>
        </div>
    );
}

function Pager({ page, totalPages, onPageChange }: { page: number; totalPages: number; onPageChange: (page: number) => void }) {
    return (
        <div className="flex items-center justify-between">
            <span className="text-sm opacity-70">Page {page} of {totalPages}</span>
            <div className="join">
                <button type="button" className="join-item btn btn-sm" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>Previous</button>
                <button type="button" className="join-item btn btn-sm" disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>Next</button>
            </div>
        </div>
    );
}

function DeviceTableContent({ customerId, deviceId }: { customerId: string; deviceId: string }) {
    const [page, setPage] = useState(1);
    const pageSize = 10;

    const { data, isLoading, isError } = appRouterClient.tableData.useQuery(
        { customerId, deviceId, page, pageSize },
        { placeholderData: keepPreviousData },
    );

    if (isLoading) {
        return (
            <div className="flex justify-center mt-6">
                <span className="loading loading-spinner loading-md" />
            </div>
        );
    }

    if (isError || !data) {
        return <p className="text-center mt-6 text-error">Failed to load table data.</p>;
    }

    return (
        <div className="card bg-base-200 shadow-sm mt-4">
            <div className="card-body">
                <h3 className="card-title text-base">Telemetry Events</h3>
                <Pager page={data.page} totalPages={data.totalPages} onPageChange={setPage} />
                <div className="overflow-x-auto">
                    <table className="table table-zebra">
                        <thead>
                            <tr>
                                <th>Event ID</th>
                                <th>Recorded At</th>
                                <th>Value</th>
                            </tr>
                        </thead>
                        <tbody>
                            {data.data.map((event) => (
                                <tr key={event.eventId}>
                                    <td className="text-xs opacity-70">{event.eventId}</td>
                                    <td>{new Date(event.recordedAt).toLocaleString()}</td>
                                    <td>{event.value}°C</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
                <Pager page={data.page} totalPages={data.totalPages} onPageChange={setPage} />
            </div>
        </div>
    );
}

export function DeviceDetails({ customerId, deviceId }: { customerId: string; deviceId: string }) {
    return (
        <ApiUnavailable>
            <AppProvider>
                <DeviceDetailsContent customerId={customerId} deviceId={deviceId} />
                <DeviceMetricsContent customerId={customerId} deviceId={deviceId} />
                <DeviceChartContent customerId={customerId} deviceId={deviceId} />
                <DeviceTableContent customerId={customerId} deviceId={deviceId} />
            </AppProvider>
        </ApiUnavailable>
    );
}
