import type { Customer } from "@telemetryslice/domain/customer";

interface CustomerSelectProps {
    customers?: Customer[];
    isLoading: boolean;
    value: string;
    onChange: (customerId: string) => void;
}

export function CustomerSelect({ customers, isLoading, value, onChange }: CustomerSelectProps) {
    return (
        <select
            className="select select-bordered select-lg w-full max-w-xs"
            value={value}
            onChange={(e) => onChange(e.target.value)}
            disabled={isLoading}
        >
            <option value="" disabled>
                {isLoading ? "Loading customers..." : "Select a customer"}
            </option>
            {customers?.map((customer) => (
                <option key={customer.customerId} value={customer.customerId}>
                    {customer.customerId}
                </option>
            ))}
        </select>
    );
}
