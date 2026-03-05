export type HealthStatus = "Healthy" | "Unhealthy" | "Degraded";

export interface HealthCheck {
  name: string;
  status: HealthStatus;
  duration: string;
  description: string | null;
  exception: string | null;
}

export interface HealthReport {
  status: HealthStatus;
  totalDuration: string;
  checks: HealthCheck[];
}
