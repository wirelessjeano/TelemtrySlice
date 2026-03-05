export interface TelemetryEvent {
  eventId: string;
  deviceId: string;
  customerId: string;
  recordedAt: string;
  value: number;
}
