# Portfolio Feature Implementation Summary

## Overview

This implementation adds a complete portfolio feature to the Clercq.It application, fetching project data from the database API with full server-side rendering (SSR) support for optimal SEO.

## What Was Implemented

### 1. Backend API Endpoint
- **New Endpoint**: `GET /api/projects/{id}` - Fetch a single project by ID
- **New Query**: `GetProjectByIdQuery` and handler
- **Returns**: `ProjectDto` or 404 if not found

### 2. Redux Infrastructure
- **Redux Store**: Configured with Redux Toolkit
- **RTK Query API**: Projects API client with endpoints for:
  - `getAllProjects()`
  - `getFeaturedProjects()`
  - `getProjectById(id)`
- **Store Provider**: Wraps the application for client-side state management

### 3. Server-Side Data Fetching
- **Feature-Sliced API Layer**: `/app/portfolio/lib/api.ts`
  - `fetchAllProjects()` - Fetches all projects sorted by date
  - `fetchFeaturedProjects()` - Fetches featured projects
  - `fetchProjectById(id)` - Fetches single project

### 4. Portfolio Pages

#### Portfolio List Page (`/portfolio`)
- Server-side rendered with fresh data from API
- Projects sorted by start date (newest to oldest)
- Featured projects displayed with larger layout
- Click on any project to navigate to detail page
- Responsive grid layout

#### Portfolio Detail Page (`/portfolio/[id]`)
- Server-side rendered project details
- Full project information including:
  - Title and description
  - Project dates with formatted display
  - Skill badges
  - Project image
  - Long description
- Back navigation to portfolio list
- Returns 404 for invalid project IDs

#### Home Page Featured Projects
- Server-side rendered featured projects
- Displays up to 3 featured projects
- Click to navigate to project details
- "View All Projects" link to portfolio page

## Architecture

### Vertical/Feature-Sliced Organization
```
lib/api/
├── baseApi.ts            # Shared API client and types
└── projectsApi.ts        # RTK Query configuration

app/portfolio/
├── page.tsx              # Portfolio list (SSR)
├── [id]/
│   └── page.tsx          # Portfolio detail (SSR)
└── lib/
    └── api.ts            # SSR-specific wrappers

lib/store/
├── index.ts              # Redux store configuration
└── StoreProvider.tsx     # Redux provider component
```

**Key Architectural Benefit:**
The base API client (`baseApi.ts`) is reused by both the RTK Query layer and the SSR layer, eliminating code duplication and ensuring consistency across the application.

### Data Flow

1. **Server-Side Rendering (Initial Load)**
   - Next.js server receives request
   - Server component fetches data from API
   - HTML rendered with full content
   - Sent to browser with SEO benefits

2. **Client-Side Hydration**
   - React hydrates the page
   - Redux store initialized
   - RTK Query available for subsequent fetches
   - Client-side navigation uses React Router

## Key Features

✅ **Server-Side Rendering**: All pages rendered on server for SEO  
✅ **Fresh Data**: Always fetches latest from API (`cache: 'no-store'`)  
✅ **Sorted Projects**: Newest projects first on portfolio page  
✅ **Featured Projects**: Special layout and home page display  
✅ **Detail Pages**: Individual project pages with full information  
✅ **Redux Ready**: Infrastructure in place for client-side state  
✅ **Type Safe**: Full TypeScript types for Project DTOs  
✅ **Responsive Design**: Mobile and desktop layouts  

## API Schema

```typescript
// Zod schema with runtime validation
const ProjectSchema = z.object({
  id: z.string().uuid(),
  startDate: z.string().datetime(),
  endDate: z.string().datetime(),
  shortDescription: z.string(),
  longDescription: z.string(),
  image: z.string().url(),
  featured: z.boolean(),
  title: z.string().min(1),
  skills: z.array(z.string()),
});

// TypeScript type inferred from schema
type Project = z.infer<typeof ProjectSchema>;
```

**Runtime Validation**: All API responses are validated using Zod schemas, ensuring type safety and data integrity.

## Configuration

```bash
# .env.local (development only)
NEXT_PUBLIC_API_URL=http://localhost:5035
```

**Production**: The application uses relative URLs in production (same Docker container). The `NEXT_PUBLIC_API_URL` environment variable is only needed for local development.

## Screenshots

### Home Page - Featured Projects
![Home Featured Projects](screenshots/home-featured-projects.png)

### Portfolio List Page
![Portfolio List](screenshots/portfolio-list.png)

### Portfolio Detail Page
![Portfolio Detail](screenshots/portfolio-detail.png)

## Files Changed

### Backend
- `src/ClercqIt.Api/Features/ProjectsEndpoints.cs` - Added GET by ID endpoint
- `src/Clercq.It.Application/Features/Projects/Queries/GetProjectByIdQuery.cs` - New query
- `src/Clercq.It.Application/Features/Projects/Queries/GetProjectByIdQueryHandler.cs` - New handler

### Frontend
- `src/ClercqIt.Web/app/layout.tsx` - Added Redux provider
- `src/ClercqIt.Web/app/home/page.tsx` - Fetch featured projects from API
- `src/ClercqIt.Web/app/portfolio/page.tsx` - Fetch all projects from API
- `src/ClercqIt.Web/app/portfolio/[id]/page.tsx` - New detail page
- `src/ClercqIt.Web/app/portfolio/lib/api.ts` - Server-side fetch utilities
- `src/ClercqIt.Web/lib/api/projectsApi.ts` - RTK Query API client
- `src/ClercqIt.Web/lib/store/index.ts` - Redux store config
- `src/ClercqIt.Web/lib/store/StoreProvider.tsx` - Redux provider

### Documentation
- `docs/portfolio-feature.md` - Comprehensive feature documentation
- `docs/screenshots/` - UI screenshots

## Testing

Build verified successful:
- TypeScript compilation: ✅ No errors in our code
- Next.js build: ✅ All pages build successfully
- SSR validation: ✅ Pages marked as dynamic (server-rendered)

## Next Steps

The implementation is complete and ready for use. When the backend API is running with project data:

1. The portfolio page will display all projects sorted by date
2. Featured projects will appear on the home page
3. Users can click projects to view detailed information
4. All pages will be server-rendered for optimal SEO

## Notes

- All portfolio pages are server-rendered on demand (dynamic mode)
- No static generation used to ensure fresh data from API
- Redux infrastructure ready for future client-side features
- Follows Next.js 15 and React 19 best practices
- Compatible with the existing Clean Architecture backend
