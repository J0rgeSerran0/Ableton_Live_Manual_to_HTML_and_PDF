using System.Configuration;

namespace AbletonLiveManualToPDF
{
    internal class Settings
    {
        public ValidationResult ValidationResult { get; set; }

        // Optional Settings
        private string _headerPage;
        public string HeaderPage { get => _headerPage; }

        private string _homePage;
        public string HomePage { get => _homePage; }

        private string _htmlFilePath;
        public string HtmlFilePath { get => _htmlFilePath; }

        private string _linkPageContains;
        public string LinkPageContains { get => _linkPageContains; }

        private string _pdfFilePath;
        public string PdfFilePath { get => _pdfFilePath; }

        public Settings()
        {
            ValidationResult = new ValidationResult();
            Get();
            
            if (ValidationResult.IsValidated)
                Validate();
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
