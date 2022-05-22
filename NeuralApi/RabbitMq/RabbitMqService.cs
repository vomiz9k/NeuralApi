namespace NeuralApi.RabbitMq
{
	using RabbitMQ.Client;
	using RabbitMQ.Client.Events;
	using System.Text;
	using System.Text.Json;
	using NeuralApi.Database;
	using Newtonsoft.Json.Linq;

	public class RabbitMqService : IRabbitMqService
	{
		private static ConnectionFactory _factory = new ConnectionFactory() { HostName = "host.docker.internal", Port = 5672 };
		private static IConnection _connection = _factory.CreateConnection();
		private IModel _channel;
		
        private static ReadyContext _ready;
		private static PendingContext _pending;
		private static int _counter = 1;

		public RabbitMqService(PendingContext pending, ReadyContext ready)
		{
			_channel = _connection.CreateModel();
			_channel.QueueDeclare(queue: "Pending",
								durable: false,
								exclusive: false,
								autoDelete: false,
								arguments: null);

			_pending = pending;
			_ready = ready;
		}

		public async Task<int> PutTask(string path)
		{
			var id = _counter;
			++_counter;
			var file_model = new FileModel { Id = id, Path = path };
			var message = JsonSerializer.Serialize(file_model);
			var body = Encoding.UTF8.GetBytes(message);

			_channel.BasicPublish(exchange: "",
							routingKey: "Pending",
							basicProperties: null,
							body: body);
			await _pending.Files.AddAsync(file_model);
			await _pending.SaveChangesAsync();
			return id;
		}

		public async Task<FileModel> GetReadyTask(int id)
        {
			var file = await _ready.Files.FindAsync(id);
			return file;
		}

		public async Task<string> GetTaskStatus(int id)
		{
			var file = await _ready.Files.FindAsync(id);
			if (file != null)
            {
				return "SUCCESS";
            }
			file = await _pending.Files.FindAsync(id);
			if (file != null)
            {
				return "PENDING";
            }
			return "FAILURE";
		}
	}
}
