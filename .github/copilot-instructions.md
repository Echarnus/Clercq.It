# Clercq.It - Personal Portfolio & API

A fullstack web application featuring a .NET 9.0 minimal API backend and a Next.js React frontend with TypeScript, Tailwind CSS, and Radix UI components.

**ALWAYS reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that contradicts what is documented here.**

## Working Effectively

### Prerequisites & System Setup
Before building or running the application, ensure you have the required dependencies:

1. **Install .NET 9.0** (if not available):
   ```bash
   wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
   chmod +x dotnet-install.sh
   ./dotnet-install.sh --channel 9.0
   export PATH="$HOME/.dotnet:$PATH"
   ```

2. **Install pnpm** (required package manager):
   ```bash
   corepack enable
   corepack prepare pnpm@10.12.4 --activate
   ```

3. **Verify installations**:
   ```bash
   dotnet --version  # Should be 9.0.x
   pnpm --version    # Should be 10.12.4
   ```

### Build the Complete Application

**CRITICAL BUILD TIMING:**
- **NEVER CANCEL BUILD COMMANDS** - builds may take 30+ seconds
- **Use timeouts of 120+ seconds** for all build commands
- **pnpm install**: Takes ~2 seconds after first run, ~25 seconds on fresh install
- **.NET build**: Takes ~2 seconds after first build, ~8 seconds on fresh build
- **Next.js build**: Takes ~28 seconds (consistent timing)

Execute these commands in order:

1. **Build .NET API** (2-8 seconds):
   ```bash
   export PATH="$HOME/.dotnet:$PATH"
   dotnet build src/ClercqIt.Api/ClercqIt.Api.csproj
   ```

2. **Install Next.js dependencies** (2-25 seconds - NEVER CANCEL):
   ```bash
   cd src/ClercqIt.Web
   pnpm install
   ```

3. **Build Next.js frontend** (28 seconds - NEVER CANCEL):
   ```bash
   cd src/ClercqIt.Web
   pnpm run build
   ```

### Run the Applications

**Start both services** (they must run simultaneously):

1. **API Server** (runs on http://localhost:5035):
   ```bash
   export PATH="$HOME/.dotnet:$PATH"
   dotnet run --project src/ClercqIt.Api/ClercqIt.Api.csproj
   ```

2. **Frontend Development Server** (runs on http://localhost:3000):
   ```bash
   cd src/ClercqIt.Web
   pnpm run dev
   ```

### Quality Assurance

**Linting** (3-4 seconds):
```bash
cd src/ClercqIt.Web
pnpm run lint
```
*Note: First run will prompt to configure ESLint - select "Strict (recommended)" and allow 2+ minutes for setup.*

**Known Lint Issues**: The project has intentional lint warnings for unused variables and unescaped quotes. ESLint will return exit code 1 but this is expected.

## Validation Scenarios

**ALWAYS manually validate changes using these scenarios:**

### Backend API Testing
```bash
# Verify API is running and responsive
curl http://localhost:5035/weatherforecast
```
Expected: JSON array with 5 weather forecast objects containing date, temperatureC, temperatureF, and summary fields.

### Frontend Testing  
```bash
# Verify frontend loads
curl -s http://localhost:3000 | head -5
```
Expected: HTML content with title "Clercqlt - Continuous Development"

### End-to-End Validation
1. Start both API and frontend servers
2. Navigate to http://localhost:3000 in a browser (if available)
3. Verify the page loads with portfolio content
4. Check that the navigation works between Home and Portfolio pages

## Common Issues & Solutions

### Google Fonts Network Error
If you encounter "ENOTFOUND fonts.googleapis.com" during Next.js build:
- This is due to network restrictions
- The font imports have been removed from `app/layout.tsx`
- Uses system fonts instead: `system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto`

### Missing .NET 9.0
If you get "No compatible frameworks found":
- Install .NET 9.0 using the install script in Prerequisites
- Always set PATH: `export PATH="$HOME/.dotnet:$PATH"`

### ESLint Configuration
On first `pnpm run lint`:
- Select "Strict (recommended)" when prompted
- Allow 2+ minutes for dependency installation
- Subsequent runs are much faster (~3 seconds)

## Key Project Structure

```
src/
├── ClercqIt.Api/           # .NET 9.0 minimal API
│   ├── Program.cs          # Main API entry point with weather endpoint
│   ├── ClercqIt.Api.csproj # Project file targeting net9.0
│   └── Properties/         # Launch settings (port 5035)
└── ClercqIt.Web/           # Next.js React frontend
    ├── app/                # App router pages
    │   ├── layout.tsx      # Root layout (system fonts)
    │   ├── page.tsx        # Home page
    │   ├── home/           # Home route
    │   └── portfolio/      # Portfolio route  
    ├── components/         # Reusable React components
    ├── package.json        # Dependencies (pnpm 10.12.4)
    └── next.config.mjs     # Next.js config (standalone output)
```

## Technology Stack
- **Backend**: .NET 9.0 ASP.NET Core minimal API
- **Frontend**: Next.js 15.2.4 with React 19 
- **Styling**: Tailwind CSS 3.4.17 with custom theme
- **UI Components**: Radix UI component library
- **Package Manager**: pnpm 10.12.4
- **TypeScript**: Latest version with strict mode
- **Deployment**: Docker with nginx reverse proxy

## Docker Setup
The repository includes Docker configuration:
- `src/Dockerfile`: Multi-stage build for the Next.js app
- `src/nginx.conf`: Reverse proxy config (API on /api, frontend on /)
- Nginx routes `/api` to localhost:5000 and everything else to localhost:3000

## No Tests Available
This project currently has no unit tests or integration tests. Focus validation on manual testing of the running applications rather than automated test suites.

## Important Notes
- The project uses Next.js standalone output mode
- TypeScript and ESLint errors are ignored during builds (intentional configuration)
- Images are unoptimized in the Next.js config
- The .github/workflows/build.yml file exists but is currently empty