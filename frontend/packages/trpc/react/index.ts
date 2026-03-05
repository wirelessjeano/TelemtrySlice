import { createTRPCReact } from "@trpc/react-query";

import type { AdminRouter } from "../routers/types";
import type { AppRouter } from "../routers/types";

export const adminRouterClient = createTRPCReact<AdminRouter>({});
export const appRouterClient = createTRPCReact<AppRouter>({});


