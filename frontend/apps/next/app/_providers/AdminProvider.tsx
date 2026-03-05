"use client"

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { httpBatchLink } from "@trpc/client";

import { adminRouterClient } from "@telemetryslice/trpc/react";
import { useState } from "react";

export default function AdminProvider({ children }: { children: React.ReactNode }) {
    const [queryClient] = useState(() => new QueryClient({}));
    const [adminTrpcClient] = useState(() => adminRouterClient.createClient({
        links: [
            httpBatchLink({
                url: `${typeof window !== 'undefined' ? window.location.origin : ''}/api/admin`
            }),
        ]
    }));

    return (
        <adminRouterClient.Provider client={adminTrpcClient} queryClient={queryClient}>
            <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
        </adminRouterClient.Provider>
    )
}
