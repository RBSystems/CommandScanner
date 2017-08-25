﻿using CommandScanner.HelperClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CommandScanner.SystemCommands;

namespace CommandScanner
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		#region Properties & Fields

		private ConnectionService Connection { get; set; }
		private ConnectionType _connectionType;

		#endregion

		#region Constructor

		public MainWindow()
		{
			InitializeComponent();
		}

		#endregion

		#region Event Handlers

		#region Connection Type Combo Box

		/// <summary>
		/// Sets the connection type based on what the user chooses
		/// </summary>
		private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var comboBox = (ComboBox)sender;
			string selection = (string)comboBox.SelectedItem;

			switch (selection)
			{
				case "SSH":
					_connectionType = ConnectionType.Ssh;
					break;
				case "CTP":
					_connectionType = ConnectionType.Ctp;
					break;
				case "Auto Detect":
					_connectionType = ConnectionType.Auto;
					break;
			}
		}

		/// <summary>
		/// Loads the available selections to the connectionTypeBox
		/// </summary>
		private void comboBox_Loaded(object sender, RoutedEventArgs e)
		{
			List<string> data = new List<string>();
			data.Add("Auto Detect");
			data.Add("SSH");
			data.Add("CTP");

			var comboBox = (ComboBox)sender;

			comboBox.ItemsSource = data;
			comboBox.SelectedIndex = 0;
		}

		#endregion

		#region Connect Button

		/// <summary>
		/// Establish a connection with the device
		/// </summary>
		private void connect_Click(object sender, RoutedEventArgs e)
		{
			// TODO add a 'connecting...' prompt

			string address = hostName.Text;
			Connection = new ConnectionService(address, _connectionType);

			bool connected = Connection.Connect();

			if (connected)
				scanDevice.IsEnabled = true;


			// TODO change prompt to say 'connected' if successful
		}

		#endregion

		#region Scan Button

		/// <summary>
		/// Scan the device for the commands
		/// </summary>
		private void button_Click(object sender, RoutedEventArgs e)
		{
			// get all commands
			var allCommands = GetAllCommands();

			// get all hidden commands
			//var hiddenCommands = GetHiddenCommands();

			// write all of the commands to an html file
			WriteCommandsToFile(allCommands);

			//var loadWindow = new LoadScreen();
			//loadWindow.ShowDialog();
		}

		#endregion

		#endregion

		#region Helper Methods

		private List<Command> GetAllCommands()
		{
			var allCommands = new List<Command>();

			string result = Connection.SendCommand("help all");
			var commands = result.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

			foreach (string command in commands)
			{
				string[] data = command.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries);
				if (data.Length != 3)
					continue;

				var com = new Command(data[0].Trim(), data[1].Trim(), data[2].Trim());
				GetCommandHelp(ref com);

				allCommands.Add(com);
			}

			return allCommands;
		}

		/// <summary>
		/// Get the command help by calling the command followed by '?'
		/// </summary>
		/// <param name="command"></param>
		private void GetCommandHelp(ref Command command)
		{
			string result = Connection.SendCommand($"{command.Name} ?");

			//if (!CheckIfValidHelp(result)) TODO
			//{
			//	result = Connection.SendCommand($"{command.Name} help");

			//	if (!CheckIfValidHelp(result))
			//	{
			//		result = Connection.SendCommand(command.Name);
			//	}
			//}

			command.Help = result.Trim();
		}

		/// <summary>
		/// Verify that the help value returned valid
		/// </summary>
		/// <param name="helpResult"></param>
		/// <returns></returns>
		private static bool CheckIfValidHelp(string helpResult)
		{
			return helpResult != "\r\n" && helpResult != "\r" && helpResult != "\n" && helpResult != "" && helpResult != " ";
		}

		/// <summary>
		/// Write all of the commands to an easy to read html file
		/// </summary>
		/// <param name="commands"></param>
		private void WriteCommandsToFile(List<Command> commands)
		{
			string fileName = hostName.Text;
			string path = $@"..\..\..\{fileName}.html";

			FileStream fs;
			try
			{
				fs = File.OpenWrite(path);
				fs.Close();
			}
			catch
			{
				fs = File.Create(path);
				fs.Close();
			}

			StringBuilder htmlFile = new StringBuilder();

			htmlFile.AppendLine("<!DOCTYPE HTML>");
			htmlFile.AppendLine("<html>");
			htmlFile.AppendLine("<head>  <title>TEST FILE</title>");
			htmlFile.AppendLine("    <meta name=\"utility_author\" content=\"Frank Cusano\">");
			htmlFile.AppendLine("    <meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">");
			htmlFile.AppendLine("    <style type=\"text/css\">");
			htmlFile.AppendLine("        table { page-break-inside:auto }");
			htmlFile.AppendLine("        tr    { page-break-inside:avoid; page-break-after:auto }");
			htmlFile.AppendLine("        thead { display:table-header-group }");
			htmlFile.AppendLine("        tfoot { display:table-footer-group }");
			htmlFile.AppendLine("    </style>");
			htmlFile.AppendLine("</head>\n<body>\n<font face='arial'>");

			htmlFile.AppendLine("<h1>This is a test</h1>");
			htmlFile.AppendLine($"<p>&nbsp;&nbsp; {commands.Count} normal commands found.</p>");

			htmlFile.AppendLine(
				"<table border=\"1\" cellpadding=\"5\" cellspacing=\"5\" style=\"border-collapse:collapse;\" width=\"100%\">\n");

			foreach (var command in commands)
			{
				htmlFile.AppendLine("<tr bgcolor=\"#C0C0C0\">");
				htmlFile.AppendLine($"    <th width=\"20%\"><font color=\"#000000\">{command.Name}</font></th>");
				htmlFile.AppendLine($"    <th align=\"left\"><font color=\"#000000\">{command.Description} </font></th>");
				htmlFile.AppendLine("</tr>");

				htmlFile.AppendLine("<tr>");
				htmlFile.AppendLine("<td colspan=\"2\">");
				htmlFile.AppendLine($"<pre>\n{command.Help}\n</pre>");
				htmlFile.AppendLine("</td>");
				htmlFile.AppendLine("</tr>\n");
			}
			htmlFile.AppendLine("</table>\n</font>\n</body>\n</html>");

			File.WriteAllText(path, htmlFile.ToString());
		}

		#endregion

	}
}
