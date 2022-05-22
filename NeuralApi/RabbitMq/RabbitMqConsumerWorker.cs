namespace NeuralApi.RabbitMq
{
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using System.Text;
    using System.Text.Json;
    using NeuralApi.Database;
    using Newtonsoft.Json.Linq;

    public class RabbitMqConsumerWorker : IHostedService
    {
        private ConnectionFactory _factory = new ConnectionFactory() { HostName = "host.docker.internal", Port = 5672 };
        private IModel _consumer_channel;
        private IConnection _connection;
        private readonly IServiceScopeFactory _scope_factory;
        private static bool _cancel = false;

        public RabbitMqConsumerWorker(IServiceScopeFactory scope_factory)
        {
            _scope_factory = scope_factory;
        }

        public async Task StartAsync(CancellationToken token)
        {
            _cancel = token.IsCancellationRequested;

            _connection = _factory.CreateConnection();
            _consumer_channel = _connection.CreateModel();
            _consumer_channel.QueueDeclare(queue: "Ready",
                                durable: false,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);

            Task.Run(() => Consume());
        }

        public async Task StopAsync(CancellationToken token)
        {
            _cancel = token.IsCancellationRequested;
            _consumer_channel.Close();
            _connection.Close();
        }
        private async Task Consume()
        {
            using (var scope = _scope_factory.CreateScope())
            {
                var ready = scope.ServiceProvider.GetRequiredService<ReadyContext>();
                var pending = scope.ServiceProvider.GetRequiredService<PendingContext>();

                while (!_cancel)
                {
                    var result = _consumer_channel.BasicGet("Ready", false);
                   
                    if (result == null)
                    {
                        continue;
                    }
                    var body = result.Body.ToArray();
                    var message = JObject.Parse(Encoding.UTF8.GetString(body));
                    var file_model = new FileModel { Id = Int32.Parse(message["Id"].ToString()), Path = message["Path"].ToString() };
                    await ready.Files.AddAsync(file_model);
                    var file_model_old = await pending.Files.FindAsync(file_model.Id);
                    if (file_model_old != null)
                        pending.Files.Remove(file_model_old);
                    await pending.SaveChangesAsync();
                    await ready.SaveChangesAsync();
                }
            }
           
        }
    }
}
