using Hazelcast;
using Hazelcast.DistributedObjects;

namespace HazelcastBasics;

class HazelcastBasics
{
    public static async Task Main()
    {
        var options = new HazelcastOptionsBuilder().Build();
        options.Networking.Addresses.Add("localhost:5701");
        options.Networking.Addresses.Add("localhost:5702");
        options.Networking.Addresses.Add("localhost:5703");
        
        // Create Hazelcast client and connect to the cluster
        var hz = await HazelcastClientFactory.StartNewClientAsync(options);

        //await DistributedMapExample(hz);
        //await NoLocks(hz);
        await PessimisticLocks(hz);
        // await OptimisticLocks(hz);
        await hz.DisposeAsync();
    }

    private static async Task DistributedMapExample(IHazelcastClient hz)
    {
        // Get or Create a Distributed Map
        var map = await hz.GetMapAsync<int, string>("my-distributed-map");

        // Populate the map with 1000 entries
        for (var i = 0; i < 1000; i++)
        {
            await map.SetAsync(i, $"Value {i}");
        }

        Console.WriteLine("1000 entries added to the Distributed Map");
    }

    private static async Task NoLocks(IHazelcastClient hz)
    {
        var map = await hz.GetMapAsync<string, int>("my-distributed-map-2");
        await map.PutAsync("key", 0);

        var tasks = new List<Task>();

        for (var i = 0; i < 3; i++)
        {
            var task = Task.Run(async () =>
            {
                for (var k = 0; k < 10_000; k++)
                {
                    var currentValue = await map.GetAsync("key");
                    currentValue++;
                    await map.PutAsync("key", currentValue);
                }
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        var finalValue = await map.GetAsync("key");
        Console.WriteLine($"Final value without locks: {finalValue}");
    }

    private static async Task PessimisticLocks(IHazelcastClient hz)
    {
        var map = await hz.GetMapAsync<string, int>("my-distributed-map-2");
        await map.PutAsync("key", 0);
        
        var tasks = new List<Task>();

        for (var i = 0; i < 3; i++)
        {
            var task = Task.Run(async () =>
            {
                for (var k = 0; k < 10_000; k++)
                {
                    await map.LockAsync("key");
                    try
                    {
                        var value = await map.GetAsync("key");
                        value++;
                        await map.PutAsync("key", value);
                    }
                    finally
                    {
                        await map.UnlockAsync("key");
                    }
                }
            });
            
            tasks.Add(task);
        }
        
        await Task.WhenAll(tasks);        

        var finalValue = await map.GetAsync("key");
        Console.WriteLine($"Final value with pessimistic locking: {finalValue}");
    }
    
    private static async Task OptimisticLocks(IHazelcastClient hz)
    {
        var map = await hz.GetMapAsync<string, int>("my-distributed-map-2");
        await map.PutAsync("key", 0);
        
        var tasks = new List<Task>();

        for (var i = 0; i < 3; i++)
        {
            var task = Task.Run(async () =>
            {
                for (var k = 0; k < 10_000; k++)
                {
                    var success = false;
                    while (!success) {
                        var oldValue = await map.GetAsync("key");
                        var newValue = oldValue + 1;
                        success = await map.ReplaceAsync("key", oldValue, newValue);
                    }
                }
            });
            
            tasks.Add(task);
        }
        
        await Task.WhenAll(tasks);        

        var finalValue = await map.GetAsync("key");
        Console.WriteLine($"Final value with optimistic locking: {finalValue}");
    }
}