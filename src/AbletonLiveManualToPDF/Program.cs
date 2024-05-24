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

            // Show the date time (informative information)
            Console.ResetColor();
            Console.Write(" ");
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write($" {DateTime.Now} ");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine();

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
                    var htmlContent = await ReadHtmlRawContentAsync(links);

                    // Convert HTML to Markdown
                    var htmlToMarkdown = htmlContent;
                    var markdownContent = ConvertoContentToMarkdown(htmlToMarkdown);

                    // Write the Markdown Content to disk                    
                    WriteMarkdownFile(settings.MarkdownFilePath, markdownContent, "Writing the Markdown file...");

                    // Convert the Raw HTML to HTML using the template
                    htmlContent = ConvertToHtml(htmlContent, settings.HeaderPage);

                    // Write the HTML Content parsed to disk
                    WriteHtmlFile(settings.HtmlFilePath, htmlContent, "Writing the HTML file...");

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

            // Show the date time (informative information)
            Console.ResetColor();
            Console.Write(" ");
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write($" {DateTime.Now} ");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine();

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
            Console.Write("\tMarkdownFilePath: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"'{settings.MarkdownFilePath}'");

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

        private static async Task<string> ReadHtmlRawContentAsync(List<string> links)
        {
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

            // Generating the Markdown and HTML Files
            Console.WriteLine();
            Console.ResetColor();
            Console.Write(" ");
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(" (3 of 4) Generating the Markdown and HTML files... ");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine();

            // Return the Raw HTML Content ready to be processed
            return htmlContent.ToString();
        }

        private static string ConvertToHtml(string htmlContent, string headerPage)
        {
            if (headerPage == null)
                headerPage = String.Empty;

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

        private static string ConvertoContentToMarkdown(string htmlContent)
        {
            if (htmlContent == null)
                return String.Empty;

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(htmlContent);

            // Avoid null reference exception
            htmlDocument.OptionEmptyCollection = true;

            foreach (var htmlNode in htmlDocument.DocumentNode.SelectNodes("//a[@href]"))
            {
                var linkCaptionValue = htmlNode.InnerHtml;
                var hrefValue = htmlNode.Attributes["href"].Value;

                var newNodeText = $"[{linkCaptionValue}]({hrefValue})";
                var newHtmlNode = HtmlNode.CreateNode(newNodeText);
                htmlNode.ParentNode.ReplaceChild(newHtmlNode, htmlNode);
            }

            foreach (var htmlNode in htmlDocument.DocumentNode.SelectNodes("//figure[contains(@class, 'image-container')]"))
            {
                var figCaptionNode = htmlNode.SelectSingleNode("//figcaption");
                var figCaptionValue = figCaptionNode.InnerHtml;

                var imageSourceTag = htmlNode.SelectSingleNode("//img[@data-src]");
                var imageSourceValue = imageSourceTag.GetAttributeValue("data-src", "");

                var newNodeText = $"{Environment.NewLine}{Environment.NewLine}![{imageSourceValue}]({imageSourceValue}){Environment.NewLine}{Environment.NewLine}**{figCaptionValue}**{Environment.NewLine}{Environment.NewLine}";
                var newHtmlNode = HtmlNode.CreateNode(newNodeText);
                htmlNode.ParentNode.ReplaceChild(newHtmlNode, htmlNode);
            }

            var querySelectNodes = htmlDocument.DocumentNode.SelectNodes("//span[contains(@class, 'header-section-number')]");
            foreach (var htmlNode in querySelectNodes.ToList())
            {
                var newNodeText = $"{htmlNode.InnerHtml}";
                var newHtmlNode = HtmlNode.CreateNode(newNodeText);
                htmlNode.ParentNode.ReplaceChild(newHtmlNode, htmlNode);
            }

            var query = htmlDocument.DocumentNode.Descendants("h1");
            foreach (var htmlNode in query.ToList())
            {
                var newNodeText = $"<p><br># {htmlNode.InnerHtml}<br></p>";
                var newHtmlNode = HtmlNode.CreateNode(newNodeText);
                htmlNode.ParentNode.ReplaceChild(newHtmlNode, htmlNode);
            }

            query = htmlDocument.DocumentNode.Descendants("h2");
            foreach (var htmlNode in query.ToList())
            {
                var newNodeText = $"<p><br>## {htmlNode.InnerHtml}<br></p>";
                var newHtmlNode = HtmlNode.CreateNode(newNodeText);
                htmlNode.ParentNode.ReplaceChild(newHtmlNode, htmlNode);
            }

            query = htmlDocument.DocumentNode.Descendants("h3");
            foreach (var htmlNode in query.ToList())
            {
                var newNodeText = $"<p><br>### {htmlNode.InnerHtml}<br></p>";
                var newHtmlNode = HtmlNode.CreateNode(newNodeText);
                htmlNode.ParentNode.ReplaceChild(newHtmlNode, htmlNode);
            }

            query = htmlDocument.DocumentNode.Descendants("figcaption");
            foreach (var htmlNode in query.ToList())
            {
                var newNodeText = $"> {htmlNode.InnerHtml}";
                var newHtmlNode = HtmlNode.CreateNode(newNodeText);
                htmlNode.ParentNode.ReplaceChild(newHtmlNode, htmlNode);
            }

            query = htmlDocument.DocumentNode.Descendants("kbd");
            foreach (var item in query.ToList())
            {
                var newNodeText = $"`{item.InnerHtml}` ";
                var newHtmlNode = HtmlNode.CreateNode(newNodeText);
                item.ParentNode.ReplaceChild(newHtmlNode, item);
            }

            query = htmlDocument.DocumentNode.Descendants("span");
            foreach (var htmlNode in query.ToList())
            {
                var newNodeText = HtmlNode.CreateNode(String.Empty);
                htmlNode.ParentNode.ReplaceChild(newNodeText, htmlNode);
            }

            htmlContent = htmlDocument.DocumentNode.InnerHtml;
            htmlContent = htmlContent.Replace("<li>", "* ");
            htmlContent = htmlContent.Replace("</li>", Environment.NewLine);
            htmlContent = htmlContent.Replace("<strong>", "**");
            htmlContent = htmlContent.Replace("</strong>", "**");
            htmlContent = htmlContent.Replace("<em>", "*");
            htmlContent = htmlContent.Replace("</em>", "*");
            htmlContent = htmlContent.Replace("<p>", Environment.NewLine + Environment.NewLine);
            htmlContent = htmlContent.Replace("</p>", Environment.NewLine);
            htmlContent = htmlContent.Replace("<br>", Environment.NewLine + Environment.NewLine);

            htmlContent = RemoveUnwantedTags(htmlContent);

            return htmlContent;
        }

        private static string RemoveUnwantedTags(string data)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;

            var document = new HtmlDocument();
            document.LoadHtml(data);

            var acceptableTags = new String[] { "strong", "em", "u" };

            var nodes = new Queue<HtmlNode>(document.DocumentNode.SelectNodes("./*|./text()"));
            while (nodes.Count > 0)
            {
                var node = nodes.Dequeue();
                var parentNode = node.ParentNode;

                if (!acceptableTags.Contains(node.Name) && node.Name != "#text")
                {
                    var childNodes = node.SelectNodes("./*|./text()");

                    if (childNodes != null)
                    {
                        foreach (var child in childNodes)
                        {
                            nodes.Enqueue(child);
                            parentNode.InsertBefore(child, node);
                        }
                    }

                    parentNode.RemoveChild(node);

                }
            }

            return document.DocumentNode.InnerHtml;
        }

        private static void WriteHtmlFile(string filePath, string htmlContent, string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\t{message}");
            Console.ResetColor();

            File.WriteAllText(filePath, htmlContent);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\tDONE! - {TerminalURL(filePath, filePath)}");
            Console.ResetColor();
            Console.WriteLine();
        }

        private static void WriteMarkdownFile(string filePath, string markdownContent, string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\t{message}");
            Console.ResetColor();

            File.WriteAllText(filePath, markdownContent);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\tDONE! - {TerminalURL(filePath, filePath)}");
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

            // Downloading Chromium (information)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\tDownloading Chromium...");
            Console.ResetColor();

            // Download the Browser Executable
            await new BrowserFetcher().DownloadAsync();

            // Browser Execution Configs
            var launchOptions = new LaunchOptions
            {
                Headless = true, // = false for testing
            };

            // Launching Chromium (information)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\tLaunching Chromium internally...");
            Console.ResetColor();

            // Open a new page in the controlled Browser
            using var browser = await Puppeteer.LaunchAsync(launchOptions);

            // Executing Chromium (information)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\tExecuting Chromium to get the content...");
            Console.ResetColor();

            using (var page = await browser.NewPageAsync())
            {
                await page.GoToAsync($"file://{settings.HtmlFilePath}", 50000, new[] { WaitUntilNavigation.Networkidle2 });
                var html = await page.GetContentAsync();
                var pdfOptions = new PdfOptions()
                {
                    Format = PaperFormat.A4,
                    DisplayHeaderFooter = false,
                    MarginOptions = new MarginOptions() { Top = "50px", Bottom = "50px" },
                    HeaderTemplate = String.Empty,
                    FooterTemplate = String.Empty,
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