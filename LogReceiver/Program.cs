using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace LogReceiver
{
	class Program
	{

		static void Main(string[] args)
		{
			if (args.Length > 0 && (args.Contains("help") || args.Contains("-help") || args.Contains("--help") || args.Contains("-h")))
			{
				PrintHelp();
				return;
			}

			int port = 999;
			if (args.Length > 0 && int.TryParse(args.Last(), out int portParsed))
            {
				port = portParsed;
			}

			Console.WriteLine($"========================== LogReceiver v{typeof(Program).Assembly.GetName().Version} ==========================");

			Server server = new Server(port, !args.Contains("--noecho"), !args.Contains("--nosave"), args.Contains("--here"));
			server.Start();

			bool isRunning = true;
			Console.CancelKeyPress += delegate {
				isRunning = false;
			};

			while (isRunning) { Thread.Sleep(1000); }

			server.Stop();

			if (!IsInShell())
				Console.ReadKey();
		}

		static void PrintHelp()
        {
			Console.WriteLine("Usage: LogReceiver [--noecho] [--nosave] [--here] <port>");
			Console.WriteLine("    If no args are supplied default port is 999, echo and save is on and logs are saved to app directory");
			Console.WriteLine("");
			Console.WriteLine("    --noecho   - Do not print logs");
			Console.WriteLine("    --nosave   - Do not save logs to file");
			Console.WriteLine("    --here     - Will save logs into working directory");
			Console.ReadKey();
		}

		static bool IsInShell()
        {
			Process p = Process.GetCurrentProcess();
			PerformanceCounter parent = new PerformanceCounter("Process", "Creating Process ID", p.ProcessName);
			int ppid = (int)parent.NextValue();

			return Process.GetProcessById(ppid).ProcessName == "powershell" 
				|| Process.GetProcessById(ppid).ProcessName == "cmd";
		}
	}
}
