using System;
using System.Collections.Generic;
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
		private Thread listenThread;
		private TcpListener tcpListener;

		private bool isRunning = false;

		public Server(int port, bool echo)
		{
			this.port = port;
			this.echo = echo;

			if (!Directory.Exists("Logs"))
			{
				Directory.CreateDirectory("Logs");
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
						Receive(tcpClient);
					}
					catch (Exception ex)
					{
						ConsoleError(ex.ToString());
					}
				});
				receiveThread.Start();
			}
		}

		private void Receive(TcpClient client)
		{
			Console.WriteLine("Got connection from " + client.Client.RemoteEndPoint.ToString());
			string path = Path.Combine("Logs", $"log_{DateTime.Now:dd-MM-yyyy_hh-mm-ss}.txt");

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
							var clr = Console.ForegroundColor;
							Console.ForegroundColor = ConsoleColor.DarkGray;
							Console.WriteLine(line);
							Console.ForegroundColor = clr;
						}
					}

					writer.Close();
					reader.Close();

					Console.WriteLine("Closed file: " + path);
				}
			}
		}

		public void Stop()
		{
			isRunning = false;
			tcpListener.Stop();
		}

		private static void ConsoleError(string str)
		{
			var clr = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(str);
			Console.ForegroundColor = clr;
		}
	}
}
