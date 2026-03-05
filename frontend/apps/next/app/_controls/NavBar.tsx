
import Link from "next/link";

import React from "react";
import { Health } from "./Health";

export default async function NavBar() {
    return <div className="drawer">
        <input id="my-drawer-2" type="checkbox" className="drawer-toggle" />
        <div className="drawer-content flex flex-col">
            <div className="border-b border-base-content/20 pt-0">
                <div className="navbar bg-base-100 container mx-auto py-0 min-h-12 md:py-2 md:min-h-16">
                    <div className="mx-2 flex-1  items-center">
                        <div className="flex items-center">
                            <Link href={"/"}>TelemetrySlice UI</Link>
                        </div>
                    </div>
                    <div className="flex gap-2 items-center">
                        <Health />
                    </div>
                </div>
            </div>
        </div>

    </div>

}
