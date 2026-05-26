var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.mpelabs_dnslookup>("dnslookup")
	.WithExternalHttpEndpoints();

builder.Build().Run();
