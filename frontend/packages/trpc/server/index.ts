
export { adminRouter } from "../routers/admin-router";
export { appRouter } from "../routers/app-router";

export type { AdminRouter, AppRouter } from "../routers/types";

import { adminRouter } from "../routers/admin-router";
import { appRouter } from "../routers/app-router";

export const adminRouterServerClient = adminRouter.createCaller({});
export const appRouterServerClient = appRouter.createCaller({});
