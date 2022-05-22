namespace NeuralApi.RabbitMq
{
    using NeuralApi.Database;
    public interface IRabbitMqService
    {
        public Task<int> PutTask(string path);
        public Task<string> GetTaskStatus(int id);
        public Task<FileModel> GetReadyTask(int id);
    }
}
