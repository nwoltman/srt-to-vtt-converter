using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SRT_to_VTT_Converter
{
    class SubtitleConverter
    {
        private static readonly Regex _rgxCueID = new Regex(@"^\d+$");
        private static readonly Regex _rgxTimeFrame = new Regex(@"(\d\d:\d\d:\d\d(?:[,.]\d\d\d)?) --> (\d\d:\d\d:\d\d(?:[,.]\d\d\d)?)");

        public static void ConvertSRTToVTT(string filePath, int offsetMilliseconds)
        {
            using (var srtReader = new StreamReader(filePath))
            using (var vttWriter = new StreamWriter(filePath.Replace(".srt", ".vtt")))
            {
                vttWriter.WriteLine("WEBVTT"); // Starting line for the WebVTT files
                vttWriter.WriteLine("");
                
                string srtLine;
                while ((srtLine = srtReader.ReadLine()) != null)
                {
                    if (_rgxCueID.IsMatch(srtLine)) // Ignore cue ID number lines
                    {
                        continue;
                    }

                    Match match = _rgxTimeFrame.Match(srtLine);
                    if (match.Success) // Format the time frame to VTT format (and handle offset)
                    {
                        var startTime = TimeSpan.Parse(match.Groups[1].Value.Replace(',', '.'));
                        var endTime = TimeSpan.Parse(match.Groups[2].Value.Replace(',', '.'));

                        if (offsetMilliseconds != 0)
                        {
                            double startTimeMs = startTime.TotalMilliseconds + offsetMilliseconds;
                            double endTimeMs = endTime.TotalMilliseconds + offsetMilliseconds;

                            startTime = TimeSpan.FromMilliseconds(startTimeMs < 0 ? 0 : startTimeMs);
                            endTime = TimeSpan.FromMilliseconds(endTimeMs < 0 ? 0 : endTimeMs);
                        }
                        
                        srtLine =
                            startTime.ToString(@"hh\:mm\:ss\.fff") +
                            " --> " +
                            endTime.ToString(@"hh\:mm\:ss\.fff");
                    }

                    vttWriter.WriteLine(srtLine);
                }
            }
        }
    }
}
