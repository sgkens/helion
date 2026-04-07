namespace Helion.Host.Orchestration;

using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Helion.Host.Telemetry;

public class DockerProvider
{
    private readonly DockerClient _client;
    private readonly LogDispatcher _logger;

    public DockerProvider(LogDispatcher logger)
    {
        _logger = logger;
        
        string dockerUri = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? "npipe://./pipe/docker_engine" 
            : "unix:///var/run/docker.sock";

        _client = new DockerClientConfiguration(new Uri(dockerUri)).CreateClient();
    }

    public async Task<bool> EnsureContainerRunningAsync(string containerName, string image, Dictionary<string, string> envVars, Dictionary<string, string> portMappings)
    {
        // 20-Pass Fix: Orchestration Idempotency
        try 
        {
            var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters { All = true });
            foreach (var container in containers)
            {
                if (container.Names.Contains("/" + containerName))
                {
                    if (container.State != "running")
                    {
                        _logger.LogInfo("Docker", $"Starting existing container: {containerName}");
                        await _client.Containers.StartContainerAsync(container.ID, null);
                    }
                    return true;
                }
            }

            _logger.LogInfo("Docker", $"Provisioning new container: {containerName} from image: {image}");
            
            // Pull image if not exists
            await _client.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = image }, null, new Progress<JSONMessage>(m => {
                if (m.Status != null) _logger.LogInfo("Docker", $"Pulling {image}: {m.Status}");
            }));

            var hostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>()
            };

            foreach (var mapping in portMappings)
            {
                hostConfig.PortBindings.Add(mapping.Key, new List<PortBinding> { new PortBinding { HostPort = mapping.Value } });
            }

            var response = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = image,
                Name = containerName,
                Env = BuildEnvList(envVars),
                HostConfig = hostConfig
            });

            return await _client.Containers.StartContainerAsync(response.ID, null);
        }
        catch (Exception ex)
        {
            _logger.LogError("Docker", ex);
            return false;
        }
    }

    public async Task StartAllHelionContainersAsync()
    {
        try
        {
            var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters { All = true });
            foreach (var container in containers)
            {
                if (container.Names.Any(n => n.Contains("/helion-")))
                {
                    if (container.State != "running")
                    {
                        _logger.LogInfo("Docker", $"Recovering container: {container.Names.First()}");
                        await _client.Containers.StartContainerAsync(container.ID, null);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Docker", ex);
        }
    }

    private List<string> BuildEnvList(Dictionary<string, string> envVars)
    {
        var list = new List<string>();
        foreach (var kvp in envVars)
        {
            list.Add($"{kvp.Key}={kvp.Value}");
        }
        return list;
    }
}
