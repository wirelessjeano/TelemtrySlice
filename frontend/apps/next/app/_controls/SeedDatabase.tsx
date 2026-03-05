"use client"

import { adminRouterClient } from "@telemetryslice/trpc/react";
import AdminProvider from "../_providers/AdminProvider";

function SeedDatabaseButton({ onSeeded }: { onSeeded?: () => void }) {
    const seed = adminRouterClient.seed.useMutation({
        onSuccess: () => onSeeded?.(),
    });

    return (
        <div className="flex flex-col items-center justify-center gap-4 py-12">
            <p className="text-lg opacity-70">No data found. Seed the database to get started.</p>
            <button
                type="button"
                className="btn btn-primary"
                disabled={seed.isPending}
                onClick={() => seed.mutate({ intervalSeconds: 30 })}
            >
                {seed.isPending && <span className="loading loading-spinner loading-sm" />}
                Seed database
            </button>
            {seed.isError && (
                <p className="text-error text-sm">Failed to seed database. Please try again.</p>
            )}
        </div>
    );
}

export function SeedDatabase({ onSeeded }: { onSeeded?: () => void }) {
    return (
        <AdminProvider>
            <SeedDatabaseButton onSeeded={onSeeded} />
        </AdminProvider>
    );
}
