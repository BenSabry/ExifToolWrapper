using System.Collections.Generic;

namespace Wrappers.DTO
{
    public struct ExifResult
    {
        public readonly string Output;
        public readonly Dictionary<string, string> Tags;
        public readonly int Updates;
        public readonly int Errors;
        public ExifResult(string Output, Dictionary<string, string> Tags, int Updates, int Errors)
        {
            this.Output = Output;
            this.Tags = Tags;
            this.Updates = Updates;
            this.Errors = Errors;
        }
    }
}