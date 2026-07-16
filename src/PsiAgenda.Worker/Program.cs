using PsiAgenda.Infrastructure;
using PsiAgenda.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfraestrutura(builder.Configuration);
builder.Services.AddHostedService<MigracaoESeedWorker>();

var host = builder.Build();
host.Run();
