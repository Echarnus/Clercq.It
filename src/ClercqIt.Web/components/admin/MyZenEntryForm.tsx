"use client";

import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  MyZenRejectedError,
  searchMyZenProjects,
  type MyZenProjectOption,
  type MyZenTimeEntry,
  type MyZenTimeEntryInput,
} from "@/lib/api/myzenApi";

/** Minutes in a duration typed as `h:mm` or as hours (`7.5`, `7,5`); null when it is not one. */
export function parseDuration(text: string): number | null {
  const value = text.trim();
  const clock = /^(\d{1,2}):([0-5]\d)$/.exec(value);
  const hours = /^[\d.,]{1,5}$/.test(value) ? Number(value.replace(",", ".")) : Number.NaN;
  const minutes = clock
    ? Number(clock[1]) * 60 + Number(clock[2])
    : Number.isFinite(hours)
      ? Math.round(hours * 60)
      : null;
  return minutes !== null && minutes >= 1 && minutes <= 24 * 60 ? minutes : null;
}

/** `07:30:00` (the API's duration) as `7:30`. */
export function formatDuration(duration: string): string {
  const [hours = "0", minutes = "00"] = duration.split(":");
  return `${Number(hours)}:${minutes}`;
}

type Selected = Pick<MyZenProjectOption, "id" | "name" | "customerId">;

/**
 * Adds or edits one MyZen time entry (issue #357). The project comes from
 * MyZen's own lookup, which also gives the customer the entry must carry.
 */
export function MyZenEntryForm({
  entry,
  defaultDate,
  onSave,
  onCancel,
}: {
  /** The entry to edit, read on its own so it has its project and customer IDs. */
  entry?: MyZenTimeEntry;
  defaultDate: string;
  onSave: (input: MyZenTimeEntryInput) => Promise<void>;
  onCancel: () => void;
}) {
  const [date, setDate] = useState(entry?.date ?? defaultDate);
  const [duration, setDuration] = useState(entry ? formatDuration(entry.duration) : "8:00");
  const [project, setProject] = useState<Selected | null>(
    entry?.projectId
      ? { id: entry.projectId, name: entry.project, customerId: entry.customerId }
      : null,
  );
  const [search, setSearch] = useState("");
  const [options, setOptions] = useState<MyZenProjectOption[]>([]);
  const [description, setDescription] = useState(entry?.description ?? "");
  const [invoiced, setInvoiced] = useState(entry?.invoiced ?? false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const query = search.trim();
    if (query.length < 2) return;
    let current = true;
    const timer = setTimeout(() => {
      searchMyZenProjects(query)
        .then((found) => {
          if (current) setOptions(found);
        })
        .catch(() => {
          if (current) setError("Could not search MyZen's projects.");
        });
    }, 300);
    return () => {
      current = false;
      clearTimeout(timer);
    };
  }, [search]);

  // Results of an older, longer search are hidden once the text is too short to search.
  const shown = search.trim().length >= 2 ? options : [];
  const minutes = parseDuration(duration);
  const valid = minutes !== null && project !== null && date !== "" && description.length <= 1000;

  const submit = async () => {
    if (minutes === null || project === null) return;
    setBusy(true);
    setError(null);
    try {
      await onSave({
        date,
        durationMinutes: minutes,
        projectId: project.id,
        customerId: project.customerId,
        description: description.trim(),
        invoiced,
      });
    } catch (e: unknown) {
      setError(e instanceof MyZenRejectedError ? e.message : "MyZen did not save the entry.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <form
      aria-label={entry ? "Edit time entry" : "New time entry"}
      className="max-w-lg space-y-3 rounded-md border p-4"
      onSubmit={(e) => {
        e.preventDefault();
        void submit();
      }}
    >
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <Label htmlFor="myzen-date">Date</Label>
          <Input
            id="myzen-date"
            type="date"
            value={date}
            onChange={(e) => setDate(e.target.value)}
          />
        </div>
        <div className="space-y-1">
          <Label htmlFor="myzen-duration">Duration (h:mm)</Label>
          <Input
            id="myzen-duration"
            value={duration}
            inputMode="decimal"
            aria-invalid={minutes === null}
            onChange={(e) => setDuration(e.target.value)}
          />
        </div>
      </div>

      <div className="space-y-1">
        <Label htmlFor="myzen-project">Project</Label>
        {project ? (
          <div className="flex items-center gap-2 text-sm">
            <span>{project.name}</span>
            <Button type="button" variant="link" size="sm" onClick={() => setProject(null)}>
              Change
            </Button>
          </div>
        ) : (
          <>
            <Input
              id="myzen-project"
              value={search}
              autoComplete="off"
              placeholder="Type at least two letters"
              onChange={(e) => setSearch(e.target.value)}
            />
            {shown.length > 0 && (
              <ul className="rounded-md border text-sm" aria-label="Matching projects">
                {shown.map((option) => (
                  <li key={option.id}>
                    <button
                      type="button"
                      className="w-full px-3 py-1 text-left hover:bg-muted"
                      onClick={() => {
                        setProject(option);
                        setSearch("");
                      }}
                    >
                      {option.name}
                      {option.customer && (
                        <span className="text-muted-foreground"> · {option.customer}</span>
                      )}
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </>
        )}
      </div>

      <div className="space-y-1">
        <Label htmlFor="myzen-description">Description</Label>
        <Textarea
          id="myzen-description"
          value={description}
          maxLength={1000}
          onChange={(e) => setDescription(e.target.value)}
        />
      </div>

      <div className="flex items-center gap-2">
        <input
          id="myzen-invoiced"
          type="checkbox"
          checked={invoiced}
          onChange={(e) => setInvoiced(e.target.checked)}
        />
        <Label htmlFor="myzen-invoiced">Invoiced</Label>
      </div>

      {error && <p className="text-sm text-destructive">{error}</p>}

      <div className="flex gap-2">
        <Button type="submit" disabled={busy || !valid}>
          {entry ? "Save changes" : "Add entry"}
        </Button>
        <Button type="button" variant="outline" disabled={busy} onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </form>
  );
}
