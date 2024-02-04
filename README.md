# ExifToolWrapper
A C# Wrapper for ExifTool 

# Usage
```C#
//Read current ExifTool version
var version = ExifToolWrapper.GetVersion();

//Format date to Exif format
var date = ExifToolWrapper.FormatDateTime(DateTime.Now);

//For one time execution using static method
var path = @"D:\Media\20181112-035722.jpg";
var properties = ExifToolWrapper.InstaExecute(path);

//For many executions and best performance 
//using always-open instance of ExifTool
using (ExifToolWrapper exif = new ExifToolWrapper())
{
    var dates = exif.Execute(path, "-AllDates");    //Read
    exif.Execute(path, $"-FileModifyDate={date}");  //Write
}
```

## About ExifTool
[ExifTool](https://exiftool.org/) is a customizable set of Perl modules plus a full-featured
command-line application for reading and writing meta information in a wide
variety of files, including the maker note information of many digital
cameras by various manufacturers such as Canon, Casio, DJI, FLIR, FujiFilm,
GE, HP, JVC/Victor, Kodak, Leaf, Minolta/Konica-Minolta, Nikon, Nintendo,
Olympus/Epson, Panasonic/Leica, Pentax/Asahi, Phase One, Reconyx, Ricoh,
Samsung, Sanyo, Sigma/Foveon and Sony.
