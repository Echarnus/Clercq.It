import { configureStore } from '@reduxjs/toolkit';
import { projectsApi } from '../api/projectsApi';

export const makeStore = () => {
  return configureStore({
    reducer: {
      [projectsApi.reducerPath]: projectsApi.reducer,
    },
    middleware: (getDefaultMiddleware) =>
      getDefaultMiddleware().concat(projectsApi.middleware),
  });
};

export type AppStore = ReturnType<typeof makeStore>;
export type RootState = ReturnType<AppStore['getState']>;
export type AppDispatch = AppStore['dispatch'];
