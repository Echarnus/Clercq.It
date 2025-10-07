"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  LogOut,
  FileText,
  FolderKanban,
  Settings,
  BarChart3,
  Image as ImageIcon,
  Award,
} from "lucide-react";

interface UserRoles {
  hasAdminView: boolean;
  hasBlogsContributor: boolean;
  hasProjectsContributor: boolean;
  hasCertificationsContributor: boolean;
}

export default function AdminDashboard() {
  const router = useRouter();
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [userRoles, setUserRoles] = useState<UserRoles>({
    hasAdminView: false,
    hasBlogsContributor: false,
    hasProjectsContributor: false,
    hasCertificationsContributor: false,
  });
  const [username, setUsername] = useState<string>("");

  useEffect(() => {
    // Check if user is authenticated
    const token = localStorage.getItem("admin_token");
    const userDataStr = localStorage.getItem("admin_user");
    
    if (!token) {
      router.push("/admin");
    } else {
      setIsAuthenticated(true);
      
      // Parse user roles from stored user data
      if (userDataStr) {
        try {
          const userData = JSON.parse(userDataStr);
          setUsername(userData.username || "");
          
          const roles = userData.roles || [];
          setUserRoles({
            hasAdminView: roles.includes("Admin.View"),
            hasBlogsContributor: roles.includes("Blogs.Contributor"),
            hasProjectsContributor: roles.includes("Projects.Contributor"),
            hasCertificationsContributor: roles.includes("Certifications.Contributor"),
          });
        } catch (e) {
          console.error("Failed to parse user data:", e);
        }
      }
    }
  }, [router]);

  const handleLogout = () => {
    localStorage.removeItem("admin_token");
    localStorage.removeItem("admin_user");
    router.push("/admin");
  };

  if (!isAuthenticated) {
    return null; // or a loading spinner
  }

  // Check if user has any admin permissions
  const hasAnyPermission = userRoles.hasAdminView || userRoles.hasBlogsContributor || userRoles.hasProjectsContributor || userRoles.hasCertificationsContributor;

  if (!hasAnyPermission) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800 p-4">
        <Card className="w-full max-w-md shadow-xl">
          <CardHeader>
            <CardTitle className="text-2xl text-center">No Access</CardTitle>
            <CardDescription className="text-center">
              Your account does not have any admin permissions assigned.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-sm text-muted-foreground text-center">
              Please contact an administrator to request access to the admin panel.
              You will need one of the following roles:
            </p>
            <ul className="text-sm list-disc list-inside space-y-1 text-muted-foreground">
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
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800">
      {/* Header */}
      <header className="border-b bg-white/80 backdrop-blur-sm dark:bg-slate-900/80">
        <div className="container mx-auto px-4 py-4 flex justify-between items-center">
          <div>
            <h1 className="text-2xl font-bold">Clercq.It Admin</h1>
            <p className="text-sm text-muted-foreground">
              Content Management System {username && `• Welcome, ${username}`}
            </p>
          </div>
          <Button
            variant="outline"
            onClick={handleLogout}
            className="flex items-center gap-2"
          >
            <LogOut className="h-4 w-4" />
            Logout
          </Button>
        </div>
      </header>

      {/* Main Content */}
      <main className="container mx-auto px-4 py-8">
        <Tabs defaultValue="overview" className="space-y-6">
          <TabsList className="grid w-full lg:w-auto" style={{ gridTemplateColumns: `repeat(${[userRoles.hasAdminView, userRoles.hasBlogsContributor, userRoles.hasProjectsContributor, userRoles.hasCertificationsContributor, userRoles.hasAdminView].filter(Boolean).length}, 1fr)` }}>
            {userRoles.hasAdminView && (
              <TabsTrigger value="overview">
                <BarChart3 className="h-4 w-4 mr-2" />
                Overview
              </TabsTrigger>
            )}
            {userRoles.hasBlogsContributor && (
              <TabsTrigger value="blogs">
                <FileText className="h-4 w-4 mr-2" />
                Blogs
              </TabsTrigger>
            )}
            {userRoles.hasProjectsContributor && (
              <TabsTrigger value="projects">
                <FolderKanban className="h-4 w-4 mr-2" />
                Projects
              </TabsTrigger>
            )}
            {userRoles.hasCertificationsContributor && (
              <TabsTrigger value="certifications">
                <Award className="h-4 w-4 mr-2" />
                Certifications
              </TabsTrigger>
            )}
            {userRoles.hasAdminView && (
              <TabsTrigger value="settings">
                <Settings className="h-4 w-4 mr-2" />
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
                  <CardTitle className="text-sm font-medium">
                    Total Blogs
                  </CardTitle>
                  <FileText className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">0</div>
                  <p className="text-xs text-muted-foreground">
                    Published blog posts
                  </p>
                </CardContent>
              </Card>

              <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">
                    Total Projects
                  </CardTitle>
                  <FolderKanban className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">0</div>
                  <p className="text-xs text-muted-foreground">
                    Portfolio projects
                  </p>
                </CardContent>
              </Card>

              <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">
                    Media Files
                  </CardTitle>
                  <ImageIcon className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">0</div>
                  <p className="text-xs text-muted-foreground">
                    Images in storage
                  </p>
                </CardContent>
              </Card>

              <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">
                    System Status
                  </CardTitle>
                  <Settings className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold text-green-600">
                    Healthy
                  </div>
                  <p className="text-xs text-muted-foreground">
                    All systems operational
                  </p>
                </CardContent>
              </Card>
            </div>

            <Card>
              <CardHeader>
                <CardTitle>Welcome to the Admin Panel</CardTitle>
                <CardDescription>
                  Manage your content, projects, and settings from this
                  dashboard.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <p className="text-sm text-muted-foreground">
                  Use the tabs above to navigate between different sections:
                </p>
                <ul className="list-disc list-inside space-y-2 text-sm text-muted-foreground">
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
                    <strong>Settings:</strong> Configure system settings and
                    preferences
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
                <CardDescription>
                  Create and manage your blog posts
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="flex flex-col items-center justify-center py-12 text-center">
                  <FileText className="h-12 w-12 text-muted-foreground mb-4" />
                  <h3 className="text-lg font-semibold mb-2">
                    No blogs yet
                  </h3>
                  <p className="text-sm text-muted-foreground mb-6">
                    Create your first blog post to get started
                  </p>
                  <Button>
                    <FileText className="mr-2 h-4 w-4" />
                    Create Blog Post
                  </Button>
                </div>
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
                <CardDescription>
                  Manage your portfolio projects
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="flex flex-col items-center justify-center py-12 text-center">
                  <FolderKanban className="h-12 w-12 text-muted-foreground mb-4" />
                  <h3 className="text-lg font-semibold mb-2">
                    No projects yet
                  </h3>
                  <p className="text-sm text-muted-foreground mb-6">
                    Add your first project to showcase your work
                  </p>
                  <Button>
                    <FolderKanban className="mr-2 h-4 w-4" />
                    Add Project
                  </Button>
                </div>
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
                <CardDescription>
                  Manage your professional certifications
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="flex flex-col items-center justify-center py-12 text-center">
                  <Award className="h-12 w-12 text-muted-foreground mb-4" />
                  <h3 className="text-lg font-semibold mb-2">
                    No certifications yet
                  </h3>
                  <p className="text-sm text-muted-foreground mb-6">
                    Add your first certification to showcase your expertise
                  </p>
                  <Button>
                    <Award className="mr-2 h-4 w-4" />
                    Add Certification
                  </Button>
                </div>
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
                <CardDescription>
                  Configure your admin panel settings
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <h4 className="text-sm font-medium">Authentication</h4>
                  <p className="text-sm text-muted-foreground">
                    Auth0 Identity as a Service active
                  </p>
                </div>
                <div className="space-y-2">
                  <h4 className="text-sm font-medium">Your Roles</h4>
                  <p className="text-sm text-muted-foreground">
                    {userRoles.hasAdminView && "Admin.View "}
                    {userRoles.hasBlogsContributor && "Blogs.Contributor "}
                    {userRoles.hasProjectsContributor && "Projects.Contributor "}
                    {userRoles.hasCertificationsContributor && "Certifications.Contributor "}
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
                  <p className="text-sm text-muted-foreground">
                    All endpoints operational
                  </p>
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
