using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;



namespace speech_function
{
    public class DataSpeech
    {
        [JsonProperty("textspeech")]
        public string Textspeech { get; set; }
    }
    public class Response<T>
    {
        [JsonProperty("data")]
        public T Data { get; set; }
        [JsonProperty("success")]
        public bool Success { get; set; }
        [JsonProperty("method")]
        public string Method { get; set; }
    }

    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "audio/")] HttpRequest req,
            [CosmosDB(
                databaseName: "dbproyecto",
                collectionName: "SpeechData",
                ConnectionStringSetting = "strCosmos")]
                IAsyncCollector<DataSpeech> itemSpeech,
            ILogger log)
        {

            // <--------------------------------- Abrir el Archivo y leerlo ----------------------------------> //
            var audio = req.Form.Files[0].OpenReadStream();
            //var texto = await new StreamReader(audio).ReadToEndAsync();


            // <--------------------------------- Esto es para el Speech -------------------------------------> //
            var speechConfig = SpeechConfig.FromSubscription("4d803319d5f644ee9355a538f740f55b", "eastus");
            speechConfig.SpeechRecognitionLanguage = "es-ES";

            var reader = new BinaryReader(audio);
            using var audioInputStream = AudioInputStream.CreatePushStream();
            using var audioConfig = AudioConfig.FromStreamInput(audioInputStream);
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            byte[] readBytes;
            do
            {
                readBytes = reader.ReadBytes(1024);
                audioInputStream.Write(readBytes, readBytes.Length);
            } while (readBytes.Length > 0);

            var result = await recognizer.RecognizeOnceAsync();
            Console.WriteLine($"RECOGNIZED: Text={result.Text}");

            // <---------------------------------- Viene el Http Trigger ---------------------------------------> //


            var audioText = new DataSpeech()
            {
                Textspeech = result.Text
            };
            await itemSpeech.AddAsync(audioText);

            var Response = new Response<DataSpeech>
            {
                Data = audioText,
                Success = true,
                Method = "POST"
            };

            
            return new OkObjectResult(Response);
        }
    }
}
