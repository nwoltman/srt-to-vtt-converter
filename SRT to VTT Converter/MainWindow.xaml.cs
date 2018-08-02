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

        private int _offsetMilliseconds = 0;
        private bool _negativeOffset = false;

        public MainWindow()
        {
            InitializeComponent();

            // Configure open file dialog box
            _dlgOpenFile.Filter = "SubRip Subtitles (*.srt)|*.srt"; // Filter files by .srt extension 
            _dlgOpenFile.FileOk += DlgOpenFile_FileOk;
            _dlgOpenFile.Multiselect = true; // Allow multiple files to be selected

            // Configure the background worker
            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.WorkerSupportsCancellation = true;
            _backgroundWorker.DoWork += BackgroundWorker_DoWork;
            _backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            _backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // Toggles the timing offset direction
        private void BtnOffsetPlusMinus_Click(object sender, RoutedEventArgs e)
        {
            _negativeOffset = !_negativeOffset;
            BtnOffsetPlusMinus.Content = _negativeOffset ? "-" : "+";
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

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            _dlgOpenFile.ShowDialog();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private int GetOffsetTime()
        {
            var h = UInt32.Parse(TbHours.Text);
            var m = UInt32.Parse(TbMinutes.Text);
            var s = UInt32.Parse(TbSeconds.Text);
            var ms = UInt32.Parse(TbMilliseconds.Text);

            var offset = h * 3600000 + m * 60000 + s * 1000 + ms;

            return Convert.ToInt32(_negativeOffset ? -offset : offset);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // Is called when the open file dialog is closed with a legal file being selected
        private void DlgOpenFile_FileOk(object sender, CancelEventArgs e)
        {
            // Set up GUI for conversion
            BtnOpenFile.Visibility = Visibility.Collapsed;
            BtnCancel.Visibility = Visibility.Visible;
            WpOffsetInput.IsEnabled = false;
            LblProgress.Content = "Progress: 0%";
            TxtOutput.Clear();
            TxtOutput.Visibility = Visibility.Visible;

            // Record the offset before starting the conversion
            _offsetMilliseconds = GetOffsetTime();

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

        // Async work handler, used to convert each of the user's selected files
        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            int numFilesProcessed = 0;
            
            foreach (var filePath in _dlgOpenFile.FileNames)
            {
                if (_backgroundWorker.CancellationPending) // The user requested to cancel the conversion
                {
                    e.Cancel = true;
                    break;
                }
                
                string doneMsg = "Done";
                try
                {
                    SubtitleConverter.ConvertSRTToVTT(filePath, _offsetMilliseconds);
                }
                catch (Exception ex)
                {
                    doneMsg = "ERROR:\n" + ex.Message;
                }

                ++numFilesProcessed;

                _backgroundWorker.ReportProgress(
                    (int)(numFilesProcessed / (double)_dlgOpenFile.FileNames.Length * 100),
                    numFilesProcessed + ". \"" + Path.GetFileName(filePath) + "\" - " + doneMsg + "\n"
                );
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // This event handler executes on the main thread to update displayed progress in the GUI
        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            LblProgress.Content = "Progress: " + e.ProgressPercentage + "%";
            TxtOutput.AppendText((string)e.UserState); // Append the progress message
            TxtOutput.ScrollToEnd(); // Scroll to the bottom to keep the most recently converted files visible
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // This event handler deals with the results of the background operation
        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TxtOutput.AppendText("\n");

            if (e.Cancelled)
            {
                TxtOutput.AppendText("Process Cancelled 🚫");
                BtnCancel.Content = "Cancel";
                BtnCancel.IsEnabled = true;
            }
            else if (e.Error != null)
            {
                TxtOutput.AppendText(
                    "ERROR!\nThe following error occured during the conversion:\n\n" + e.Error.Message
                );
            }
            else
            {
                TxtOutput.AppendText("Finished");
            }

            BtnOpenFile.Visibility = Visibility.Visible;
            BtnCancel.Visibility = Visibility.Collapsed;
            WpOffsetInput.IsEnabled = true;
        }

    }
}
