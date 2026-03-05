"use client"

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { httpBatchLink } from "@trpc/client";

import { appRouterClient } from "@telemetryslice/trpc/react";
import { useState } from "react";

export default function AppProvider({ children }: { children: React.ReactNode }) {
    const [queryClient] = useState(() => new QueryClient({}));
    const [appTrpcClient] = useState(() => appRouterClient.createClient({
        links: [
            httpBatchLink({
                url: `${typeof window !== 'undefined' ? window.location.origin : ''}/api/app`
            }),
        ]
    }));

    return (
        <appRouterClient.Provider client={appTrpcClient} queryClient={queryClient}>
            <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
        </appRouterClient.Provider>
    )
}
