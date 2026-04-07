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
        if (!File.Exists(filePath))
        {
            return GenerateDefaultConfig(filePath);
        }

        var yaml = File.ReadAllText(filePath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<HelionConfig>(yaml);
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
