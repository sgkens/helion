namespace Helion.Host.Configuration;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO;

public class HelionConfig
{
    public InfrastructureConfig Infrastructure { get; set; } = new();
    public SecurityConfig Security { get; set; } = new();

    public static HelionConfig Load(string filePath = "helion.yml")
    {
        HelionConfig config;
        if (File.Exists(filePath))
        {
            var yaml = File.ReadAllText(filePath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            config = deserializer.Deserialize<HelionConfig>(yaml);
        }
        else
        {
            config = GenerateDefaultConfig(filePath);
        }

        // 20-Pass Fix: Config Interpolation (Env Vars override YAML)
        ApplyEnvironmentOverrides(config);

        return config;
    }

    private static void ApplyEnvironmentOverrides(HelionConfig config)
    {
        var infraConn = Environment.GetEnvironmentVariable("HELION_INFRA_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(infraConn)) config.Infrastructure.ConnectionString = infraConn;

        var jwtTtl = Environment.GetEnvironmentVariable("HELION_SECURITY_JWT_TTL");
        if (int.TryParse(jwtTtl, out var ttl)) config.Security.JwtTtlSeconds = ttl;

        var acmeEnabled = Environment.GetEnvironmentVariable("HELION_SECURITY_ACME_ENABLED");
        if (bool.TryParse(acmeEnabled, out var enabled)) config.Security.EnableAcme = enabled;
    }

    private static HelionConfig GenerateDefaultConfig(string filePath)
    {
        var config = new HelionConfig
        {
            Infrastructure = new InfrastructureConfig 
            { 
                Strategy = "managed_local", 
                Engine = "postgres:16",
                ConnectionString = "Host=localhost;Database=helion_system;Username=postgres;Password=postgres"
            },
            Security = new SecurityConfig 
            { 
                JwtTtlSeconds = 3600,
                EnableAcme = false 
            }
        };

        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
            
        File.WriteAllText(filePath, serializer.Serialize(config));
        return config;
    }
}

public class InfrastructureConfig
{
    public string Strategy { get; set; } = "managed_local";
    public string Engine { get; set; } = "postgres:16";
    public string ConnectionString { get; set; } = "";
}

public class SecurityConfig
{
    public int JwtTtlSeconds { get; set; } = 3600;
    public bool EnableAcme { get; set; } = false;
    public string? AcmeEmail { get; set; }
}
