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

/** Thrown when MyZen needs to be reconnected (the API answers 409). */
export class MyZenNotConnectedError extends Error {
  constructor() {
    super("MyZen is not connected. Connect it again.");
    this.name = "MyZenNotConnectedError";
  }
}

async function send<T>(endpoint: string, schema: z.ZodType<T>, init: RequestInit = {}): Promise<T> {
  const response = await authFetch(endpoint, init);
  if (response.status === 409) {
    throw new MyZenNotConnectedError();
  }
  if (!response.ok) {
    throw new Error(`MyZen request failed (${response.status})`);
  }
  return schema.parse(await response.json());
}

function postJson(body: object): RequestInit {
  return {
    method: "POST",
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
