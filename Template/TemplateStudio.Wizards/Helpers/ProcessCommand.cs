using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TemplateStudio.Wizards.Helpers;

public static class ProcessCommand
{
	public static void getAsyncUnoCheck()
	{
		Thread objThread = new Thread(new ParameterizedThreadStart(getUnoCheck));
		objThread.IsBackground = true;
		objThread.Priority = ThreadPriority.AboveNormal;
		objThread.Start();
	}

	public static void getUnoCheck(object command = null)
	{

		try
		{
			string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Uno";
			DirectoryInfo di = Directory.CreateDirectory(path);

			using (Process p = new Process())
			{
				p.StartInfo = new ProcessStartInfo("cmd.exe")
				{
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					RedirectStandardInput = true,
					RedirectStandardOutput = false,
					RedirectStandardError = false,
					UseShellExecute = false,
					WorkingDirectory = path
				};


				//To get line by line on p_OutputDataReceived
				//p.OutputDataReceived += p_OutputDataReceived;
				//p.ErrorDataReceived += p_ErrorDataReceived;

				p.Start();
				p.StandardInput.Write("dotnet tool install -g uno.check" + p.StandardInput.NewLine);
				p.StandardInput.Write("uno-check > resultUnoCheck.txt" + p.StandardInput.NewLine + p.StandardInput.NewLine);

				//To get line by line on p_OutputDataReceived, not the file resultUnoCheck
				//p.StandardInput.Write("uno-check" + p.StandardInput.NewLine + p.StandardInput.NewLine);

				p.BeginOutputReadLine();
				p.BeginErrorReadLine();

				p.WaitForExit();
				p.Close();
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
		}
	}

	public static string getFileContent()
	{
		try
		{
			string ContentFile = "";
			string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Uno\\resultUnoCheck.txt";

			using (var stream = File.Open(path, FileMode.Open))
			{
				StreamReader reader = new StreamReader(stream);
				ContentFile = reader.ReadToEnd();
			}

			//return File.Exists(path)?File.File.ReadAllText(path):"";
			return ContentFile;
		}
		catch (Exception ex)
		{
			return "Error" + ex.Message+ ex.InnerException;
		}
	}

	//public static void p_ErrorDataReceived(object sender, DataReceivedEventArgs e)
	//{
	//	Process p = sender as Process;
	//	if (p == null)
	//		return;
	//	Console.WriteLine(e.Data);
	//}

	//public static void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
	//{
	//	Process p = sender as Process;
	//	if (p == null)
	//		return;
	//	Console.WriteLine(e.Data);
	//}

}
