using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SRT_to_VTT_Converter
{
    class SubtitleConverter
    {
        private static readonly Regex _rgxDialogNumber = new Regex(@"^\d+$");
        private static readonly Regex _rgxTimeFrame = new Regex(@"(\d\d:\d\d:\d\d,\d\d\d) --> (\d\d:\d\d:\d\d,\d\d\d)");

        public static void ConvertSRTToVTT(string filePath, int offsetMilliseconds)
        {
            using (var srtReader = new StreamReader(filePath))
            using (var vttWriter = new StreamWriter(filePath.Replace(".srt", ".vtt")))
            {
                // Write starting line for the WebVTT file
                vttWriter.WriteLine("WEBVTT");
                vttWriter.WriteLine("");

                // Handle each line of the SRT file
                string srtLine;
                while ((srtLine = srtReader.ReadLine()) != null)
                {
                    if (_rgxDialogNumber.IsMatch(srtLine)) // Ignore dialog ID number lines
                    {
                        continue;
                    }

                    // If the line is a time frame line, reformat and output the time frame
                    Match match = _rgxTimeFrame.Match(srtLine);
                    if (match.Success)
                    {
                        if (offsetMilliseconds != 0)
                        {
                            // Extract the times from the matched time frame line
                            var startTime = TimeSpan.Parse(match.Groups[1].Value.Replace(',', '.'));
                            var endTime = TimeSpan.Parse(match.Groups[2].Value.Replace(',', '.'));

                            // Modify the time with the offset
                            long startTimeMs = (uint)startTime.TotalMilliseconds + offsetMilliseconds;
                            long endTimeMs = (uint)endTime.TotalMilliseconds + offsetMilliseconds;

                            startTime = TimeSpan.FromMilliseconds(startTimeMs < 0 ? 0 : startTimeMs);
                            endTime = TimeSpan.FromMilliseconds(endTimeMs < 0 ? 0 : endTimeMs);

                            // Construct the new time frame line
                            srtLine =
                                startTime.ToString(@"hh\:mm\:ss\.fff") +
                                " --> " +
                                endTime.ToString(@"hh\:mm\:ss\.fff");
                        }
                        else
                        {
                            srtLine = srtLine.Replace(',', '.'); // Simply replace the comma in the time with a period
                        }
                    }

                    vttWriter.WriteLine(srtLine); // Write out the line
                }
            }
        }
    }
}
