using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LogReceiver
{
	public class Server
	{
		private int port;
		private bool echo;
		private bool saveToFile;
		private bool saveToWorkingDir;
		private Thread listenThread;
		private TcpListener tcpListener;

		private bool isRunning = false;

		private string basePath;

		public Server(int port, bool echo, bool saveToFile, bool saveToWorkingDir)
		{
			this.port = port;
			this.echo = echo;
			this.saveToFile = saveToFile;
			this.saveToWorkingDir = saveToWorkingDir;

			basePath = saveToWorkingDir ? Environment.CurrentDirectory : Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "Logs");

			if (saveToFile)
			{
				if (!Directory.Exists(basePath))
				{
					Directory.CreateDirectory(basePath);
					Console.WriteLine("Created directory: " + basePath);
				}
				Console.WriteLine("Saving logs into directory: " + basePath);
			}
		}

		public void Start()
		{
			try
			{
				tcpListener = new TcpListener(IPAddress.Any, port);
				tcpListener.Start();

				Console.WriteLine("Started listening on port " + port);

				isRunning = true;
				listenThread = new Thread(Listen);
				listenThread.Start();
			}
			catch (Exception ex)
			{
				ConsoleError(ex.ToString());
			}
		}

		private void Listen()
		{
			while (isRunning)
			{
				TcpClient tcpClient = tcpListener.AcceptTcpClient();
				Thread receiveThread = new Thread(() =>
				{
					try
					{
						if (saveToFile)
							Receive(tcpClient);
						else
							ReceiveNoSave(tcpClient);
					}
					catch (Exception ex)
					{
						ConsoleError(ex.ToString());
					}
				});
				receiveThread.Start();
			}
		}

		enum LogType
        {
			Log,
			Warning,
			Error,
			Exception,
			Assert
        }

		private void Receive(TcpClient client)
		{
			ConsoleLog("Got connection from " + client.Client.RemoteEndPoint.ToString(), ConsoleColor.Magenta);
			string path = Path.Combine(basePath, $"log_{DateTime.Now:dd-MM-yyyy_hh-mm-ss}.txt");

			using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
			using (StreamWriter writer = new StreamWriter(fs)) 
			{
				Console.WriteLine("Opened file for writing: " + path);

				using (NetworkStream ns = client.GetStream())
				using (StreamReader reader = new StreamReader(ns))
				{
					while (isRunning && client.Connected)
					{
						string line = reader.ReadLine();
						if (line == null) break;

						writer.WriteLine(line);
						writer.Flush();

						if (echo)
						{
							ConsoleColor clr = ConsoleColor.DarkGray;
							if (line.Contains("[Warning]"))
                            {
								clr = ConsoleColor.DarkYellow;
                            }
							else if (line.Contains("[Error]") || line.Contains("[Exception]"))
                            {
								clr = ConsoleColor.Red;
                            }

							var origClr = Console.ForegroundColor;
							Console.ForegroundColor = clr;
							Console.WriteLine(line);
							Console.ForegroundColor = origClr;
						}
					}

					writer.Close();
					reader.Close();

					ConsoleLog("Client disconnected, closed file: " + path, ConsoleColor.Magenta);
				}
			}
		}

		private void ReceiveNoSave(TcpClient client)
		{
			ConsoleLog("Got connection from " + client.Client.RemoteEndPoint.ToString(), ConsoleColor.Magenta);
			
			using (NetworkStream ns = client.GetStream())
			using (StreamReader reader = new StreamReader(ns))
			{
				while (isRunning && client.Connected)
				{
					string line = reader.ReadLine();
					if (line == null) break;

					if (echo)
					{
						ConsoleColor clr = ConsoleColor.DarkGray;
						if (line.Contains("[Warning]"))
						{
							clr = ConsoleColor.DarkYellow;
						}
						else if (line.Contains("[Error]") || line.Contains("[Exception]"))
						{
							clr = ConsoleColor.Red;
						}

						var origClr = Console.ForegroundColor;
						Console.ForegroundColor = clr;
						Console.WriteLine(line);
						Console.ForegroundColor = origClr;
					}
				}

				reader.Close();

				ConsoleLog("Client disconnected", ConsoleColor.Magenta);
			}
		}

		public void Stop()
		{
			isRunning = false;
			tcpListener.Stop();
		}

		private static void ConsoleLog(string str, ConsoleColor color)
		{
			var clr = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine(str);
			Console.ForegroundColor = clr;
		}

		private static void ConsoleError(string str)
		{
			ConsoleLog(str, ConsoleColor.DarkRed);
		}
	}
}
