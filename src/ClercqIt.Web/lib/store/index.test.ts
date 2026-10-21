import { describe, expect, it } from "vitest";
import { projectsApi } from "../api/projectsApi";
import { project } from "@/test/msw/fixtures";
import { makeStore } from "./index";

describe("store", () => {
  it("runs the projects queries through RTK Query", async () => {
    const store = makeStore();

    const all = await store.dispatch(projectsApi.endpoints.getAllProjects.initiate());
    const featured = await store.dispatch(projectsApi.endpoints.getFeaturedProjects.initiate());
    const one = await store.dispatch(projectsApi.endpoints.getProjectById.initiate(project.id));

    expect(all.data).toEqual([project]);
    expect(featured.data).toEqual([project]);
    expect(one.data).toEqual(project);
    expect(store.getState()[projectsApi.reducerPath]).toBeDefined();
  });
});
