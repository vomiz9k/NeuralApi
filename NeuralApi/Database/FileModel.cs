using System.ComponentModel.DataAnnotations;


namespace NeuralApi.Database
{
    public class FileModel
    {
        
        public string Path { get; set; }
        [Key]
        public int Id { get; set; }
    }
}
