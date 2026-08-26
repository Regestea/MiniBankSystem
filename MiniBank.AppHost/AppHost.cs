var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL — provisioned & wired by Aspire (container + pgAdmin UI)
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var db = postgres.AddDatabase("minibankdb");

builder.AddProject<Projects.MiniBank_Api>("api")
    .WithReference(db)   // injects ConnectionStrings__minibankdb
    .WaitFor(db);        // API starts only after DB is ready

builder.Build().Run();
