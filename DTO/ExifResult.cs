namespace Wrappers.DTO;

internal record struct ExifResult(string Output, Dictionary<string, string> Tags, int Updates, int Errors);
