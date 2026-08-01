var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.InfoTrack_Api>("api");

builder.AddNpmApp("web", "../InfoTrack.Web", "dev", ["--", "--host", "0.0.0.0", "--port", "5173", "--strictPort"])
	.WithReference(api)
	.WaitFor(api)
	.WithHttpEndpoint(targetPort: 5173, port: 5173, isProxied: false);

builder.Build().Run();
