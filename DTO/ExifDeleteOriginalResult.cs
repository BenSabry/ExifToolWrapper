namespace Wrappers.DTO
{
    public struct ExifDeleteOriginalResult
    {
        public readonly long DirectoriesScanned;
        public readonly long ImageFilesFound;
        public readonly long OriginalFilesDeleted;
        public ExifDeleteOriginalResult(long DirectoriesScanned, long ImageFilesFound, long OriginalFilesDeleted)
        {
            this.DirectoriesScanned = DirectoriesScanned;
            this.ImageFilesFound = ImageFilesFound;
            this.OriginalFilesDeleted = OriginalFilesDeleted;
        }
    }
}
