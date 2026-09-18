#pragma warning disable ASPIREJAVASCRIPT001 // AddNextJsApp is Experimental in Aspire 13

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var db = postgres.AddDatabase("minibankdb");

var api = builder.AddProject<Projects.MiniBank_Api>("api")
    .WithReference(db)
    .WaitFor(db);

// Web frontend (Next.js) — pinned to :3000 to match API CORS defaults + local npm scripts.
// NEXT_PUBLIC_API_URL is resolved from the `api` http endpoint via service discovery;
// when running `npm run dev` outside Aspire the shared client falls back to http://localhost:5194.
var web = builder.AddNextJsApp("web", "../Frontend/web")
    .WithReference(api)
    .WithHttpEndpoint(port: 3000, name: "http")
    .WithEnvironment("NEXT_PUBLIC_API_URL", api.GetEndpoint("http"));

// Mobile frontend (Vite + Capacitor web build) — pinned to :5173 to match API CORS defaults.
// VITE_API_URL is resolved the same way; `npm run dev` outside Aspire falls back to :5194.
var mobile = builder.AddViteApp("mobile", "../Frontend/mobile")
    .WithReference(api)
    .WithHttpEndpoint(port: 5173, name: "http")
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"));

builder.Build().Run();
