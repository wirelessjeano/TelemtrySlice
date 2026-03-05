import { appRouter } from "@telemetryslice/trpc/server";
import { fetchRequestHandler } from "@trpc/server/adapters/fetch";

export const dynamic = 'force-dynamic';

const handler = (req: Request) =>
    fetchRequestHandler({
        endpoint: "/api/app",
        req,
        router: appRouter,
        createContext: () => ({})
    });

export { handler as GET, handler as POST };
