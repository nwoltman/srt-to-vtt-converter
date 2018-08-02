/*
The MIT License (MIT)

Copyright (c) 2014-2015, 2018 Nathan Woltman

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace SRT_to_VTT_Converter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly Microsoft.Win32.OpenFileDialog _dlgOpenFile = new Microsoft.Win32.OpenFileDialog();
        private readonly BackgroundWorker _backgroundWorker = new BackgroundWorker();

        /// <summary>
        /// Always either 1 or -1.
        /// 1 means add the offset time, -1 means subtract the offset time
        /// </summary>
        private int _nOffsetDirection = 1;
        private uint _offsetMs;

        public MainWindow()
        {
            InitializeComponent();

            // Configure open file dialog box
            _dlgOpenFile.Filter = "SubRip Subtitles (*.srt)|*.srt"; // Filter files by .srt extension 
            _dlgOpenFile.FileOk += dlgOpenFile_FileOk;
            _dlgOpenFile.Multiselect = true; // Allow multiple files to be selected

            // Configure the background worker
            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.WorkerSupportsCancellation = true;
            _backgroundWorker.DoWork += BackgroundWorker_DoWork;
            _backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            _backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // Event handler for when the text of a textbox that only accepts numbers changes
        private void NumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            textBox.Text = Regex.Replace(textBox.Text, @"\D", ""); // Remove all non-digit characters
        }

        // Event handler for when a textbox that only accepts numbers loses focus
        private void NumberTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var paddingWidth = textBox.Name == "TbMilliseconds" ? 3 : 2;
            if (textBox.Text.Length < paddingWidth)
            {
                textBox.Text = textBox.Text.PadLeft(paddingWidth, '0');
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private uint getOffsetTime()
        {
            var h = UInt32.Parse(TbHours.Text);
            var m = UInt32.Parse(TbMinutes.Text);
            var s = UInt32.Parse(TbSeconds.Text);
            var ms = UInt32.Parse(TbMilliseconds.Text);

            return h * 3600000 + m * 60000 + s * 1000 + ms;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            // Simply show the open file dialog
            _dlgOpenFile.ShowDialog();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // Is called when the open file dialog is closed with a legal file being selected
        private void dlgOpenFile_FileOk(object sender, CancelEventArgs e)
        {
            // Set up GUI for conversion
            BtnOpenFile.Visibility = Visibility.Collapsed;
            BtnCancel.Visibility = Visibility.Visible;
            WpOffsetInput.IsEnabled = false;
            LblProgress.Content = "Progress: 0%";
            TxtOutput.Clear();
            TxtOutput.Visibility = Visibility.Visible;

            // Record the offset before starting the conversion
            _offsetMs = getOffsetTime();

            // Run the BackgroundWorker asynchronously to convert the selected files
            _backgroundWorker.RunWorkerAsync();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _backgroundWorker.CancelAsync();
            BtnCancel.Content = "Cancelling...";
            BtnCancel.IsEnabled = false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void BtnOffsetPlusMinus_Click(object sender, RoutedEventArgs e)
        {
            _nOffsetDirection *= -1; // Toggle the offset direction
            BtnOffsetPlusMinus.Content = _nOffsetDirection == 1 ? "+" : "-"; // Set the symbol based on the direction
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // This event handler is called by running the background worker asynchronously
        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            int nConverted = 0; // This will count the number of conversions completed

            // For each of the user's selected files
            foreach (var sFile in _dlgOpenFile.FileNames)
            {
                // If the user has requested to cancel the conversion, set the Cancel flag and break out of the loop
                if (_backgroundWorker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }

                //Convert the file and report progress when finished
                string sDoneMsg = "Done";
                try
                {
                    Convert(sFile);
                }
                catch (Exception ex)
                {
                    sDoneMsg = "ERROR:\n" + ex.Message;
                }
                ++nConverted;
                _backgroundWorker.ReportProgress(
                    (int)(nConverted / (double)_dlgOpenFile.FileNames.Length * 100), // % complete
                    nConverted + ". \"" + Path.GetFileName(sFile) + "\" - " + sDoneMsg + "\n" // Done message for the file
                );
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // This event handler executes on the main thread to update displayed progress in the GUI
        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            LblProgress.Content = "Progress: " + e.ProgressPercentage + "%";
            TxtOutput.AppendText((string)e.UserState); // Append the message to the output textbox
            TxtOutput.ScrollToEnd(); // Scroll to the bottom to keep the most recently converted files visible
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //This event handler deals with the results of the background operation
        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                TxtOutput.AppendText("PROCESS CANCELLED");
                BtnCancel.Content = "Cancel";
                BtnCancel.IsEnabled = true;
            }
            else if (e.Error != null)
            {
                TxtOutput.AppendText(
                    "\nERROR!\nThe following error occured during the conversion:\n\n" + e.Error.Message
                );
            }

            BtnOpenFile.Visibility = Visibility.Visible;
            BtnCancel.Visibility = Visibility.Collapsed;
            WpOffsetInput.IsEnabled = true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Convert(string sFilePath)
        {
            using (var strReader = new StreamReader(sFilePath))
            using (var strWriter = new StreamWriter(sFilePath.Replace(".srt", ".vtt")))
            {
                var rgxDialogNumber = new Regex(@"^\d+$");
                var rgxTimeFrame = new Regex(@"(\d\d:\d\d:\d\d,\d\d\d) --> (\d\d:\d\d:\d\d,\d\d\d)");

                // Write starting line for the WebVTT file
                strWriter.WriteLine("WEBVTT");
                strWriter.WriteLine("");

                // Handle each line of the SRT file
                string sLine;
                while ((sLine = strReader.ReadLine()) != null)
                {
                    // We only care about lines that aren't just an integer (aka ignore dialog id number lines)
                    if (rgxDialogNumber.IsMatch(sLine))
                        continue;

                    // If the line is a time frame line, reformat and output the time frame
                    Match match = rgxTimeFrame.Match(sLine);
                    if (match.Success)
                    {
                        if (_offsetMs > 0)
                        {
                            // Extract the times from the matched time frame line
                            var tsStartTime = TimeSpan.Parse(match.Groups[1].Value.Replace(',', '.'));
                            var tsEndTime = TimeSpan.Parse(match.Groups[2].Value.Replace(',', '.'));

                            // Modify the time with the offset
                            long startTimeMs = _nOffsetDirection * _offsetMs + (uint)tsStartTime.TotalMilliseconds;
                            long endTimeMs = _nOffsetDirection * _offsetMs + (uint)tsEndTime.TotalMilliseconds;
                            tsStartTime = TimeSpan.FromMilliseconds(startTimeMs < 0 ? 0 : startTimeMs);
                            tsEndTime = TimeSpan.FromMilliseconds(endTimeMs < 0 ? 0 : endTimeMs);

                            // Construct the new time frame line
                            sLine = tsStartTime.ToString(@"hh\:mm\:ss\.fff") +
                                    " --> " +
                                    tsEndTime.ToString(@"hh\:mm\:ss\.fff");
                        }
                        else
                        {
                            sLine = sLine.Replace(',', '.'); // Simply replace the comma in the time with a period
                        }
                    }

                    strWriter.WriteLine(sLine); // Write out the line
                }
            }
        }

    }
}
