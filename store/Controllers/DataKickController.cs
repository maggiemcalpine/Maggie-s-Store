using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace store.Controllers
{

    public class DataKickController : Controller
    {


        public IActionResult Index()
        {
            //The WebClient is a built-in DotNet object for making HTTP Requests.
            System.Net.WebClient webClient = new System.Net.WebClient();

            //I typically use "headers" to configure my request to the API.  In this case, I'm saying that I want to the response
            //as JSON instead of HTML. I might also use a header to specify my API Key.  Every API is going to work a little bit\
            //differently here, so read the documentation!
            webClient.Headers.Add("accept", "application/json");


            string openTriviaJson = webClient.DownloadString("https://opentdb.com/api.php?amount=10");

            //Almost all of our APIs use JSON formatting.  DotNet has a crappy built-in JSON parser, so most apps use the 
            //NewtonSoft.Json NuGet package instead.  See the "DadJoke" class below.
            OpenTriviaItem openTriviaItems = Newtonsoft.Json.JsonConvert.DeserializeObject<OpenTriviaItem>(openTriviaJson);


            return View("index", openTriviaItems.question);
        }
    }

    //Easiest way to read JSON is to create simple objects that look the same as the JSON response object.
    //If I have these objects, I can use the JsonConvert class to quickly read JSON data.
    public class OpenTriviaItem
    {
        public int id { get; set; }
        public string category { get; set; }
        public string type { get; set; }
        public string difficulty { get; set; }
        public string question { get; set; }
       
    }
}
