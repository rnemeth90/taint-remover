using k8s;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using k8s.Authentication;
using System.IO;

IHost host = null;

try {
  host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
      services.AddHostedService<Worker>();
      services.AddSingleton((s) => {
        // modified for running in a windows HPC on containerd 1.6 
        // https://kubernetes.io/docs/tasks/configure-pod-container/create-hostprocess-pod/#volume-mounts
        var ServiceAccountPath = Path.Combine(Environment.GetEnvironmentVariable("CONTAINER_SANDBOX_MOUNT_POINT"), "var", "run", "secrets", "kubernetes.io", "serviceaccount");
        var rootCAFile = Path.Combine(ServiceAccountPath, "ca.crt");
        var namespaceFile = Path.Combine(ServiceAccountPath, "namespace");
        var tokenFile = new TokenFileAuth(Path.Combine(ServiceAccountPath, "token"));
        var host = Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST");
        var port = Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_PORT");
        var config = new KubernetesClientConfiguration
        {
            Host = new UriBuilder("https", host, Convert.ToInt32(port)).ToString(),
            TokenProvider = tokenFile,
            SslCaCerts = CertUtils.LoadPemFileCert(rootCAFile),
        };
        config.Namespace = File.ReadAllText(namespaceFile);
        return new Kubernetes(config);
      });
    })
    .ConfigureLogging((_, logging) => 
    {
      logging.AddSimpleConsole(options => 
      { 
        options.IncludeScopes = true; 
        options.SingleLine = true; 
      });
    })
    .ConfigureAppConfiguration((config) => {
      config.AddEnvironmentVariables();
    })
    .Build();


  await host.StartAsync();
  await host.StopAsync();
  await host.WaitForShutdownAsync();
}
finally
{
  if (host is IAsyncDisposable d) await d.DisposeAsync();
}