"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { LogOut, FileText, FolderKanban, Settings, BarChart3, Award, Clock } from "lucide-react";
import { BlogList } from "@/components/admin/BlogList";
import { ProjectList } from "@/components/admin/ProjectList";
import { CertificationList } from "@/components/admin/CertificationList";
import { MyZenPanel } from "@/components/admin/MyZenPanel";
import { fetchAllBlogs, fetchAllProjects, fetchAllCertifications } from "@/lib/api/adminApi";
import { claimsOf, getUser, signIn, signOut } from "@/lib/auth";
import { isEnabled } from "@/lib/featureFlags";

interface UserRoles {
  hasAdminView: boolean;
  hasBlogsContributor: boolean;
  hasProjectsContributor: boolean;
  hasCertificationsContributor: boolean;
  hasMyZenOwner: boolean;
}

export default function AdminDashboard() {
  const router = useRouter();
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [userRoles, setUserRoles] = useState<UserRoles>({
    hasAdminView: false,
    hasBlogsContributor: false,
    hasProjectsContributor: false,
    hasCertificationsContributor: false,
    hasMyZenOwner: false,
  });
  const [myZenEnabled, setMyZenEnabled] = useState(false);
  const [username, setUsername] = useState<string>("");
  const [stats, setStats] = useState({ blogs: 0, projects: 0, certifications: 0 });

  // Fetch stats when authenticated
  useEffect(() => {
    const loadStats = async () => {
      try {
        const [blogs, projects, certifications] = await Promise.all([
          fetchAllBlogs(),
          fetchAllProjects(),
          fetchAllCertifications(),
        ]);
        setStats({
          blogs: blogs.length,
          projects: projects.length,
          certifications: certifications.length,
        });
      } catch (error) {
        console.error("Failed to load stats:", error);
      }
    };
    void loadStats();
    void isEnabled("MyZen").then(setMyZenEnabled);
  }, []);

  useEffect(() => {
    // Tokens live in memory (lib/auth.ts). Without a session, go through the
    // Keycloak login; while the Keycloak session lasts that is a quick redirect.
    let cancelled = false;
    const load = async () => {
      const user = await getUser();
      if (cancelled) return;
      if (!user) {
        await signIn("/admin/dashboard");
        return;
      }
      const { username, roles } = claimsOf(user);
      setUsername(username);
      setUserRoles({
        hasAdminView: roles.includes("Admin.View"),
        hasBlogsContributor: roles.includes("Blogs.Contributor"),
        hasProjectsContributor: roles.includes("Projects.Contributor"),
        hasCertificationsContributor: roles.includes("Certifications.Contributor"),
        hasMyZenOwner: roles.includes("MyZen.Owner"),
      });
      setIsAuthenticated(true);
    };
    load().catch((error: unknown) => {
      console.error("Sign-in failed:", error);
      router.push(`/admin?error=${encodeURIComponent("Sign-in is not available right now")}`);
    });
    return () => {
      cancelled = true;
    };
  }, [router]);

  const showMyZen = myZenEnabled && userRoles.hasMyZenOwner;

  const handleLogout = () => {
    void signOut();
  };

  if (!isAuthenticated) {
    return null; // or a loading spinner
  }

  // Check if user has any admin permissions
  const hasAnyPermission =
    userRoles.hasAdminView ||
    userRoles.hasBlogsContributor ||
    userRoles.hasProjectsContributor ||
    userRoles.hasCertificationsContributor;

  if (!hasAnyPermission) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-linear-to-br from-slate-50 to-slate-100 p-4 dark:from-slate-900 dark:to-slate-800">
        <Card className="w-full max-w-md shadow-xl">
          <CardHeader>
            <CardTitle className="text-center text-2xl">No Access</CardTitle>
            <CardDescription className="text-center">
              Your account does not have any admin permissions assigned.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-center text-sm text-muted-foreground">
              Please contact an administrator to request access to the admin panel. You will need
              one of the following roles:
            </p>
            <ul className="list-inside list-disc space-y-1 text-sm text-muted-foreground">
              <li>Admin.View - Access admin area</li>
              <li>Blogs.Contributor - Manage blog posts</li>
              <li>Projects.Contributor - Manage projects</li>
              <li>Certifications.Contributor - Manage certifications</li>
            </ul>
            <Button onClick={handleLogout} className="w-full">
              <LogOut className="mr-2 h-4 w-4" />
              Return to Login
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-linear-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800">
      {/* Header */}
      <header className="border-b bg-white/80 backdrop-blur-xs dark:bg-slate-900/80">
        <div className="container mx-auto flex items-center justify-between px-4 py-4">
          <div>
            <h1 className="text-2xl font-bold">Clercq.It Admin</h1>
            <p className="text-sm text-muted-foreground">
              Content Management System {username && `• Welcome, ${username}`}
            </p>
          </div>
          <Button variant="outline" onClick={handleLogout} className="flex items-center gap-2">
            <LogOut className="h-4 w-4" />
            Logout
          </Button>
        </div>
      </header>

      {/* Main Content */}
      <main className="container mx-auto px-4 py-8">
        <Tabs defaultValue="overview" className="space-y-6">
          <TabsList
            className="grid w-full lg:w-auto"
            style={{
              gridTemplateColumns: `repeat(${[userRoles.hasAdminView, userRoles.hasBlogsContributor, userRoles.hasProjectsContributor, userRoles.hasCertificationsContributor, showMyZen, userRoles.hasAdminView].filter(Boolean).length}, 1fr)`,
            }}
          >
            {userRoles.hasAdminView && (
              <TabsTrigger value="overview">
                <BarChart3 className="mr-2 h-4 w-4" />
                Overview
              </TabsTrigger>
            )}
            {userRoles.hasBlogsContributor && (
              <TabsTrigger value="blogs">
                <FileText className="mr-2 h-4 w-4" />
                Blogs
              </TabsTrigger>
            )}
            {userRoles.hasProjectsContributor && (
              <TabsTrigger value="projects">
                <FolderKanban className="mr-2 h-4 w-4" />
                Projects
              </TabsTrigger>
            )}
            {userRoles.hasCertificationsContributor && (
              <TabsTrigger value="certifications">
                <Award className="mr-2 h-4 w-4" />
                Certifications
              </TabsTrigger>
            )}
            {showMyZen && (
              <TabsTrigger value="myzen">
                <Clock className="mr-2 h-4 w-4" />
                MyZen
              </TabsTrigger>
            )}
            {userRoles.hasAdminView && (
              <TabsTrigger value="settings">
                <Settings className="mr-2 h-4 w-4" />
                Settings
              </TabsTrigger>
            )}
          </TabsList>

          {/* Overview Tab */}
          {userRoles.hasAdminView && (
            <TabsContent value="overview" className="space-y-6">
              <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
                <Card>
                  <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle className="text-sm font-medium">Total Blogs</CardTitle>
                    <FileText className="h-4 w-4 text-muted-foreground" />
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold">{stats.blogs}</div>
                    <p className="text-xs text-muted-foreground">Published blog posts</p>
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle className="text-sm font-medium">Total Projects</CardTitle>
                    <FolderKanban className="h-4 w-4 text-muted-foreground" />
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold">{stats.projects}</div>
                    <p className="text-xs text-muted-foreground">Portfolio projects</p>
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle className="text-sm font-medium">Certifications</CardTitle>
                    <Award className="h-4 w-4 text-muted-foreground" />
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold">{stats.certifications}</div>
                    <p className="text-xs text-muted-foreground">Professional certifications</p>
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle className="text-sm font-medium">System Status</CardTitle>
                    <Settings className="h-4 w-4 text-muted-foreground" />
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-green-600">Healthy</div>
                    <p className="text-xs text-muted-foreground">All systems operational</p>
                  </CardContent>
                </Card>
              </div>

              <Card>
                <CardHeader>
                  <CardTitle>Welcome to the Admin Panel</CardTitle>
                  <CardDescription>
                    Manage your content, projects, and settings from this dashboard.
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <p className="text-sm text-muted-foreground">
                    Use the tabs above to navigate between different sections:
                  </p>
                  <ul className="list-inside list-disc space-y-2 text-sm text-muted-foreground">
                    <li>
                      <strong>Blogs:</strong> Create, edit, and manage blog posts
                    </li>
                    <li>
                      <strong>Projects:</strong> Manage your portfolio projects
                    </li>
                    <li>
                      <strong>Certifications:</strong> Showcase your professional certifications
                    </li>
                    <li>
                      <strong>Settings:</strong> Configure system settings and preferences
                    </li>
                  </ul>
                </CardContent>
              </Card>
            </TabsContent>
          )}

          {/* Blogs Tab */}
          {userRoles.hasBlogsContributor && (
            <TabsContent value="blogs" className="space-y-6">
              <Card>
                <CardHeader>
                  <CardTitle>Blog Management</CardTitle>
                  <CardDescription>Create and manage your blog posts</CardDescription>
                </CardHeader>
                <CardContent>
                  <BlogList />
                </CardContent>
              </Card>
            </TabsContent>
          )}

          {/* Projects Tab */}
          {userRoles.hasProjectsContributor && (
            <TabsContent value="projects" className="space-y-6">
              <Card>
                <CardHeader>
                  <CardTitle>Project Management</CardTitle>
                  <CardDescription>Manage your portfolio projects</CardDescription>
                </CardHeader>
                <CardContent>
                  <ProjectList />
                </CardContent>
              </Card>
            </TabsContent>
          )}

          {/* Certifications Tab */}
          {userRoles.hasCertificationsContributor && (
            <TabsContent value="certifications" className="space-y-6">
              <Card>
                <CardHeader>
                  <CardTitle>Certification Management</CardTitle>
                  <CardDescription>Manage your professional certifications</CardDescription>
                </CardHeader>
                <CardContent>
                  <CertificationList />
                </CardContent>
              </Card>
            </TabsContent>
          )}

          {/* MyZen Tab: the owner's own MyZen account (docs/myzen.md) */}
          {showMyZen && (
            <TabsContent value="myzen" className="space-y-6">
              <Card>
                <CardHeader>
                  <CardTitle>MyZen</CardTitle>
                  <CardDescription>
                    Connect your MyZen account and see this month&apos;s time entries
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <MyZenPanel />
                </CardContent>
              </Card>
            </TabsContent>
          )}

          {/* Settings Tab */}
          {userRoles.hasAdminView && (
            <TabsContent value="settings" className="space-y-6">
              <Card>
                <CardHeader>
                  <CardTitle>System Settings</CardTitle>
                  <CardDescription>Configure your admin panel settings</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="space-y-2">
                    <h4 className="text-sm font-medium">Authentication</h4>
                    <p className="text-sm text-muted-foreground">
                      Cloud IAM (KeyCloak) Identity as a Service active
                    </p>
                  </div>
                  <div className="space-y-2">
                    <h4 className="text-sm font-medium">Your Roles</h4>
                    <p className="text-sm text-muted-foreground">
                      {userRoles.hasAdminView && "Admin.View "}
                      {userRoles.hasBlogsContributor && "Blogs.Contributor "}
                      {userRoles.hasProjectsContributor && "Projects.Contributor "}
                      {userRoles.hasCertificationsContributor && "Certifications.Contributor "}
                      {userRoles.hasMyZenOwner && "MyZen.Owner "}
                    </p>
                  </div>
                  <div className="space-y-2">
                    <h4 className="text-sm font-medium">Storage</h4>
                    <p className="text-sm text-muted-foreground">
                      Connected to Scaleway Object Storage
                    </p>
                  </div>
                  <div className="space-y-2">
                    <h4 className="text-sm font-medium">API Status</h4>
                    <p className="text-sm text-muted-foreground">All endpoints operational</p>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>
          )}
        </Tabs>
      </main>
    </div>
  );
}
