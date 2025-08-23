var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.MiniBankSystem_API>("minibanksystem-api");

builder.Build().Run();
