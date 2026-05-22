export default function Home() {
  return (
    <div className="flex flex-col flex-1 items-center justify-center min-h-screen bg-background">
      <main className="flex flex-col items-center gap-8 text-center px-4">
        <h1 className="text-4xl font-bold tracking-tight text-foreground">
          AutoVerdict
        </h1>
        <p className="text-lg text-muted-foreground max-w-md">
          AI-powered car history verification. Upload a document and get a
          detailed vehicle report in seconds.
        </p>
        <a
          href="/api/auth/google"
          className="inline-flex h-11 items-center justify-center rounded-md bg-primary px-8 text-sm font-medium text-primary-foreground shadow transition-colors hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        >
          Sign in with Google
        </a>
      </main>
    </div>
  );
}
