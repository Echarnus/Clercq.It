import { AdminSectionView } from "@/components/admin/AdminSectionView";

// One URL per admin section, e.g. /admin/dashboard/blogs (issue #449). The keys come from
// ADMIN_NAV in components/admin/admin-sections.tsx; unknown keys show "Section not found".
export default async function AdminSectionPage({
  params,
}: {
  params: Promise<{ section: string }>;
}) {
  const { section } = await params;
  return <AdminSectionView sectionKey={section} />;
}
