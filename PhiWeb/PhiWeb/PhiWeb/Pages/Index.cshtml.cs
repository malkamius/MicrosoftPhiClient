using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System;
using Microsoft.AspNetCore.Http;
using static PhiWeb.Pages.IndexModel;

namespace PhiWeb.Pages
{
    public class IndexModel : PageModel
    {
        public class Message
        {
            public string role { get; set; } = "";
            public string content { get; set; } = "";
        }
        private readonly ILogger<IndexModel> _logger;
        private readonly HttpClient _httpClient;

        public IndexModel(ILogger<IndexModel> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        private string CurrentMessage = string.Empty;
        private List<Message> Messages = new List<Message>();
        private string LastText = string.Empty;
        private string url = "http://localhost:9090/generate";
        private string WholeMessage;

        public async Task OnGet()
        {
            CurrentMessage = ""; // Simulating message storage
            HttpContext.Session.SetString("CurrentMessage", "");
            HttpContext.Session.SetString("WholeMessage", "");
            HttpContext.Session.SetString("LastText", "");
            var serializedMessages = JsonSerializer.Serialize(new List<Message>());
            HttpContext.Session.SetString("Messages", serializedMessages);
            HttpContext.Session.SetInt32("StopGenerating", 0);
            await HttpContext.Session.CommitAsync();
        }

        // Handler for sending messages
        public async Task<IActionResult> OnPostSendMessage(string message)
        {
            await HttpContext.Session.LoadAsync();
            var messages = HttpContext.Session.GetString("Messages");
            if (!string.IsNullOrEmpty(messages))
            {
                Messages = JsonSerializer.Deserialize<List<Message>>(messages);
            }
            //CurrentMessage = HttpContext.Session.GetString("CurrentMessage");
            //LastText = HttpContext.Session.GetString("LastText");
            //WholeMessage = HttpContext.Session.GetString("WholeMessage");

            // Store or send the message to your Flask application here
            CurrentMessage = message; // Simulating message storage
            Messages.Add(new Message { role = "user", content = message });
            HttpContext.Session.SetString("CurrentMessage", CurrentMessage);
            HttpContext.Session.SetString("WholeMessage", "");
            HttpContext.Session.SetString("LastText", "");
            var serializedMessages = JsonSerializer.Serialize(Messages);
            HttpContext.Session.SetString("Messages", serializedMessages);
            HttpContext.Session.SetInt32("StopGenerating", 0);
            await HttpContext.Session.CommitAsync();
            // Simulate sending message to another system (e.g., Flask)
            // Here you might call a service that interacts with Flask
            return new JsonResult("Message sent: " + message);
        }

        // Handler for checking messages
        public async Task<IActionResult> OnGetCheckMessages()
        {
            await HttpContext.Session.LoadAsync();
            var messages = HttpContext.Session.GetString("Messages") ?? "";
            if (!string.IsNullOrEmpty(messages))
            {
                Messages = JsonSerializer.Deserialize<List<Message>>(messages);
            }
            CurrentMessage = HttpContext.Session.GetString("CurrentMessage");
            LastText = HttpContext.Session.GetString("LastText");
            WholeMessage = HttpContext.Session.GetString("WholeMessage");

            string myResponse = "";
            if (!string.IsNullOrEmpty(CurrentMessage))
            {
                var jsonData = JsonSerializer.Serialize(new { messages = Messages, lasttext = WholeMessage }, new JsonSerializerOptions { WriteIndented = true });
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                try
                {
                    var response = await _httpClient.PostAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        //Console.WriteLine("Received: " + responseString);
                        var responseJson = JsonDocument.Parse(responseString);
                        
                        
                            
                        LastText = responseJson.RootElement.GetString() ?? "";// responseString.Trim('"');
                        await HttpContext.Session.LoadAsync();
                        var newmessages = HttpContext.Session.GetString("Messages") ?? "";
                        var currentMessage = HttpContext.Session.GetString("CurrentMessage") ?? "";
                        var stopgenerating = HttpContext.Session.GetInt32("StopGenerating");
                        if (newmessages != messages || string.IsNullOrEmpty(currentMessage) || stopgenerating != 0)
                        {
                            throw new OperationCanceledException();
                        }
                        WholeMessage = WholeMessage + LastText;
                        if (string.IsNullOrEmpty(LastText))
                        {
                            LastText = LastText + "--end of text";

                            
                            Messages.Add(new Message { role = "assistant", content = WholeMessage });
                            var serializedMessages = JsonSerializer.Serialize(Messages);
                            HttpContext.Session.SetString("Messages", serializedMessages);
                            WholeMessage = "";
                            CurrentMessage = "";
                            myResponse = LastText;
                            LastText = "";
                        } 
                        else
                        {
                            myResponse = LastText;
                        }

                        HttpContext.Session.SetString("CurrentMessage", CurrentMessage);
                        HttpContext.Session.SetString("WholeMessage", WholeMessage);
                        HttpContext.Session.SetString("LastText", LastText);
                        await HttpContext.Session.CommitAsync();



                    }
                    else
                    {
                        myResponse = "Error calling API. Status Code: " + response.StatusCode + "--end of text";
                    }
                }
                catch (Exception ex)
                {
                    HttpContext.Session.SetString("CurrentMessage", CurrentMessage);
                    HttpContext.Session.SetString("WholeMessage", WholeMessage);
                    HttpContext.Session.SetString("LastText", LastText);
                    await HttpContext.Session.CommitAsync();
                    myResponse = "Exception caught: " + ex.Message + "--end of text";
                }
                
                if (myResponse.EndsWith("--end of text")) // Example condition
                {
                    CurrentMessage = string.Empty; // Reset after end condition
                    LastText = string.Empty;
                    WholeMessage = string.Empty;
                    HttpContext.Session.SetString("CurrentMessage", CurrentMessage);
                    HttpContext.Session.SetString("WholeMessage", WholeMessage);
                    HttpContext.Session.SetString("LastText", LastText);
                    await HttpContext.Session.CommitAsync();
                }
                return new JsonResult(myResponse);
            }
            return new JsonResult("--end of text");
        }

        public async Task<IActionResult> OnGetStopProcessing()
        {
            await HttpContext.Session.LoadAsync();
            var messages = HttpContext.Session.GetString("Messages");
            if (!string.IsNullOrEmpty(messages))
            {
                Messages = JsonSerializer.Deserialize<List<Message>>(messages);
            }
           
            WholeMessage = HttpContext.Session.GetString("WholeMessage");

            CurrentMessage = string.Empty;
            LastText = string.Empty;
            if (!string.IsNullOrEmpty(WholeMessage))
            {
                Messages.Add(new Message { role = "assistant", content = WholeMessage + "--end of text" });
                
            }
            WholeMessage = "";
            var serializedMessages = JsonSerializer.Serialize(Messages);
            HttpContext.Session.SetString("Messages", serializedMessages);
            HttpContext.Session.SetString("CurrentMessage", CurrentMessage);
            HttpContext.Session.SetString("WholeMessage", WholeMessage);
            HttpContext.Session.SetString("LastText", LastText);
            HttpContext.Session.SetInt32("StopGenerating", 1);
            await HttpContext.Session.CommitAsync();
            return new JsonResult("Stopping generation of text.");
        }
    }
}
