import { DeviceDetails } from "@/app/_controls/DeviceDetails";
import { FullFooter } from "@/app/_controls/FullFooter";
import NavBar from "@/app/_controls/NavBar";

export default async function DevicePage({ params }: { params: Promise<{ customerId: string; deviceId: string }> }) {
  const { customerId, deviceId } = await params;

  return (
    <div className="flex flex-col min-h-screen">
      <NavBar />
      <main className="mx-auto mt-5 container pb-8">
        <DeviceDetails customerId={customerId} deviceId={deviceId} />
      </main>
      <FullFooter />
    </div>
  );
}
