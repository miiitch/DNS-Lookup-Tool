var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.mpelabs_dnslookup>("dnslookup")
	.WithExternalHttpEndpoints()
	.WithHealthCheck("/health");

builder.Build().Run();
