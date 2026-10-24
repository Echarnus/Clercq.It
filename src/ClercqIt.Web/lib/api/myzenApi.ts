import { z } from "zod";
import { authFetch } from "./adminApi";

// The owner's own MyZen account (issue #345, docs/myzen.md). Every call needs
// the MyZen.Owner role; the API answers 404 while the MyZen flag is off.

export const MyZenStatusSchema = z.object({
  state: z.enum(["Disconnected", "Connecting", "Connected", "Expired"]),
  connectedAt: z.string().nullable(),
  lastSeenAlive: z.string().nullable(),
});

export type MyZenStatus = z.infer<typeof MyZenStatusSchema>;

export const MyZenLoginStepSchema = z.object({
  step: z.enum(["Credentials", "Mfa", "Connected"]),
  // Only an inline PNG is ever shown, so a hostile value cannot load a remote image.
  captchaImage: z.string().startsWith("data:image/png;base64,").nullable(),
  message: z.string().nullable(),
});

export type MyZenLoginStep = z.infer<typeof MyZenLoginStepSchema>;

export const MyZenTimeEntrySchema = z.object({
  id: z.number(),
  date: z.iso.date(),
  description: z.string(),
  projectId: z.number().nullable(),
  project: z.string(),
  customerId: z.number().nullable(),
  customer: z.string(),
  duration: z.string(),
  invoiced: z.boolean(),
});

export type MyZenTimeEntry = z.infer<typeof MyZenTimeEntrySchema>;

export const MyZenProjectOptionSchema = z.object({
  id: z.number(),
  name: z.string(),
  customerId: z.number().nullable(),
  customer: z.string(),
});

export type MyZenProjectOption = z.infer<typeof MyZenProjectOptionSchema>;

/** A time entry to add or change; `customerId` is null for projects without a customer. */
export const MyZenTimeEntryInputSchema = z.object({
  date: z.iso.date(),
  durationMinutes: z
    .number()
    .int()
    .min(1)
    .max(24 * 60),
  projectId: z.number().int().positive(),
  customerId: z.number().int().positive().nullable(),
  description: z.string().max(1000),
  invoiced: z.boolean(),
});

export type MyZenTimeEntryInput = z.infer<typeof MyZenTimeEntryInputSchema>;

/** Thrown when MyZen needs to be reconnected (the API answers 409). */
export class MyZenNotConnectedError extends Error {
  constructor() {
    super("MyZen is not connected. Connect it again.");
    this.name = "MyZenNotConnectedError";
  }
}

/** Thrown when MyZen refuses a change (the API answers 422); the message is MyZen's reason. */
export class MyZenRejectedError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "MyZenRejectedError";
  }
}

/** Thrown when MyZen has no time entry with the ID (the API answers 404). */
export class MyZenEntryNotFoundError extends Error {
  constructor() {
    super("MyZen has no such time entry.");
    this.name = "MyZenEntryNotFoundError";
  }
}

/**
 * Thrown for any other failed call; `message` carries the API's reason (the
 * problem `detail`, such as "MyZen could not be reached.") when it gives one.
 */
export class MyZenRequestError extends Error {
  constructor(
    message: string,
    readonly status: number,
  ) {
    super(message);
    this.name = "MyZenRequestError";
  }
}

/** The problem `detail` of a failed response, if the API sent one. */
async function problemDetail(response: Response): Promise<string | undefined> {
  const problem = z
    .object({ detail: z.string().optional() })
    .safeParse(await response.json().catch(() => ({})));
  return (problem.success && problem.data.detail) || undefined;
}

async function requestError(response: Response): Promise<MyZenRequestError> {
  const detail = await problemDetail(response);
  return new MyZenRequestError(
    detail ?? `MyZen request failed (${response.status}).`,
    response.status,
  );
}

async function check(response: Response): Promise<Response> {
  if (response.status === 409) {
    throw new MyZenNotConnectedError();
  }
  if (response.status === 404) {
    throw new MyZenEntryNotFoundError();
  }
  if (response.status === 422) {
    throw new MyZenRejectedError((await problemDetail(response)) ?? "MyZen refused the change.");
  }
  if (!response.ok) {
    throw await requestError(response);
  }
  return response;
}

async function send<T>(endpoint: string, schema: z.ZodType<T>, init: RequestInit = {}): Promise<T> {
  const response = await authFetch(endpoint, init);
  if (response.status === 409) {
    throw new MyZenNotConnectedError();
  }
  if (!response.ok) {
    throw await requestError(response);
  }
  return schema.parse(await response.json());
}

function postJson(body: object, method = "POST"): RequestInit {
  return {
    method,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  };
}

export function fetchMyZenStatus(): Promise<MyZenStatus> {
  return send("/api/myzen/session", MyZenStatusSchema);
}

export function startMyZenLogin(): Promise<MyZenLoginStep> {
  return send("/api/myzen/session/start", MyZenLoginStepSchema, { method: "POST" });
}

export function submitMyZenLogin(captcha: string): Promise<MyZenLoginStep> {
  return send("/api/myzen/session/login", MyZenLoginStepSchema, postJson({ captcha }));
}

export function submitMyZenMfaCode(code: string): Promise<MyZenLoginStep> {
  return send("/api/myzen/session/mfa", MyZenLoginStepSchema, postJson({ code }));
}

export async function disconnectMyZen(): Promise<void> {
  const response = await authFetch("/api/myzen/session", { method: "DELETE" });
  if (!response.ok) {
    throw new Error(`MyZen request failed (${response.status})`);
  }
}

/** Entries from `from` to `to` (yyyy-MM-dd, at most 93 days). */
export function fetchMyZenTimeEntries(from: string, to: string): Promise<MyZenTimeEntry[]> {
  const query = new URLSearchParams({ from, to });
  return send(`/api/myzen/time-entries?${query.toString()}`, z.array(MyZenTimeEntrySchema));
}

/** One entry with its project and customer IDs, as the edit form needs them. */
export async function fetchMyZenTimeEntry(id: number): Promise<MyZenTimeEntry> {
  const response = await check(await authFetch(`/api/myzen/time-entries/${id}`));
  return MyZenTimeEntrySchema.parse(await response.json());
}

/** Projects whose name matches `query` (at least two characters). */
export function searchMyZenProjects(query: string): Promise<MyZenProjectOption[]> {
  const params = new URLSearchParams({ q: query });
  return send(`/api/myzen/projects/search?${params.toString()}`, z.array(MyZenProjectOptionSchema));
}

export async function createMyZenTimeEntry(entry: MyZenTimeEntryInput): Promise<void> {
  const body = MyZenTimeEntryInputSchema.parse(entry);
  await check(await authFetch("/api/myzen/time-entries", postJson(body)));
}

export async function updateMyZenTimeEntry(id: number, entry: MyZenTimeEntryInput): Promise<void> {
  const body = MyZenTimeEntryInputSchema.parse(entry);
  await check(await authFetch(`/api/myzen/time-entries/${id}`, postJson(body, "PUT")));
}

export async function deleteMyZenTimeEntry(id: number): Promise<void> {
  await check(await authFetch(`/api/myzen/time-entries/${id}`, { method: "DELETE" }));
}
