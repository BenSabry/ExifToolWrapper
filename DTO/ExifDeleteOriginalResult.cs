namespace Wrappers.DTO;

public record struct ExifDeleteOriginalResult(long DirectoriesScanned, long ImageFilesFound, long OriginalFilesDeleted);