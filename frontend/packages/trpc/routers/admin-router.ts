import type { HealthReport } from "@telemetryslice/domain/health";
import { z } from "zod";
import { publicProcedure, router } from "../trpc";

export const adminRouter = router({
    apiHealth: publicProcedure.query(async (): Promise<HealthReport> => {
        const res = await fetch(`${process.env.NEXT_APP_API_URL}/Health`);
        if (!res.ok) throw new Error(`Request failed with status ${res.status}`);
        return res.json();
    }),
    writerHealth: publicProcedure.query(async (): Promise<HealthReport> => {
        const res = await fetch(`${process.env.NEXT_APP_WRITER_URL}/Health`);
        if (!res.ok) throw new Error(`Request failed with status ${res.status}`);
        return res.json();
    }),
    seed: publicProcedure
        .input(z.object({ intervalSeconds: z.number() }))
        .mutation(async ({ input }) => {
            const res = await fetch(
                `${process.env.NEXT_APP_API_URL}/Admin/seed?intervalSeconds=${input.intervalSeconds}`,
                { method: "POST" },
            );
            if (!res.ok) throw new Error(`Request failed with status ${res.status}`);
        }),
});

export type AdminRouter = typeof adminRouter;
