namespace AbletonLiveManualToPDF
{
    internal class ValidationResult
    {
        public bool IsValidated { get => String.IsNullOrEmpty(Message); }
        public string Message { get; set; }

        public ValidationResult(string message = "")
        {
            Message = message;
        }
    }
}
