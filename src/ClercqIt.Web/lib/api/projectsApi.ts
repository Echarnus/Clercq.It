import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

export interface Project {
  id: string;
  startDate: string;
  endDate: string;
  shortDescription: string;
  longDescription: string;
  image: string;
  featured: boolean;
  title: string;
  skills: string[];
}

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5035';

export const projectsApi = createApi({
  reducerPath: 'projectsApi',
  baseQuery: fetchBaseQuery({ baseUrl: `${API_URL}/api` }),
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
