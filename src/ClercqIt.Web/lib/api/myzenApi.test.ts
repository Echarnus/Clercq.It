// @vitest-environment node
import { http, HttpResponse } from "msw";
import { describe, expect, it, vi } from "vitest";
import { API } from "@/test/msw/fixtures";
import { server } from "@/test/msw/server";
import {
  createMyZenTimeEntry,
  deleteMyZenTimeEntry,
  disconnectMyZen,
  fetchMyZenStatus,
  fetchMyZenTimeEntries,
  fetchMyZenTimeEntry,
  MyZenEntryNotFoundError,
  MyZenNotConnectedError,
  MyZenRejectedError,
  MyZenRequestError,
  searchMyZenProjects,
  startMyZenLogin,
  submitMyZenLogin,
  submitMyZenMfaCode,
  updateMyZenTimeEntry,
} from "./myzenApi";

vi.mock("../auth", () => ({ getAccessToken: vi.fn(() => Promise.resolve("token")) }));

const entry = {
  id: 7,
  date: "2026-09-01",
  description: "Work",
  projectId: null,
  project: "Website",
  customerId: null,
  customer: "Acme",
  duration: "08:00:00",
  invoiced: false,
};

describe("MyZen API client", () => {
  it("reads the session state with the access token", async () => {
    let auth: string | null = null;
    server.use(
      http.get(`${API}/api/myzen/session`, ({ request }) => {
        auth = request.headers.get("Authorization");
        return HttpResponse.json({
          state: "Connected",
          connectedAt: "2026-10-23T20:00:00Z",
          lastSeenAlive: null,
        });
      }),
    );

    expect((await fetchMyZenStatus()).state).toBe("Connected");
    expect(auth).toBe("Bearer token");
  });

  it("posts the connect steps and returns the next step", async () => {
    const bodies: unknown[] = [];
    const step = (s: string, captchaImage: string | null = null) =>
      HttpResponse.json({ step: s, captchaImage, message: null });
    server.use(
      http.post(`${API}/api/myzen/session/start`, () =>
        step("Credentials", "data:image/png;base64,AA=="),
      ),
      http.post(`${API}/api/myzen/session/login`, async ({ request }) => {
        bodies.push(await request.json());
        return step("Mfa");
      }),
      http.post(`${API}/api/myzen/session/mfa`, async ({ request }) => {
        bodies.push(await request.json());
        return step("Connected");
      }),
    );

    expect((await startMyZenLogin()).captchaImage).toBe("data:image/png;base64,AA==");
    expect((await submitMyZenLogin("ab12")).step).toBe("Mfa");
    expect((await submitMyZenMfaCode("123456")).step).toBe("Connected");
    expect(bodies).toEqual([{ captcha: "ab12" }, { code: "123456" }]);
  });

  it("rejects a CAPTCHA that is not an inline PNG", async () => {
    server.use(
      http.post(`${API}/api/myzen/session/start`, () =>
        HttpResponse.json({
          step: "Credentials",
          captchaImage: "https://evil.example/x.png",
          message: null,
        }),
      ),
    );

    await expect(startMyZenLogin()).rejects.toThrow();
  });

  it("reads time entries for a period", async () => {
    let query = "";
    server.use(
      http.get(`${API}/api/myzen/time-entries`, ({ request }) => {
        query = new URL(request.url).search;
        return HttpResponse.json([entry]);
      }),
    );

    expect(await fetchMyZenTimeEntries("2026-09-01", "2026-09-30")).toEqual([entry]);
    expect(query).toBe("?from=2026-09-01&to=2026-09-30");
  });

  it("throws MyZenNotConnectedError on 409 and a plain error otherwise", async () => {
    server.use(
      http.get(`${API}/api/myzen/time-entries`, () => new HttpResponse(null, { status: 409 })),
      http.get(`${API}/api/myzen/session`, () => new HttpResponse(null, { status: 502 })),
    );

    await expect(fetchMyZenTimeEntries("2026-09-01", "2026-09-30")).rejects.toBeInstanceOf(
      MyZenNotConnectedError,
    );
    await expect(fetchMyZenStatus()).rejects.toThrow("MyZen request failed (502)");
  });

  it("carries the API's problem detail on other failures", async () => {
    server.use(
      http.get(`${API}/api/myzen/time-entries`, () =>
        HttpResponse.json(
          { title: "MyZen is unavailable", status: 502, detail: "MyZen could not be reached." },
          { status: 502 },
        ),
      ),
    );

    const error = await fetchMyZenTimeEntries("2026-09-01", "2026-09-30").catch((e: unknown) => e);

    expect(error).toBeInstanceOf(MyZenRequestError);
    expect(error).toMatchObject({ message: "MyZen could not be reached.", status: 502 });
  });

  it("disconnects, and reports a failed disconnect", async () => {
    server.use(
      http.delete(`${API}/api/myzen/session`, () => new HttpResponse(null, { status: 204 })),
    );
    await expect(disconnectMyZen()).resolves.toBeUndefined();

    server.use(
      http.delete(`${API}/api/myzen/session`, () => new HttpResponse(null, { status: 500 })),
    );
    await expect(disconnectMyZen()).rejects.toThrow("MyZen request failed (500)");
  });

  it("searches projects and reads one entry", async () => {
    let query = "";
    server.use(
      http.get(`${API}/api/myzen/projects/search`, ({ request }) => {
        query = new URL(request.url).search;
        return HttpResponse.json([{ id: 5, name: "Website", customerId: 7, customer: "Acme" }]);
      }),
      http.get(`${API}/api/myzen/time-entries/7`, () => HttpResponse.json(entry)),
    );

    expect(await searchMyZenProjects("web site")).toEqual([
      { id: 5, name: "Website", customerId: 7, customer: "Acme" },
    ]);
    expect(query).toBe("?q=web+site");
    expect(await fetchMyZenTimeEntry(7)).toEqual(entry);
  });

  it("adds, changes and deletes entries", async () => {
    const calls: { method: string; path: string; body: unknown }[] = [];
    const record = async ({ request }: { request: Request }) => {
      calls.push({
        method: request.method,
        path: new URL(request.url).pathname,
        body: request.method === "DELETE" ? null : await request.json(),
      });
      return new HttpResponse(null, { status: 204 });
    };
    server.use(
      http.post(`${API}/api/myzen/time-entries`, record),
      http.put(`${API}/api/myzen/time-entries/7`, record),
      http.delete(`${API}/api/myzen/time-entries/7`, record),
    );

    await createMyZenTimeEntry(input);
    await updateMyZenTimeEntry(7, { ...input, customerId: null });
    await deleteMyZenTimeEntry(7);

    expect(calls).toEqual([
      { method: "POST", path: "/api/myzen/time-entries", body: input },
      { method: "PUT", path: "/api/myzen/time-entries/7", body: { ...input, customerId: null } },
      { method: "DELETE", path: "/api/myzen/time-entries/7", body: null },
    ]);
  });

  it("does not send an invalid entry", async () => {
    await expect(createMyZenTimeEntry({ ...input, durationMinutes: 0 })).rejects.toThrow();
  });

  it("maps 409, 404 and 422 to their errors, with MyZen's reason", async () => {
    server.use(
      http.delete(`${API}/api/myzen/time-entries/1`, () => new HttpResponse(null, { status: 409 })),
      http.delete(`${API}/api/myzen/time-entries/2`, () => new HttpResponse(null, { status: 404 })),
      http.delete(`${API}/api/myzen/time-entries/3`, () =>
        HttpResponse.json({ detail: "Invoiced" }, { status: 422 }),
      ),
      http.delete(`${API}/api/myzen/time-entries/4`, () => new HttpResponse("x", { status: 422 })),
      http.delete(`${API}/api/myzen/time-entries/5`, () => new HttpResponse(null, { status: 500 })),
    );

    await expect(deleteMyZenTimeEntry(1)).rejects.toBeInstanceOf(MyZenNotConnectedError);
    await expect(deleteMyZenTimeEntry(2)).rejects.toBeInstanceOf(MyZenEntryNotFoundError);
    await expect(deleteMyZenTimeEntry(3)).rejects.toEqual(new MyZenRejectedError("Invoiced"));
    await expect(deleteMyZenTimeEntry(4)).rejects.toThrow("MyZen refused the change.");
    await expect(deleteMyZenTimeEntry(5)).rejects.toThrow("MyZen request failed (500)");
  });
});

const input = {
  date: "2026-09-01",
  durationMinutes: 480,
  projectId: 5,
  customerId: 7,
  description: "Work",
  invoiced: false,
};
