import { ProjectsService } from "./projectsService";

type Factory<T> = () => T;

const factories = new Map<string, Factory<unknown>>();

export function registerFactory<T>(key: string, factory: Factory<T>) {
  factories.set(key, factory as Factory<unknown>);
}

export function resolve<T>(key: string): T {
  const factory = factories.get(key);
  if (!factory) {
    throw new Error(`Service factory not registered: ${key}`);
  }
  return factory() as T;
}

// Simple scope that holds instances created during a request
export function createScope() {
  const instances = new Map<string, unknown>();

  return {
    resolve<T>(key: string): T {
      if (instances.has(key)) {
        return instances.get(key) as T;
      }
      const factory = factories.get(key);
      if (!factory) throw new Error(`Service factory not registered: ${key}`);
      const inst = factory();
      instances.set(key, inst);
      return inst as T;
    },
  };
}

// Register default factories
registerFactory("ProjectsService", () => new ProjectsService());

export function getProjectsServiceFactory(): ProjectsService {
  return resolve<ProjectsService>("ProjectsService");
}
