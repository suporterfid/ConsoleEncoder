namespace ConsoleEncoder
{
    internal class TagEvent
    {
        public string Epc { get; set; } = string.Empty;

        public string Tid { get; set; } = string.Empty;

        public string User { get; set; } = string.Empty;

        public double? PeakRssi { get; set; }
    }
}
