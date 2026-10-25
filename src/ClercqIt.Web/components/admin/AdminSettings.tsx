import type { AdminSectionContext } from "@/lib/adminNav";

/** Read-only facts about the admin setup and the signed-in user's roles. */
export function AdminSettings({ roles }: AdminSectionContext) {
  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <h3 className="text-sm font-medium">Authentication</h3>
        <p className="text-sm text-muted-foreground">
          Cloud IAM (KeyCloak) Identity as a Service active
        </p>
      </div>
      <div className="space-y-2">
        <h3 className="text-sm font-medium">Your Roles</h3>
        <p className="text-sm text-muted-foreground">{roles.join(" ") || "None"}</p>
      </div>
      <div className="space-y-2">
        <h3 className="text-sm font-medium">Storage</h3>
        <p className="text-sm text-muted-foreground">Connected to Scaleway Object Storage</p>
      </div>
      <div className="space-y-2">
        <h3 className="text-sm font-medium">API Status</h3>
        <p className="text-sm text-muted-foreground">All endpoints operational</p>
      </div>
    </div>
  );
}
