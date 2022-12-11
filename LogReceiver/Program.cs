using System;
using System.Linq;

namespace LogReceiver
{
	class Program
	{

		static void Main(string[] args)
		{
			if (args.Length == 0 || !int.TryParse(args.Last(), out int port))
			{
				Console.WriteLine("Usage: LogReceiver [--echo] <port>");
				Console.ReadKey();
				return;
			}

			Server server = new Server(port, args.Contains("--echo"));
			server.Start();

			bool isRunning = true;
			Console.CancelKeyPress += delegate {
				isRunning = false;
			};

			while (isRunning) { }

			server.Stop();
		}
	}
}
