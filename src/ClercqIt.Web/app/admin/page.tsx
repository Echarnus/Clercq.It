"use client";

import { useState, Suspense } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Lock, LogIn, Shield } from "lucide-react";
import { InputOTP, InputOTPGroup, InputOTPSlot } from "@/components/ui/input-otp";

function AdminLoginForm() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [totpCode, setTotpCode] = useState("");
  const [showMfa, setShowMfa] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const router = useRouter();
  const searchParams = useSearchParams();

  // Check for error from OAuth redirect
  const oauthError = searchParams.get("error");

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setIsLoading(true);

    try {
      // Call authentication endpoint
      const response = await fetch("/api/auth/login", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          username,
          password,
          totpCode: showMfa ? totpCode : undefined,
        }),
      });

      const data = (await response.json()) as LoginResponse;

      // Check if MFA is required
      if (response.status === 428 && data.requiresMfa) {
        setShowMfa(true);
        setError(data.message || "Please enter your MFA code");
        setIsLoading(false);
        return;
      }

      if (!response.ok) {
        throw new Error(data.message || "Authentication failed");
      }

      // Store the JWT token and user data
      localStorage.setItem("admin_token", data.token);
      if (data.user) {
        localStorage.setItem("admin_user", JSON.stringify(data.user));
      }

      // Redirect to dashboard
      router.push("/admin/dashboard");
    } catch (err) {
      setError(err instanceof Error ? err.message : "An error occurred");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-slate-50 to-slate-100 p-4 dark:from-slate-900 dark:to-slate-800">
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
        <form onSubmit={(e) => void handleLogin(e)}>
          <CardContent className="space-y-4">
            {(error || oauthError) && (
              <Alert variant="destructive">
                <AlertDescription>{error || oauthError}</AlertDescription>
              </Alert>
            )}
            <div className="space-y-2">
              <Label htmlFor="username">Username</Label>
              <Input
                id="username"
                type="text"
                placeholder="Enter your username"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                required
                disabled={isLoading}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                placeholder="Enter your password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                disabled={isLoading}
              />
            </div>
            {showMfa && (
              <div className="space-y-2">
                <Label htmlFor="totpCode" className="flex items-center gap-2">
                  <Shield className="h-4 w-4" />
                  MFA Code (TOTP)
                </Label>
                <InputOTP
                  maxLength={6}
                  value={totpCode}
                  onChange={(value) => setTotpCode(value)}
                  disabled={isLoading}
                >
                  <InputOTPGroup>
                    <InputOTPSlot index={0} />
                    <InputOTPSlot index={1} />
                    <InputOTPSlot index={2} />
                    <InputOTPSlot index={3} />
                    <InputOTPSlot index={4} />
                    <InputOTPSlot index={5} />
                  </InputOTPGroup>
                </InputOTP>
                <p className="text-xs text-muted-foreground">
                  Enter the 6-digit code from your authenticator app
                </p>
              </div>
            )}
          </CardContent>
          <CardFooter className="flex flex-col gap-4">
            <Button type="submit" className="w-full" disabled={isLoading}>
              {isLoading ? (
                "Signing in..."
              ) : (
                <>
                  <LogIn className="mr-2 h-4 w-4" />
                  Sign In
                </>
              )}
            </Button>
          </CardFooter>
        </form>
      </Card>
    </div>
  );
}

interface LoginResponse {
  requiresMfa?: boolean;
  message?: string;
  token: string;
  user?: unknown;
}

export default function AdminLoginPage() {
  return (
    <Suspense
      fallback={
        <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-slate-50 to-slate-100 p-4 dark:from-slate-900 dark:to-slate-800">
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
