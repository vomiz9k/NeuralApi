namespace NeuralWorker.Worker
{
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
	using System.Text;
	using System.Text.Json;
	using Newtonsoft.Json.Linq;
	using Microsoft.VisualStudio.Threading;
	using System.Diagnostics;
	public class Worker
    {
        private ConnectionFactory _factory = new ConnectionFactory() { HostName = "host.docker.internal", Port = 5672 };
        private IModel _producer_channel;
		private IModel _consumer_channel;
		private EventingBasicConsumer _consumer;


        public Worker()
        {
			IConnection connection = _factory.CreateConnection();

			_producer_channel = connection.CreateModel();
			_producer_channel.QueueDeclare(queue: "Ready",
								durable: false,
								exclusive: false,
								autoDelete: false,
								arguments: null);


			_consumer_channel = connection.CreateModel();
			_consumer_channel.QueueDeclare(queue: "Pending",
								durable: false,
								exclusive: false,
								autoDelete: false,
								arguments: null);
			_consumer = new EventingBasicConsumer(_consumer_channel);
			_consumer.Received += (model, eventArgs) =>
			{
				var body = eventArgs.Body.ToArray();
				var message = JObject.Parse(Encoding.UTF8.GetString(body));
				PerformImage(message["Path"].ToString(), Int32.Parse(message["Id"].ToString()));
			};
			
		}

		public void Start()
        {
			_consumer_channel.BasicConsume("Pending", true, _consumer);
			while(true)
            {
				Thread.Sleep(1000);
            }
		}
		
		private void PerformImage(string path, int id)
        {
			var out_path = "/neuralapi/images/" + id.ToString() + ".mat";
			Process.Start("python3", "main.py --i " + path + " --o " + out_path);
			var message = "{ \"Path\": \"" + out_path + "\", \"Id\": " + id.ToString() + "}";
			var body = Encoding.UTF8.GetBytes(message);
			_producer_channel.BasicPublish(exchange: "",
							routingKey: "Ready",
							basicProperties: null,
							body: body);
		}
	}
}
