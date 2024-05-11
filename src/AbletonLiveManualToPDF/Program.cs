using HtmlAgilityPack;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.Net.Http.Headers;
using System.Text;

namespace AbletonLiveManualToPDF
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var nameOfProject = "Ableton Live Manual To PDF";

            PrintHeaderInformation(nameOfProject);

            // Reading Settings...
            Console.ResetColor();
            Console.Write(" ");
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(" (1 of 4) Reading Settings... ");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine();

            var settings = new Settings();

            if (!settings.ValidationResult.IsValidated)
            {
                // VALIDATION ERROR
                Console.ResetColor();
                Console.Write(" ");
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(" Validation Error ");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($" {settings.ValidationResult.Message} ");
                Console.ResetColor();
                Console.WriteLine();
            }
            else
            {
                try
                {
                    // Print the Settings details
                    PrintSettingsInformation(settings);

                    // Reading Html Content
                    Console.ResetColor();
                    Console.Write(" ");
                    Console.BackgroundColor = ConsoleColor.Cyan;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.Write(" (2 of 4) Reading Html Content... ");
                    Console.ResetColor();
                    Console.WriteLine();
                    Console.WriteLine();

                    // Get the links of the Ableton Live Manual
                    var links = GetLinks(settings);

                    // General information about the links (each link is a web page)
                    Console.Write($"\tNumber of Pages: ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"'{links.Count}'");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.ResetColor();
                    Console.WriteLine();

                    // Read the HTML Content from Ableton Live web site
                    var htmlContent = await ReadHtmlContentAsync(links, settings.HeaderPage);

                    // Write the HTML Content parsed to disk
                    WriteHtmlFile(settings.HtmlFilePath, htmlContent);

                    // Write the PDF from the HTML file parsed
                    await WritePdfFileAsync(settings);
                }
                catch (Exception ex)
                {
                    // ERROR
                    Console.ResetColor();
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($" ERROR: {ex.Message} ");
                    Console.ResetColor();
                    Console.WriteLine();
                }
            }

            // Process Finished!
            Console.ResetColor();
            Console.Write(" ");
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(" The process has finished! ");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine();
        }

        private static void PrintHeaderInformation(string nameOfProject)
        {
            Console.WriteLine();
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine($" {new String('*', nameOfProject.Length + 4)} ");
            Console.Write($" * ");
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write($"{nameOfProject}");
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine($" * ");
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine($" {new String('*', nameOfProject.Length + 4)} ");
            Console.ResetColor();
            Console.WriteLine();
        }

        private static void PrintSettingsInformation(Settings settings)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\tHomePage: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"'{settings.HomePage}'");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\tHtmlFilePath: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"'{settings.HtmlFilePath}'");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\tLinkPageContains: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"'{settings.LinkPageContains}'");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\tPdfFilePath: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"'{settings.PdfFilePath}'");

            Console.ResetColor();
            Console.WriteLine();
        }

        private static List<string> GetLinks(Settings settings)
        {
            var webDocument = new HtmlWeb().Load(settings.HomePage);
            var linkTags = webDocument.DocumentNode.Descendants("link");
            var linkedPages = webDocument.DocumentNode.Descendants("a")
                                              .Select(a => a.GetAttributeValue("href", null))
                                              .Where(u => !String.IsNullOrEmpty(u));

            var links = new List<string>();

            foreach (var linkPage in linkedPages)
            {
                if (linkPage.StartsWith(settings.LinkPageContains))
                {
                    var link = linkPage.Remove(linkPage.IndexOf("#"));

                    links.Add($"https://www.ableton.com{link}");
                }
            }

            links = links.Distinct().ToList();

            return links;
        }

        private static async Task<string> ReadHtmlContentAsync(List<string> links, string headerPage)
        {
            if (headerPage == null)
                headerPage = string.Empty;

            var downloadedFiles = 0;

            // Prepare the HttpClient object to get each page
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            httpClient.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

            var htmlContent = new StringBuilder();

            foreach (var link in links)
            {
                var htmlPage = await httpClient.GetStringAsync(link);

                downloadedFiles++;

                Console.Write("\tPage ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"'{downloadedFiles}' ");
                Console.ResetColor();
                Console.Write("of ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"'{links.Count}' ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("downloaded! ");
                Console.ResetColor();

                var document2 = new HtmlDocument();
                document2.LoadHtml(htmlPage);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\t\tPreparing content for page ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"'{downloadedFiles}' ");

                var nodes = document2.DocumentNode.SelectNodes("//img");
                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        node.Attributes.Remove("data-srcset");
                        node.Attributes.Remove("sizes");
                    }
                }

                var pageContent = document2.GetElementbyId("chapter_content").InnerHtml;
                pageContent = "<div id=\"chapter_content\" style=\"display:block;page-break-inside: avoid;page-break-after: avoid;page-break-before: always;\">" + pageContent + "</div>";

                if (htmlContent.Length > 0)
                    pageContent = "<br/><br/>" + pageContent;

                htmlContent.AppendLine(pageContent);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\t\tContent for page ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"'{downloadedFiles}' ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("ready!");
                Console.ResetColor();
            }

            // Generating HTML File
            Console.WriteLine();
            Console.ResetColor();
            Console.Write(" ");
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(" (3 of 4) Generating HTML file... ");
            Console.ResetColor();
            Console.WriteLine();

            // Web template used for each page
            var abletonLiveWebTemplate = "<!DOCTYPE html><html class=\"no-js\" lang=\"en\"><head><title>Ableton | Reference Manual Version 12</title><link rel=\"stylesheet\" href=\"https://cdn-resources.ableton.com/80bA26cPQ1hEJDFjpUKntxfqdmG3ZykO/static/CACHE/css/output.766b6321509d.css\" type=\"text/css\"></head><body class=\"\"><div id=\"main\" class=\"main js-main\"><div class=\"page\"><div class=\"bars abl-pt-1u\"><section class=\"body-text body-text--manual js-manual-content\">#HEADER_PAGE# #HTML_CONTENT#</div></div></div><script src=\"https://cdn-resources.ableton.com/80bA26cPQ1hEJDFjpUKntxfqdmG3ZykO/static/CACHE/js/output.0d080a3258f0.js\"></script><script src=\"https://cdn-resources.ableton.com/80bA26cPQ1hEJDFjpUKntxfqdmG3ZykO/static/scripts/dist/modal-bf7a8605561c291deaee.521bdf865c72.js\" ></script><script src=\"https://cdn-resources.ableton.com/80bA26cPQ1hEJDFjpUKntxfqdmG3ZykO/static/CACHE/js/output.b12367c8def4.js\"></script><script src=\"https://cdn-resources.ableton.com/80bA26cPQ1hEJDFjpUKntxfqdmG3ZykO/static/scripts/dist/agnosticAxe-bf7a8605561c291deaee.d41d8cd98f00.js\" ></script></body></html>";
            abletonLiveWebTemplate = abletonLiveWebTemplate.Replace("#HEADER_PAGE#", headerPage);
            abletonLiveWebTemplate = abletonLiveWebTemplate.Replace("#HTML_CONTENT#", htmlContent.ToString());
            abletonLiveWebTemplate = abletonLiveWebTemplate.Replace("alt=\"\" ", String.Empty);
            abletonLiveWebTemplate = abletonLiveWebTemplate.Replace("class=\" lazyload\" ", String.Empty);
            abletonLiveWebTemplate = abletonLiveWebTemplate.Replace("data-src=", "src=");

            // Return the HTML Content ready to be processed
            return abletonLiveWebTemplate;
        }

        private static void WriteHtmlFile(string htmlFilePath, string htmlContent)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\tWriting the HTML file...");
            Console.ResetColor();

            File.WriteAllText(htmlFilePath, htmlContent);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\tDONE! - {TerminalURL(htmlFilePath, htmlFilePath)}");
            Console.ResetColor();
            Console.WriteLine();
        }

        private static async Task WritePdfFileAsync(Settings settings)
        {

            // Generating PDF File
            Console.ResetColor();
            Console.Write(" ");
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(" (4 of 4) Generating the PDF file... ");
            Console.ResetColor();
            Console.WriteLine();

            // Download the Browser Executable
            await new BrowserFetcher().DownloadAsync();

            // Browser Execution Configs
            var launchOptions = new LaunchOptions
            {
                Headless = true, // = false for testing
            };

            // Open a new page in the controlled Browser
            using var browser = await Puppeteer.LaunchAsync(launchOptions);
            using (var page = await browser.NewPageAsync())
            {
                await page.GoToAsync($"file://{settings.HtmlFilePath}", 50000, new[] { WaitUntilNavigation.Networkidle2 });
                var html = await page.GetContentAsync();
                var pdfOptions = new PdfOptions()
                {
                    Format = PaperFormat.A4,
                    DisplayHeaderFooter = false,
                    MarginOptions = new MarginOptions() { Top = "50px", Bottom = "50px" },
                    HeaderTemplate = "",
                    FooterTemplate = "",
                };

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\tWriting the PDF file...");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\tThis action could take a while (wait for it)");
                Console.ResetColor();

                await page.PdfAsync(settings.PdfFilePath, pdfOptions);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\tDONE! - {TerminalURL(settings.PdfFilePath, settings.PdfFilePath)}");
                Console.ResetColor();
                Console.WriteLine();
            }
        }

        private static string TerminalURL(string caption, string url) => $"\u001B]8;;{url}\a{caption}\u001B]8;;\a";
    }
}