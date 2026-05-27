# ProcessingService Debugging

The recommended debugging setup is hybrid:

- run infrastructure, API, frontend, and nginx with Docker Compose;
- stop the containerized `processing-service`;
- run `AutoVerdict.ProcessingService` locally from the IDE or `dotnet run`.

This is necessary because a headed Playwright browser launched inside a Linux Docker container will not appear on the Windows desktop unless display forwarding is configured.

## Start The Stack Without The Worker

```powershell
docker-compose up -d --scale processing-service=0
```

The local override exposes the services the worker needs:

- PostgreSQL: `localhost:5432`
- NATS: `localhost:4222`
- SeaweedFS S3: `localhost:8333`

## Run The Worker Locally

From the repository root:

```powershell
dotnet run --project src/backend/src/AutoVerdict.ProcessingService/AutoVerdict.ProcessingService.csproj
```

The project launch profile is configured for local debugging with:

- `DOTNET_ENVIRONMENT=Development`
- `DATABASE_URL=Host=localhost;Port=5432;Database=autoverdict;Username=autoverdict;Password=autoverdict`
- `NATS_URL=nats://localhost:4222`
- `S3_ENDPOINT=http://localhost:8333`
- `PLAYWRIGHT_HEADLESS=false`
- `PLAYWRIGHT_DEVTOOLS=true`
- `PLAYWRIGHT_SLOW_MO_MS=250`
- `PLAYWRIGHT_DEBUG_PAUSE_MS=120000`
- `PLAYWRIGHT_BROWSER_CHANNEL=chrome`

When a check is queued, Chromium opens locally. Log into Otomoto during the pause. After the pause, the worker reloads the listing and extracts data from that logged-in browser session.

## Breakpoints

Running `dotnet run -c Debug` creates a debug build, but it does not attach an interactive debugger. To hit breakpoints, start the project from your IDE with debugging enabled, or attach your IDE debugger to the running `AutoVerdict.ProcessingService` process after `dotnet run` starts.

Set breakpoints in:

- `src/backend/src/AutoVerdict.ProcessingService/Consumers/CarCheckConsumer.cs`
- `src/backend/src/AutoVerdict.ProcessingService/Pipeline/CarCheckAnalysisPipeline.cs`
- `src/backend/src/AutoVerdict.ProcessingService/Parsing/OtomotoListingParser.cs`

Then submit an Otomoto URL from the frontend.
