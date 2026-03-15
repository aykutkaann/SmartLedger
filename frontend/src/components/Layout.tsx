import Sidebar from './Sidebar';
import { Outlet } from 'react-router-dom';

export default function Layout() {
  return (
    <div className="flex min-h-screen bg-slate-950">
      <Sidebar />
      <main className="flex-1 overflow-y-auto">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-8 lg:py-10 pt-16 lg:pt-10">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
