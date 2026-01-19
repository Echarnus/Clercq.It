"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import { createPortal } from "react-dom";
import { Menu, X } from "lucide-react";

const navItems = [
  { href: "/", label: "Home", key: "home" },
  { href: "/portfolio", label: "Portfolio", key: "portfolio" },
  { href: "/blogs", label: "Blogs", key: "blogs" },
  { href: "/certifications", label: "Certifications", key: "certifications" },
];

export function Navigation() {
  const pathname = usePathname();
  const [isHydrated, setIsHydrated] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  useEffect(() => {
    setIsHydrated(true);
  }, []);

  // Close mobile menu when route changes
  useEffect(() => {
    setIsMobileMenuOpen(false);
  }, [pathname]);

  // Prevent body scroll when mobile menu is open
  useEffect(() => {
    if (isMobileMenuOpen) {
      document.body.style.overflow = "hidden";
    } else {
      document.body.style.overflow = "unset";
    }
    return () => {
      document.body.style.overflow = "unset";
    };
  }, [isMobileMenuOpen]);

  const getCurrentPage = () => {
    if (!isHydrated) return null;
    const item = navItems.find((item) => item.href === pathname);
    return item?.key || null;
  };

  const currentPage = getCurrentPage();

  const getLinkClasses = (key: string, isMobile: boolean = false) => {
    const isActive = currentPage === key;
    if (isMobile) {
      return isActive
        ? "block py-3 px-4 text-lg font-medium text-white bg-slate-700 rounded-lg"
        : "block py-3 px-4 text-lg text-slate-300 hover:text-white hover:bg-slate-700/50 rounded-lg transition-colors";
    }
    return isActive
      ? "text-slate-900 font-medium border-b-2 border-slate-900 dark:text-white dark:border-white"
      : "text-slate-600 hover:text-slate-900 transition-colors hover:border-b-2 hover:border-slate-600 dark:text-slate-400 dark:hover:text-white dark:hover:border-slate-400";
  };

  const mobileMenu = isHydrated ? createPortal(
    <>
      {/* Mobile Sidebar Overlay */}
      <div
        className={`fixed inset-0 bg-black/60 z-[9998] md:hidden transition-opacity duration-300 ${
          isMobileMenuOpen ? "opacity-100" : "opacity-0 pointer-events-none"
        }`}
        onClick={() => setIsMobileMenuOpen(false)}
      />

      {/* Mobile Sidebar */}
      <div
        className={`fixed top-0 right-0 bottom-0 w-72 z-[9999] transform transition-transform duration-300 ease-in-out md:hidden ${
          isMobileMenuOpen ? "translate-x-0" : "translate-x-full"
        }`}
        style={{
          backgroundColor: "#0f172a",
          boxShadow: "-4px 0 20px rgba(0, 0, 0, 0.5)"
        }}
      >
        <div
          className="flex justify-between items-center p-4 border-b border-slate-700"
          style={{ backgroundColor: "#1e293b" }}
        >
          <span className="text-white font-semibold">Menu</span>
          <button
            onClick={() => setIsMobileMenuOpen(false)}
            className="p-2 text-slate-400 hover:text-white"
            aria-label="Close menu"
          >
            <X className="w-6 h-6" />
          </button>
        </div>
        <nav
          className="p-4 space-y-2 h-full"
          style={{ backgroundColor: "#0f172a" }}
        >
          {navItems.map((item) => (
            <Link
              key={item.key}
              href={item.href}
              className={getLinkClasses(item.key, true)}
              onClick={() => setIsMobileMenuOpen(false)}
            >
              {item.label}
            </Link>
          ))}
        </nav>
      </div>
    </>,
    document.body
  ) : null;

  return (
    <>
      {/* Desktop Navigation */}
      <nav className="hidden md:flex gap-6">
        {navItems.map((item) => (
          <Link key={item.key} href={item.href} className={getLinkClasses(item.key)}>
            {item.label}
          </Link>
        ))}
      </nav>

      {/* Mobile Menu Button */}
      <button
        className="md:hidden p-2 text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-white"
        onClick={() => setIsMobileMenuOpen(true)}
        aria-label="Open menu"
      >
        <Menu className="w-6 h-6" />
      </button>

      {mobileMenu}
    </>
  );
}
