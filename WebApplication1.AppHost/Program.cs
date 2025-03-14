var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.GradeProcessor>("gradeprocessor");

builder.Build().Run();
