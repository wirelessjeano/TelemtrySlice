import { CustomerDevices } from "./_controls/CustomerDevices";
import { FullFooter } from "./_controls/FullFooter";
import NavBar from "./_controls/NavBar";

export default function Home() {
  return (
    <div className="flex flex-col min-h-screen">
      <NavBar />
      <main className="mx-auto mt-5 container">
        <CustomerDevices />
      </main>
      <FullFooter />
    </div>
  );
}
