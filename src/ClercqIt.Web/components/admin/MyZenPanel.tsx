"use client";

import { useCallback, useEffect, useState } from "react";
import {
  ChevronLeft,
  ChevronRight,
  Loader2,
  LogIn,
  Pencil,
  Plus,
  Trash2,
  Unplug,
} from "lucide-react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
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
  createMyZenTimeEntry,
  deleteMyZenTimeEntry,
  disconnectMyZen,
  fetchMyZenStatus,
  fetchMyZenTimeEntries,
  fetchMyZenTimeEntry,
  MyZenNotConnectedError,
  MyZenRejectedError,
  MyZenRequestError,
  startMyZenLogin,
  submitMyZenLogin,
  submitMyZenMfaCode,
  type MyZenLoginStep,
  type MyZenStatus,
  type MyZenTimeEntry,
  type MyZenTimeEntryInput,
  updateMyZenTimeEntry,
} from "@/lib/api/myzenApi";
import { formatDuration, MyZenEntryForm } from "./MyZenEntryForm";

/** The first and last day of the month that contains `day`, as yyyy-MM-dd. */
export function monthOf(day: Date): { from: string; to: string } {
  const pad = (n: number) => String(n).padStart(2, "0");
  const y = day.getFullYear();
  const m = day.getMonth() + 1;
  const last = new Date(y, m, 0).getDate();
  return { from: `${y}-${pad(m)}-01`, to: `${y}-${pad(m)}-${pad(last)}` };
}

/** The month before (-1) or after (+1) the one starting at `from` (yyyy-MM-01). */
export function shiftMonth(from: string, months: number): { from: string; to: string } {
  const [y, m] = from.split("-").map(Number);
  return monthOf(new Date(y, m - 1 + months, 1));
}

function formatTime(value: string | null): string {
  return value ? new Date(value).toLocaleString() : "never";
}

/**
 * Connects the owner's MyZen account (issue #345, docs/myzen.md): MyZen's own
 * login, with the CAPTCHA and MFA code typed here. Once connected it lists a
 * month's entries and adds, edits and deletes them one at a time (issue #357).
 */
export function MyZenPanel({ today }: { today?: Date }) {
  const [month, setMonth] = useState(() => monthOf(today ?? new Date()));
  const [status, setStatus] = useState<MyZenStatus | null>(null);
  const [step, setStep] = useState<MyZenLoginStep | null>(null);
  const [captcha, setCaptcha] = useState("");
  const [code, setCode] = useState("");
  const [entries, setEntries] = useState<MyZenTimeEntry[] | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // The form: a new entry, an entry being edited, or closed.
  const [editing, setEditing] = useState<MyZenTimeEntry | "new" | null>(null);
  const [deleting, setDeleting] = useState<MyZenTimeEntry | null>(null);

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

  /**
   * Shows what went wrong, with the API's reason when it gave one; a lost
   * session reloads the status, so the Reconnect button appears.
   */
  const fail = useCallback(
    (e: unknown, message: string) => {
      if (e instanceof MyZenNotConnectedError) {
        setEditing(null);
        void loadStatus();
      } else if (e instanceof MyZenRejectedError) {
        setError(e.message);
      } else if (e instanceof MyZenRequestError) {
        setError(`${message} ${e.message}`);
      } else {
        setError(`${message} Try again in a moment.`);
      }
    },
    [loadStatus],
  );

  const loadEntries = useCallback(
    () =>
      fetchMyZenTimeEntries(month.from, month.to)
        .then((loaded) => {
          setEntries(loaded);
          setError(null);
        })
        .catch((e: unknown) => {
          setEntries(null);
          fail(e, "Could not load the time entries.");
        }),
    [month, fail],
  );

  useEffect(() => {
    if (status?.state !== "Connected") return;
    void loadEntries();
  }, [status?.state, loadEntries]);

  const save = async (input: MyZenTimeEntryInput) => {
    try {
      if (editing === "new") {
        await createMyZenTimeEntry(input);
      } else if (editing) {
        await updateMyZenTimeEntry(editing.id, input);
      }
    } catch (e: unknown) {
      if (e instanceof MyZenNotConnectedError) fail(e, "");
      throw e;
    }
    setEditing(null);
    await loadEntries();
  };

  const edit = async (entry: MyZenTimeEntry) => {
    setError(null);
    try {
      setEditing(await fetchMyZenTimeEntry(entry.id));
    } catch (e: unknown) {
      fail(e, "Could not open the time entry.");
    }
  };

  const remove = async (entry: MyZenTimeEntry) => {
    setBusy(true);
    setError(null);
    try {
      await deleteMyZenTimeEntry(entry.id);
      await loadEntries();
    } catch (e: unknown) {
      fail(e, "MyZen did not delete the entry.");
    } finally {
      setBusy(false);
      setDeleting(null);
    }
  };

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
          <div className="flex flex-wrap items-center gap-2">
            <Button
              variant="ghost"
              size="icon"
              aria-label="Previous month"
              onClick={() => setMonth((m) => shiftMonth(m.from, -1))}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <span className="text-sm font-medium" aria-live="polite">
              {month.from.slice(0, 7)}
            </span>
            <Button
              variant="ghost"
              size="icon"
              aria-label="Next month"
              onClick={() => setMonth((m) => shiftMonth(m.from, 1))}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
            <Button
              className="ml-auto"
              disabled={editing !== null}
              onClick={() => setEditing("new")}
            >
              <Plus className="mr-2 h-4 w-4" />
              Add entry
            </Button>
          </div>
          {editing && (
            <MyZenEntryForm
              key={editing === "new" ? "new" : editing.id}
              entry={editing === "new" ? undefined : editing}
              defaultDate={month.from}
              onSave={save}
              onCancel={() => setEditing(null)}
            />
          )}
          {entries && (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Date</TableHead>
                  <TableHead>Project</TableHead>
                  <TableHead>Description</TableHead>
                  <TableHead className="text-right">Duration</TableHead>
                  <TableHead>Invoiced</TableHead>
                  <TableHead>
                    <span className="sr-only">Actions</span>
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {entries.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={6} className="text-muted-foreground">
                      No time entries this month.
                    </TableCell>
                  </TableRow>
                )}
                {entries.map((entry) => (
                  <TableRow key={entry.id}>
                    <TableCell>{entry.date}</TableCell>
                    <TableCell>{entry.project}</TableCell>
                    <TableCell>{entry.description}</TableCell>
                    <TableCell className="text-right">{formatDuration(entry.duration)}</TableCell>
                    <TableCell>{entry.invoiced ? "Yes" : "No"}</TableCell>
                    <TableCell className="text-right whitespace-nowrap">
                      <Button
                        variant="ghost"
                        size="icon"
                        aria-label={`Edit entry of ${entry.date}`}
                        disabled={busy}
                        onClick={() => void edit(entry)}
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        aria-label={`Delete entry of ${entry.date}`}
                        disabled={busy}
                        onClick={() => setDeleting(entry)}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
          <AlertDialog open={deleting !== null} onOpenChange={(open) => !open && setDeleting(null)}>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>Delete this time entry?</AlertDialogTitle>
                <AlertDialogDescription>
                  {deleting &&
                    `${deleting.date}, ${deleting.project}, ${formatDuration(deleting.duration)}. MyZen removes it for good.`}
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>Cancel</AlertDialogCancel>
                <AlertDialogAction
                  onClick={() => deleting && void remove(deleting)}
                  className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                >
                  Delete
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </>
      )}
    </div>
  );
}
