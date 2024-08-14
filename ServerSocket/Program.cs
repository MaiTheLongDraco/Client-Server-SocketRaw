using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSocket
{
	internal class Program
	{
		public static string Host = "127.0.0.1";
		public static int Port = 1234;
		static void Main(string[] args)
		{
			Server.Start(Host, Port,50);
		}
	}
}
