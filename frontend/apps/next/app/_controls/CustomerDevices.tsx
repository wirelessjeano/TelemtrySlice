"use client"

import { appRouterClient } from "@telemetryslice/trpc/react";
import Link from "next/link";
import { useState } from "react";
import { WiThermometer } from "react-icons/wi";
import AppProvider from "../_providers/AppProvider";
import { ApiUnavailable } from "./ApiUnavailable";
import { CustomerSelect } from "./CustomerSelect";
import { SeedDatabase } from "./SeedDatabase";

function CustomerDevicesContent() {
    const utils = appRouterClient.useUtils();
    const { data: customers, isLoading } = appRouterClient.customers.useQuery();
    const [selectedCustomerId, setSelectedCustomerId] = useState<string>("");

    const { data: devices, isLoading: devicesLoading } = appRouterClient.devicesByCustomer.useQuery(
        { customerId: selectedCustomerId },
        { enabled: !!selectedCustomerId },
    );

    if (isLoading) {
        return (
            <div className="flex justify-center mt-6">
                <span className="loading loading-spinner loading-md" />
            </div>
        );
    }

    if (customers && customers.length === 0) {
        return <SeedDatabase onSeeded={() => utils.customers.invalidate()} />;
    }

    return (
        <div>
            <div className="flex justify-end">
                <CustomerSelect
                    customers={customers}
                    isLoading={false}
                    value={selectedCustomerId}
                    onChange={setSelectedCustomerId}
                />
            </div>

            {devicesLoading && (
                <div className="flex justify-center mt-6">
                    <span className="loading loading-spinner loading-md" />
                </div>
            )}

            {devices && devices.length > 0 && (
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 mt-6">
                    {devices.map((device) => (
                        <Link key={device.deviceId} href={`/device/${device.customerId}/${device.deviceId}`} className="card bg-base-200 shadow-sm flex-row hover:bg-base-300 transition-colors">
                            <div className="flex items-center pl-4">
                                <WiThermometer className="h-14 w-14 opacity-50" />
                            </div>
                            <div className="card-body">
                                <h3 className="card-title text-base">{device.label}</h3>
                                <p className="text-sm opacity-70">{device.deviceId}</p>
                                <p className="text-sm">{device.location}</p>
                            </div>
                        </Link>
                    ))}
                </div>
            )}

            {devices && devices.length === 0 && (
                <p className="text-center mt-6 opacity-70">No devices found for this customer.</p>
            )}
        </div>
    );
}

export function CustomerDevices() {
    return (
        <ApiUnavailable>
            <AppProvider>
                <CustomerDevicesContent />
            </AppProvider>
        </ApiUnavailable>
    );
}
