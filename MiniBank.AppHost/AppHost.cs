var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var db = postgres.AddDatabase("minibankdb");

builder.AddProject<Projects.MiniBank_Api>("api")
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();
