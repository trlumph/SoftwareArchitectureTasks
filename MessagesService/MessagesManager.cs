using ConsulManagerService;
using Hazelcast;

namespace MessagesService;

public interface IMessagesManager
{
    Task StartListening(CancellationToken cancellationToken);
    Task<List<string>> GetAllMessagesAsync();
}

public class MessagesManager : IMessagesManager
{
    private readonly IHazelcastClient _hzClient;
    private readonly List<string> _messages = new();
    private readonly MessageQueueConfigurationService _messageQueueConfigService;

    public MessagesManager(IHazelcastClient hzClient, MessageQueueConfigurationService messageQueueConfigService)
    {
        _hzClient = hzClient;
        _messageQueueConfigService = messageQueueConfigService;
    }

    public async Task StartListening(CancellationToken cancellationToken)
    {
        var queueName = await _messageQueueConfigService.GetMQNameAsync();
        var queue = await _hzClient.GetQueueAsync<string>(queueName);
        
        while (!cancellationToken.IsCancellationRequested)
        {
            // Blocks until a message is available
            var message = await queue.TakeAsync();
            if (message is not null)
            {
                _messages.Add(message);
                Console.WriteLine($"Message received and processed: {message}");
            }
        }
    }

    public Task<List<string>> GetAllMessagesAsync()
    {
        return Task.FromResult(_messages);
    }
}