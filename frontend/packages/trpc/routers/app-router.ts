import type { Customer } from "@telemetryslice/domain/customer";
import type { Device } from "@telemetryslice/domain/device";
import type { DeviceChartItem } from "@telemetryslice/domain/device-chart";
import type { DeviceMetrics } from "@telemetryslice/domain/device-metrics";
import type { TelemetryEvent } from "@telemetryslice/domain/telemetry-event";
import { z } from "zod";
import { publicProcedure, router } from "../trpc";

export const appRouter = router({
    customers: publicProcedure.query(async (): Promise<Customer[]> => {
        const res = await fetch(`${process.env.NEXT_APP_API_URL}/Customers`, {
            headers: { accept: "text/plain" },
        });
        if (!res.ok) throw new Error(`Request failed with status ${res.status}`);
        return res.json();
    }),
    devicesByCustomer: publicProcedure
        .input(z.object({ customerId: z.string() }))
        .query(async ({ input }): Promise<Device[]> => {
            const res = await fetch(`${process.env.NEXT_APP_API_URL}/Devices/${input.customerId}`, {
                headers: { accept: "text/plain" },
            });
            if (!res.ok) throw new Error(`Request failed with status ${res.status}`);
        return res.json();
        }),
    device: publicProcedure
        .input(z.object({ customerId: z.string(), deviceId: z.string() }))
        .query(async ({ input }): Promise<Device> => {
            const res = await fetch(`${process.env.NEXT_APP_API_URL}/Devices/${input.customerId}/${input.deviceId}`, {
                headers: { accept: "text/plain" },
            });
            if (!res.ok) throw new Error(`Request failed with status ${res.status}`);
        return res.json();
        }),
    deviceMetrics: publicProcedure
        .input(z.object({ customerId: z.string(), deviceId: z.string() }))
        .query(async ({ input }): Promise<DeviceMetrics> => {
            const res = await fetch(`${process.env.NEXT_APP_API_URL}/Sensors/${input.customerId}/${input.deviceId}/metrics`, {
                headers: { accept: "text/plain" },
            });
            if (!res.ok) throw new Error(`Request failed with status ${res.status}`);
        return res.json();
        }),
    deviceChart: publicProcedure
        .input(z.object({ customerId: z.string(), deviceId: z.string() }))
        .query(async ({ input }): Promise<DeviceChartItem[]> => {
            const res = await fetch(`${process.env.NEXT_APP_API_URL}/Sensors/${input.customerId}/${input.deviceId}/chart`, {
                headers: { accept: "text/plain" },
            });
            if (!res.ok) throw new Error(`Request failed with status ${res.status}`);
        return res.json();
        }),
    tableData: publicProcedure
        .input(z.object({ customerId: z.string(), deviceId: z.string(), page: z.number(), pageSize: z.number() }))
        .query(async ({ input }): Promise<{ data: TelemetryEvent[]; page: number; totalPages: number; totalCount: number }> => {
            const res = await fetch(`${process.env.NEXT_APP_API_URL}/Sensors/${input.customerId}/${input.deviceId}/table?page=${input.page}&pageSize=${input.pageSize}`, {
                headers: { accept: "text/plain" },
            });
            if (!res.ok) throw new Error(`Request failed with status ${res.status}`);
        return res.json();
        }),
});

export type AppRouter = typeof appRouter;
