﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;
using System.Windows;

namespace CommandScanner.HelperClasses
{
	internal class ConnectionService : IDisposable
	{
		#region Properties & Fields

		private ConnectionType DeviceConnectionType { get; }
		private string Address { get; }
		private int Port { get; }
		private string Username { get; }
		private string Password { get; }
		private SshClient _sshClient;
		//private TcpClient _ctpClient;

		#endregion

		#region Constructors

		/// <summary>
		/// Establish a new SSH or CTP client
		/// </summary>
		/// <param name="hostName">The host name or IP address of the device you wish to connect to</param>
		/// <param name="connectionType"></param>
		public ConnectionService(string hostName, ConnectionType connectionType)
		{
			DeviceConnectionType = connectionType;

			switch (DeviceConnectionType)
			{
				case ConnectionType.Ssh:
					Address = hostName;
					Port = 22;

					// set the username and password for authentication
					Username = "crestron";
					Password = "";

					// save the connection info
					_sshClient = new SshClient(Address, Port, Username, Password);
					break;

				case ConnectionType.Ctp:
					Address = hostName;
					Port = 41795;
					break;
			}
		}

		#endregion

		#region Destructor

		/// <summary>
		/// Dispose of the SSH/CTP client
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_sshClient?.Dispose();
				_sshClient = null;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Create a new instance of a connection client and connects to the device
		/// </summary>
		public bool Connect()
		{
			try
			{
				switch (DeviceConnectionType)
				{
					case ConnectionType.Ssh:
						_sshClient.Connect();

						if (_sshClient.IsConnected)
							return true;
						break;

					case ConnectionType.Ctp:
						using (var ctpClient = new TcpClient(Address, Port))
						{
							if (ctpClient.Connected)
								return true;
						}
						break;
				}
			}
			catch
			{
				MessageBox.Show($"Unable to connect to {Address}", "Connection Failed");
				return false;
			}
			return false;
		}

		/// <summary>
		/// Disconnects from the device and destroys the connection
		/// </summary>
		public void Disconnect()
		{
			switch (DeviceConnectionType)
			{
				case ConnectionType.Ssh:
					_sshClient.Disconnect();
					_sshClient.Dispose();
					break;
				//case ConnectionType.Ctp:
				//	_ctpClient.Close();
				//	break;
			}
		}

		/// <summary>
		/// Connect to the client and send a command
		/// </summary>
		/// <param name="inputCommand">The command with arguments</param>
		/// <returns>The command response</returns>
		public string SendCommand(string inputCommand)
		{
			try
			{
				string commandResult;

				switch (DeviceConnectionType)
				{
					case ConnectionType.Ssh:
						if (!_sshClient.IsConnected)
							_sshClient.Connect();

						commandResult = SendCommand(_sshClient, inputCommand);
						return commandResult;

					case ConnectionType.Ctp:
						using (var ctpClient = new TcpClient(Address, Port))
						{
							NetworkStream stream = ctpClient.GetStream();
							commandResult = SendCommand(stream, inputCommand);
							stream.Close();
						}
						return commandResult;

					// TODO add case for ConnectionType.Auto

					default:
					{
						return null;
					}
				}
			}
			catch (Exception exception)
			{
				return exception.Message;
			}
		}

		private string SendCommand(SshClient sshClient, string inputCommand)
		{
			var task = Task.Run(() => sshClient.RunCommand(inputCommand));

			if (!task.Wait(TimeSpan.FromSeconds(2)))
				return string.Empty;

			if (task.Result.ExitStatus != 0)
				throw new Exception($"Send Command Error: {task.Result.Error} for command {inputCommand}");

			return task.Result.Result;
		}

		private string SendCommand(NetworkStream stream, string inputCommand)
		{
			var writer = new StreamWriter(stream);
			var reader = new StreamReader(stream);

			writer.WriteLine(inputCommand);
			writer.Flush();

			var prompt = ">";
			var size = 4096;
			var buffer = new char[size];
			var resultString = new StringBuilder();

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			while (!resultString.ToString().Contains(prompt) && stopwatch.ElapsedMilliseconds < 30000)
			{
				Thread.Sleep(200); // wait for the buffer to fill up
				while (stream.DataAvailable)
				{
					try
					{
						int readsize = reader.Read(buffer, 0, buffer.Length);
						resultString.Append(buffer, 0, readsize);
					}
					catch (Exception exception)
					{
						MessageBox.Show($"Send Command Failed: {exception.Message}");
					}
				}
			}

			stopwatch.Stop();
			return resultString.ToString();
		}

		#endregion
	}
}