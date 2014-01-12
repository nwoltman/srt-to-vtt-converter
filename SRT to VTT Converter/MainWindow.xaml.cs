using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace SRT_to_VTT_Converter
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private readonly Microsoft.Win32.OpenFileDialog _dlgOpenFile = new Microsoft.Win32.OpenFileDialog();

		public MainWindow()
		{
			InitializeComponent();

			//Configure open file dialog box
			_dlgOpenFile.Filter = "SubRip Subtitles (*.srt)|*.srt"; //Filter files by .srt extension 
			_dlgOpenFile.FileOk += dlgOpenFile_FileOk;
			_dlgOpenFile.Multiselect = true; //Allow multiple files to be selected
		}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		private void OpenFile(object sender, RoutedEventArgs e)
		{
			//Show the open file dialog
			_dlgOpenFile.ShowDialog();
		}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		//Is called when the open file dialog is closed with a legal file being selected
		private void dlgOpenFile_FileOk(object sender, CancelEventArgs e)
		{
			//Disable the open file button
			btnOpenFile.IsEnabled = false;

			//Clear the output text
			txtOutput.Text = "";

			//Convert each file and show a done message for each one when finished
			int nCount = 1;
			foreach (var sFile in _dlgOpenFile.FileNames)
			{
				Convert(sFile);
				txtOutput.Text += nCount++ + ". \"" +sFile + "\" - Done\n";
			}

			//Re-enable the open file button
			btnOpenFile.IsEnabled = true;
		}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		private void Convert(string sFilePath)
		{
			using( var strReader = new StreamReader(sFilePath) )
            using ( var strWriter = new StreamWriter(sFilePath.Replace(".srt", ".vtt")) )
            {

                var rgxDialogNumber = new Regex(@"^\d+$");
                var rgxTimeFrame = new Regex(@"\d\d:\d\d:\d\d,\d\d\d --> \d\d:\d\d:\d\d,\d\d\d");

                //Write mandatory starting line for the WebVTT file
                strWriter.WriteLine("WEBVTT");
                strWriter.WriteLine("");

                //Handle each line of the SRT file
                string sLine;
                while ((sLine = strReader.ReadLine()) != null)
                {
                    //We only care about lines that aren't just an integer (aka ignore dialog id number lines)
                    if (!rgxDialogNumber.IsMatch(sLine))
                    {
                        //If the line is a time frame line, replace the comma with a period
                        if (rgxTimeFrame.IsMatch(sLine))
                        {
                            sLine = sLine.Replace(',', '.');
                        }
                        strWriter.WriteLine(sLine); //Write out the line
                    }
                }
            }
		}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	}
}
