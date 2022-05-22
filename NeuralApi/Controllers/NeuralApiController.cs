using Microsoft.AspNetCore.Mvc;
using NeuralApi.RabbitMq;

namespace NeuralApi.Controllers
{
    [Route("api")]
    [ApiController]
    public class NeuralApiController : ControllerBase
    {
        private readonly IRabbitMqService _mq_service;
        private static string _path = "/neuralapi/images";
        IWebHostEnvironment _app_env;
        public NeuralApiController(IRabbitMqService mq_service, IWebHostEnvironment app_env)
        {
            _mq_service = mq_service;
            _app_env = app_env;
        }


        /// <summary>
        /// Upload file to be proccessed to server.
        /// </summary>
        [HttpPost]
        [HttpPost]
        [Route("upload")]
        public async Task<IActionResult> AddFile(IFormFile uploadedFile)
        {
            if (uploadedFile != null)
            {
                var name = uploadedFile.FileName;


                string path = _path + "/" + name;
                using (var fileStream = new FileStream(path, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(fileStream);
                }

                var id = await _mq_service.PutTask(path);
                return Ok("Id = " + id.ToString());
           
            }
            return NoContent();
        }

        [HttpGet]
        [Route("{task_id}/status")]
        public async Task<IActionResult> Status(int task_id)
        {
            return Ok(await _mq_service.GetTaskStatus(task_id));
        }

        [HttpGet]
        [Route("{task_id}/download")]
        public async Task<IActionResult> Download(int task_id)
        {
            if (await _mq_service.GetTaskStatus(task_id) != "SUCCESS")
            {
                return NoContent();  
            }
            var file = await _mq_service.GetReadyTask(task_id);
            var path = Path.Combine(_path, file.Path); //validate the path for security or use other means to generate the path.
            var stream = System.IO.File.OpenRead(path);
            var result = new FileStreamResult(stream, "application/octet-stream");
            result.FileDownloadName = task_id.ToString() + ".mat";
            return result;
        }
    }

}