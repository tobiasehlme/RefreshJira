
using System.Text.Json;
using PuppeteerSharp;

namespace RefreshJira
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;


        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var browserPath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            var headless = false;

            if (File.Exists("cookies.json"))
            {
                headless = true;
            }

            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = false,
                ExecutablePath = browserPath,
            });

            using var page = await browser.NewPageAsync();
            var url = "https://lexitgroup.atlassian.net/jira/servicedesk/projects/SR/issues/?filter=10869";
            if (File.Exists("cookies.json"))
            {
                var cookiesJson = await File.ReadAllTextAsync("cookies.json");
                var cookies = JsonSerializer.Deserialize<CookieParam[]>(cookiesJson);
                await page.SetCookieAsync(cookies);
                Console.WriteLine("-- Cookies loaded. --");
                await page.GoToAsync(url);

            }
            else
            {
                await page.GoToAsync(url);
                Console.WriteLine("Log in to the website manually...");
                Console.WriteLine("Press Enter when done.");
                Console.ReadLine();

                var cookies = await page.GetCookiesAsync();
                await File.WriteAllTextAsync("cookies.json", JsonSerializer.Serialize(cookies));
                await browser.CloseAsync();


                Console.WriteLine("-- Saved cookies --");
                Console.WriteLine("-- Please restart app --");
                Console.ReadLine();
            }

            while (true)
            {
                Console.Clear();
                Console.WriteLine($"Page refreshed at {DateTime.Now}.");
                Console.WriteLine("Fetching tickets... \n");
                try
                {
                    
                    var waitForLink = await page.WaitForSelectorAsync("#ak-main-content > div > div:nth-child(4) > div:nth-child(1) > div > table > tbody");


                    var rows = await page.QuerySelectorAllAsync("#ak-main-content > div > div:nth-child(4) > div:nth-child(1) > div > table > tbody > tr");

                    foreach (var row in rows)
                    {
                        var columns = await row.QuerySelectorAllAsync("td");
                        foreach (var column in columns)
                        {
                            var text = await column.EvaluateFunctionAsync<string>("e => e.textContent");
                            Console.Write(text + " ");
                        }
                    }

                    //var issueTexts = await page.EvaluateFunctionAsync<List<string>>(@"
                    //    () => Array.from(document.querySelectorAll('.issue-link')).map(element => element.textContent.trim())
                    //");

                    //if (createdCount != 0)
                    //{
                    //    foreach (var issueText in issueTexts)
                    //    {
                    //        if (!issueText.StartsWith("SR"))
                    //        {
                    //            Console.WriteLine($"{issueText} \n");
                    //        }
                    //        else
                    //        {
                    //            Console.WriteLine($"\x1b]8;;{serviceDeskUrl}/{issueText}\x1b\\Ctrl + Click me to open! \x1b]8;;\x1b\\");
                    //        }
                    //    }
                    //}
                    //if (createdCount >= 10)
                    //{
                    //    Console.WriteLine("Displaying the 10 newest unhandled tickets.");
                    //}
                    //Console.WriteLine($"Total unhandled tickets - {createdCount}");
                    //await Task.Delay(60000); // Refresh every 1 minute
                    
                }
                catch (PuppeteerSharp.WaitTaskTimeoutException ex)
                {
                    for (int i = 10; i >= 0; i--)
                    {
                        Console.WriteLine($"Can't find any tickets at the moment, retrying in {i}s...");
                        await Task.Delay(1000);
                    }
                }
            }
        }
    }
}
