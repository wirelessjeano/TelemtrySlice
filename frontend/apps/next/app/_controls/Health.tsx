"use client"

import type { HealthReport, HealthStatus } from "@telemetryslice/domain/health";
import { adminRouterClient } from "@telemetryslice/trpc/react";
import { keepPreviousData } from "@tanstack/react-query";
import AdminProvider from "../_providers/AdminProvider";

function StatusDot({ status }: { status: HealthStatus }) {
    return (
        <span
            className={`inline-block h-2.5 w-2.5 rounded-full ${status === "Healthy" ? "bg-success" : "bg-error"}`}
        />
    );
}

function HealthDropdown({ label, report, isError, isLoading }: { label: string; report?: HealthReport; isError: boolean; isLoading: boolean }) {
    return (
        <details className="dropdown dropdown-end">
            <summary className="btn btn-ghost btn-sm gap-2">
                <StatusDot status={!isLoading && !isError && report ? report.status : "Unhealthy"} />
                {label}
                <svg className="h-3 w-3 fill-current" viewBox="0 0 20 20"><path d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" /></svg>
            </summary>
            <ul className="dropdown-content menu bg-base-200 rounded-box z-10 w-52 p-2 shadow-sm border border-base-300">
                {isError && (
                    <li><span className="flex items-center gap-2 text-error">Unreachable</span></li>
                )}
                {report?.checks.map((check) => (
                    <li key={check.name}>
                        <span className="flex items-center gap-2">
                            <StatusDot status={isError ? "Unhealthy" : check.status} />
                            {check.name}
                        </span>
                    </li>
                ))}
            </ul>
        </details>
    );
}

function HealthContent() {
    const { data: apiHealth, isError: apiError, isLoading: apiLoading } = adminRouterClient.apiHealth.useQuery(undefined, { refetchInterval: 10000, placeholderData: keepPreviousData });
    const { data: writerHealth, isError: writerError, isLoading: writerLoading } = adminRouterClient.writerHealth.useQuery(undefined, { refetchInterval: 10000, placeholderData: keepPreviousData });

    return (
        <div className="flex items-center justify-end gap-2">
            <HealthDropdown label="API" report={apiHealth} isError={apiError} isLoading={apiLoading} />
            <HealthDropdown label="Writer" report={writerHealth} isError={writerError} isLoading={writerLoading} />
        </div>
    );
}

export function Health() {
    return (
        <AdminProvider>
            <HealthContent />
        </AdminProvider>
    );
}
