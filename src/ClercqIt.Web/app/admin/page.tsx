"use client";

import { useState, Suspense } from "react";
import { useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Lock, LogIn } from "lucide-react";
import { signIn } from "@/lib/auth";

// Admin sign-in (issue #257): Keycloak (Cloud IAM in production) shows the login
// page, including MFA. This page never sees a password.
function AdminLoginForm() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const searchParams = useSearchParams();
  const redirectError = searchParams.get("error");

  const handleSignIn = async () => {
    setError("");
    setIsLoading(true);
    try {
      await signIn("/admin/dashboard");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Sign-in is not available right now");
      setIsLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-linear-to-br from-slate-50 to-slate-100 p-4 dark:from-slate-900 dark:to-slate-800">
      <Card className="w-full max-w-md shadow-xl">
        <CardHeader className="space-y-1">
          <div className="mb-4 flex items-center justify-center">
            <div className="rounded-full bg-primary/10 p-3">
              <Lock className="h-8 w-8 text-primary" />
            </div>
          </div>
          <CardTitle className="text-center text-2xl">Admin Login</CardTitle>
          <CardDescription className="text-center">
            Sign in to access the admin dashboard
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {(error || redirectError) && (
            <Alert variant="destructive">
              <AlertDescription>{error || redirectError}</AlertDescription>
            </Alert>
          )}
          <p className="text-center text-sm text-muted-foreground">
            You will be sent to the Clercq.It sign-in page and brought back here.
          </p>
        </CardContent>
        <CardFooter className="flex flex-col gap-4">
          <Button
            type="button"
            className="w-full"
            disabled={isLoading}
            onClick={() => void handleSignIn()}
          >
            {isLoading ? (
              "Redirecting..."
            ) : (
              <>
                <LogIn className="mr-2 h-4 w-4" />
                Sign In
              </>
            )}
          </Button>
        </CardFooter>
      </Card>
    </div>
  );
}

export default function AdminLoginPage() {
  return (
    <Suspense
      fallback={
        <div className="flex min-h-screen items-center justify-center bg-linear-to-br from-slate-50 to-slate-100 p-4 dark:from-slate-900 dark:to-slate-800">
          <Card className="w-full max-w-md shadow-xl">
            <CardContent className="flex justify-center py-8">
              <div className="text-center">Loading...</div>
            </CardContent>
          </Card>
        </div>
      }
    >
      <AdminLoginForm />
    </Suspense>
  );
}
