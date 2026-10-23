"use client";

import { useCallback, useEffect, useState } from "react";
import { Loader2, LogIn, Unplug } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  disconnectMyZen,
  fetchMyZenStatus,
  fetchMyZenTimeEntries,
  MyZenNotConnectedError,
  startMyZenLogin,
  submitMyZenLogin,
  submitMyZenMfaCode,
  type MyZenLoginStep,
  type MyZenStatus,
  type MyZenTimeEntry,
} from "@/lib/api/myzenApi";

/** The first and last day of the month that contains `day`, as yyyy-MM-dd. */
export function monthOf(day: Date): { from: string; to: string } {
  const pad = (n: number) => String(n).padStart(2, "0");
  const y = day.getFullYear();
  const m = day.getMonth() + 1;
  const last = new Date(y, m, 0).getDate();
  return { from: `${y}-${pad(m)}-01`, to: `${y}-${pad(m)}-${pad(last)}` };
}

function formatTime(value: string | null): string {
  return value ? new Date(value).toLocaleString() : "never";
}

/**
 * Connects the owner's MyZen account (issue #345, docs/myzen.md): MyZen's own
 * login, with the CAPTCHA and MFA code typed here, then this month's entries.
 */
export function MyZenPanel({ today }: { today?: Date }) {
  const [month] = useState(() => monthOf(today ?? new Date()));
  const [status, setStatus] = useState<MyZenStatus | null>(null);
  const [step, setStep] = useState<MyZenLoginStep | null>(null);
  const [captcha, setCaptcha] = useState("");
  const [code, setCode] = useState("");
  const [entries, setEntries] = useState<MyZenTimeEntry[] | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadStatus = useCallback(
    () =>
      fetchMyZenStatus()
        .then(setStatus)
        .catch(() => setError("MyZen is not available right now.")),
    [],
  );

  useEffect(() => {
    void loadStatus();
  }, [loadStatus]);

  useEffect(() => {
    if (status?.state !== "Connected") return;
    fetchMyZenTimeEntries(month.from, month.to)
      .then(setEntries)
      .catch((e: unknown) => {
        if (e instanceof MyZenNotConnectedError) {
          void loadStatus();
        } else {
          setError("Could not load the time entries.");
        }
      });
  }, [status?.state, month, loadStatus]);

  const run = async (action: () => Promise<MyZenLoginStep | void>) => {
    setBusy(true);
    setError(null);
    try {
      const next = await action();
      if (next) {
        setStep(next.step === "Connected" ? null : next);
        setCaptcha("");
        setCode("");
        if (next.step === "Connected") await loadStatus();
      }
    } catch {
      setError("That did not work. Try again.");
    } finally {
      setBusy(false);
    }
  };

  if (!status) {
    return error ? (
      <p className="text-sm text-destructive">{error}</p>
    ) : (
      <Loader2 className="h-5 w-5 animate-spin" aria-label="Loading" />
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Badge variant={status.state === "Connected" ? "default" : "secondary"}>
          {status.state}
        </Badge>
        {status.state === "Connected" && (
          <span className="text-sm text-muted-foreground">
            Connected {formatTime(status.connectedAt)} · last seen alive{" "}
            {formatTime(status.lastSeenAlive)}
          </span>
        )}
      </div>

      {error && <p className="text-sm text-destructive">{error}</p>}

      {status.state !== "Connected" && !step && (
        <Button disabled={busy} onClick={() => void run(startMyZenLogin)}>
          <LogIn className="mr-2 h-4 w-4" />
          {status.state === "Expired" ? "Reconnect MyZen" : "Connect MyZen"}
        </Button>
      )}

      {step?.step === "Credentials" && (
        <form
          className="max-w-sm space-y-3"
          onSubmit={(e) => {
            e.preventDefault();
            void run(() => submitMyZenLogin(captcha));
          }}
        >
          {step.message && <p className="text-sm text-destructive">{step.message}</p>}
          {step.captchaImage ? (
            <>
              {/* eslint-disable-next-line @next/next/no-img-element -- an inline data URL, not an optimisable asset */}
              <img src={step.captchaImage} alt="MyZen CAPTCHA" className="h-10" />
              <Label htmlFor="myzen-captcha">CAPTCHA</Label>
              <Input
                id="myzen-captcha"
                value={captcha}
                autoComplete="off"
                onChange={(e) => setCaptcha(e.target.value)}
              />
            </>
          ) : (
            <p className="text-sm text-muted-foreground">
              MyZen shows no CAPTCHA. Sign in with the stored account.
            </p>
          )}
          <Button type="submit" disabled={busy}>
            Sign in to MyZen
          </Button>
        </form>
      )}

      {step?.step === "Mfa" && (
        <form
          className="max-w-sm space-y-3"
          onSubmit={(e) => {
            e.preventDefault();
            void run(() => submitMyZenMfaCode(code));
          }}
        >
          {step.message && <p className="text-sm text-destructive">{step.message}</p>}
          <Label htmlFor="myzen-code">Code from your authenticator app</Label>
          <Input
            id="myzen-code"
            value={code}
            inputMode="numeric"
            autoComplete="one-time-code"
            maxLength={6}
            onChange={(e) => setCode(e.target.value)}
          />
          <Button type="submit" disabled={busy || !/^\d{6}$/.test(code)}>
            Verify
          </Button>
        </form>
      )}

      {status.state === "Connected" && (
        <>
          <Button
            variant="outline"
            disabled={busy}
            onClick={() =>
              void run(async () => {
                await disconnectMyZen();
                setEntries(null);
                await loadStatus();
              })
            }
          >
            <Unplug className="mr-2 h-4 w-4" />
            Disconnect
          </Button>
          {entries && (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Date</TableHead>
                  <TableHead>Project</TableHead>
                  <TableHead>Description</TableHead>
                  <TableHead className="text-right">Duration</TableHead>
                  <TableHead>Invoiced</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {entries.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={5} className="text-muted-foreground">
                      No time entries this month.
                    </TableCell>
                  </TableRow>
                )}
                {entries.map((entry) => (
                  <TableRow key={entry.id}>
                    <TableCell>{entry.date}</TableCell>
                    <TableCell>{entry.project}</TableCell>
                    <TableCell>{entry.description}</TableCell>
                    <TableCell className="text-right">{entry.duration.slice(0, 5)}</TableCell>
                    <TableCell>{entry.invoiced ? "Yes" : "No"}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </>
      )}
    </div>
  );
}
