// @vitest-environment node
import { http, HttpResponse } from "msw";
import { describe, expect, it, vi } from "vitest";
import { API } from "@/test/msw/fixtures";
import { server } from "@/test/msw/server";
import {
  disconnectMyZen,
  fetchMyZenStatus,
  fetchMyZenTimeEntries,
  MyZenNotConnectedError,
  startMyZenLogin,
  submitMyZenLogin,
  submitMyZenMfaCode,
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
});
