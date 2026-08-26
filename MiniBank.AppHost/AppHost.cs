var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.MiniBank_Api>("MiniBank-api");

builder.Build().Run();
