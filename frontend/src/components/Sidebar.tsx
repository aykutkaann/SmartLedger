import { useState } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import {
  TrendingUp,
  LayoutDashboard,
  CreditCard,
  ArrowLeftRight,
  History,
  LogOut,
  Menu,
  X,
  ShieldAlert,
  ChevronRight,
} from 'lucide-react';
import { useAuthStore } from '../store/authStore';
import toast from 'react-hot-toast';

interface NavItem {
  to: string;
  label: string;
  icon: React.ReactNode;
  adminOnly?: boolean;
}

const navItems: NavItem[] = [
  { to: '/dashboard', label: 'Dashboard', icon: <LayoutDashboard className="w-5 h-5" /> },
  { to: '/accounts', label: 'Accounts', icon: <CreditCard className="w-5 h-5" /> },
  { to: '/transfer', label: 'Transfer', icon: <ArrowLeftRight className="w-5 h-5" /> },
  { to: '/transactions', label: 'Transactions', icon: <History className="w-5 h-5" /> },
  { to: '/admin/fraud-queue', label: 'Fraud Queue', icon: <ShieldAlert className="w-5 h-5" />, adminOnly: true },
];

export default function Sidebar() {
  const { email, role, logout } = useAuthStore();
  const navigate = useNavigate();
  const [mobileOpen, setMobileOpen] = useState(false);

  const handleLogout = () => {
    logout();
    toast.success('Signed out');
    navigate('/login');
  };

  const filtered = navItems.filter((item) => !item.adminOnly || role === 'Admin');

  const SidebarContent = () => (
    <div className="flex flex-col h-full">
      {/* Logo */}
      <div className="flex items-center gap-3 px-6 py-6 border-b border-white/10">
        <div className="flex items-center justify-center w-9 h-9 rounded-xl bg-blue-600 shadow-lg shadow-blue-600/30 flex-shrink-0">
          <TrendingUp className="w-5 h-5 text-white" />
        </div>
        <div>
          <p className="text-white font-bold text-lg leading-none">SmartLedger</p>
          <p className="text-slate-400 text-xs mt-0.5">Banking Platform</p>
        </div>
      </div>

      {/* Nav */}
      <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
        {filtered.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            onClick={() => setMobileOpen(false)}
            className={({ isActive }) =>
              `flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-all group ${
                isActive
                  ? 'bg-blue-600 text-white shadow-lg shadow-blue-600/20'
                  : 'text-slate-400 hover:text-white hover:bg-white/5'
              }`
            }
          >
            {item.icon}
            <span className="flex-1">{item.label}</span>
            <ChevronRight className="w-4 h-4 opacity-0 group-hover:opacity-50 transition" />
          </NavLink>
        ))}
      </nav>

      {/* User */}
      <div className="px-3 pb-4 border-t border-white/10 pt-4">
        <div className="flex items-center gap-3 px-3 py-2 mb-2">
          <div className="w-8 h-8 rounded-full bg-blue-600 flex items-center justify-center flex-shrink-0">
            <span className="text-white text-xs font-bold uppercase">
              {email?.[0] ?? '?'}
            </span>
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-white text-sm font-medium truncate">{email}</p>
            <p className="text-slate-400 text-xs capitalize">{role ?? 'User'}</p>
          </div>
        </div>
        <button
          onClick={handleLogout}
          className="flex items-center gap-3 w-full px-3 py-2.5 rounded-xl text-sm font-medium text-slate-400 hover:text-white hover:bg-red-500/10 hover:text-red-400 transition"
        >
          <LogOut className="w-5 h-5" />
          Sign out
        </button>
      </div>
    </div>
  );

  return (
    <>
      {/* Mobile toggle */}
      <button
        className="lg:hidden fixed top-4 left-4 z-50 w-10 h-10 flex items-center justify-center rounded-xl bg-slate-800 text-white shadow-lg"
        onClick={() => setMobileOpen(!mobileOpen)}
      >
        {mobileOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
      </button>

      {/* Mobile overlay */}
      {mobileOpen && (
        <div
          className="lg:hidden fixed inset-0 bg-black/60 z-40 backdrop-blur-sm"
          onClick={() => setMobileOpen(false)}
        />
      )}

      {/* Mobile sidebar */}
      <aside
        className={`lg:hidden fixed top-0 left-0 h-full w-72 bg-slate-900 border-r border-white/10 z-40 transform transition-transform duration-300 ${
          mobileOpen ? 'translate-x-0' : '-translate-x-full'
        }`}
      >
        <SidebarContent />
      </aside>

      {/* Desktop sidebar */}
      <aside className="hidden lg:flex flex-col w-64 bg-slate-900 border-r border-white/10 h-screen sticky top-0 flex-shrink-0">
        <SidebarContent />
      </aside>
    </>
  );
}
