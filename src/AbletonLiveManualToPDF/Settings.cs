using System.Configuration;

namespace AbletonLiveManualToPDF
{
    internal class Settings
    {
        public ValidationResult ValidationResult { get; set; }

        // Optional
        private string _headerPage;
        public string HeaderPage { get => _headerPage; }

        // Mandatory
        private string _homePage;
        public string HomePage { get => _homePage; }

        // Mandatory
        private string _htmlFilePath;
        public string HtmlFilePath { get => _htmlFilePath; }

        // Mandatory
        private string _linkPageContains;
        public string LinkPageContains { get => _linkPageContains; }

        // Mandatory
        private string _pdfFilePath;
        public string PdfFilePath { get => _pdfFilePath; }

        public Settings()
        {
            ValidationResult = new ValidationResult();
            Get();
        }

        private void Get()
        {
            try
            {
                // Mandatory
                _homePage = ConfigurationManager.AppSettings["HomePage"];
                _htmlFilePath = ConfigurationManager.AppSettings["HtmlFilePath"];
                _linkPageContains = ConfigurationManager.AppSettings["LinkPageContains"];
                _pdfFilePath = ConfigurationManager.AppSettings["PdfFilePath"];

                // Optionals
                _headerPage = ConfigurationManager.AppSettings["HeaderPage"];

                Validate();
            }
            catch (Exception ex)
            {
                ValidationResult = new ValidationResult(ex.Message);
            }
        }

        private void Validate()
        {
            if (String.IsNullOrEmpty(HomePage))
                ValidationResult = new ValidationResult("HomePage is mandatory but found null or empty");
            else if (String.IsNullOrEmpty(HtmlFilePath))
                ValidationResult = new ValidationResult("HtmlFilePath is mandatory but found null or empty");
            else if (String.IsNullOrEmpty(LinkPageContains))
                ValidationResult = new ValidationResult("LinkPageContains is mandatory but found null or empty");
            else if (String.IsNullOrEmpty(PdfFilePath))
                ValidationResult = new ValidationResult("PdfFilePath is mandatory but found null or empty");
        }
    }
}
