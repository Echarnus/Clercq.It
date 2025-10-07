import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { getApiUrl, type Project } from './baseApi';

// Re-export Project type for convenience
export type { Project };

export const projectsApi = createApi({
  reducerPath: 'projectsApi',
  baseQuery: fetchBaseQuery({ baseUrl: `${getApiUrl()}/api` }),
  endpoints: (builder) => ({
    getAllProjects: builder.query<Project[], void>({
      query: () => '/projects',
    }),
    getFeaturedProjects: builder.query<Project[], void>({
      query: () => '/projects/featured',
    }),
    getProjectById: builder.query<Project, string>({
      query: (id) => `/projects/${id}`,
    }),
  }),
});

export const {
  useGetAllProjectsQuery,
  useGetFeaturedProjectsQuery,
  useGetProjectByIdQuery,
} = projectsApi;
