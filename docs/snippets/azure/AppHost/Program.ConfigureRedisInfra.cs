﻿using Azure.Provisioning.Redis;
using RedisResource = Azure.Provisioning.Redis.RedisResource;

internal static partial class Program
{
    public static void ConfigureRedisInfra(IDistributedApplicationBuilder builder)
    {
        // <configure>
        builder.AddAzureRedis("redis")
            .WithAccessKeyAuthentication()
            .ConfigureInfrastructure(infra =>
            {
                var redis = infra.GetProvisionableResources()
                                 .OfType<RedisResource>()
                                 .Single();

                redis.Sku = new()
                {
                    Family = RedisSkuFamily.BasicOrStandard,
                    Name = RedisSkuName.Standard,
                    Capacity = 1,                    
                };
                redis.Tags.Add("ExampleKey", "Example value");
            });
        // </configure>
    }
}
