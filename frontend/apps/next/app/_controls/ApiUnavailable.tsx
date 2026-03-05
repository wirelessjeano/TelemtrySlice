"use client"

import { adminRouterClient } from "@telemetryslice/trpc/react";
import AdminProvider from "../_providers/AdminProvider";

function ApiUnavailableContent({ children }: { children: React.ReactNode }) {
    const { isError, isLoading } = adminRouterClient.apiHealth.useQuery(undefined, {
        refetchInterval: 5000,
    });

    if (isLoading) {
        return (
            <div className="flex justify-center mt-6">
                <span className="loading loading-spinner loading-md" />
            </div>
        );
    }

    if (isError) {
        return (
            <div className="flex flex-col items-center justify-center gap-2 py-12">
                <div className="flex items-center gap-2">
                    <span className="inline-block h-4 w-4 rounded-full bg-error" />
                    <p className="text-lg font-semibold">API Unavailable</p>
                </div>
                <p className="text-sm opacity-70">The API is currently unreachable. This will update automatically when it comes back online.</p>
            </div>
        );
    }

    return <>{children}</>;
}

export function ApiUnavailable({ children }: { children: React.ReactNode }) {
    return (
        <AdminProvider>
            <ApiUnavailableContent>{children}</ApiUnavailableContent>
        </AdminProvider>
    );
}
