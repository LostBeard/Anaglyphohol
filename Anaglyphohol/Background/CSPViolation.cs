namespace Anaglyphohol.Background
{
    /// <summary>
    /// Message used when reporting a failed Blazor start due to a CSP violation
    /// </summary>
    public class CSPViolation
    {
        /// <summary>
        /// The original policy as reported during the violation
        /// </summary>
        public string OriginalPolicy { get; set; }
        /// <summary>
        /// The document Uri
        /// </summary>
        public string DocumentURI { get; set; }
    }
}
