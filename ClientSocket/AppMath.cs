using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClientSocket
{
	public class AppMath
	{
		public static void ConvertStructeToByteArr<T>(T structure,ref int size, ref byte[] output)
		{
			size= Marshal.SizeOf(structure);
			if(size<=0) return;	
			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(structure, ptr, false);
			Marshal.Copy(ptr, output, 0, size);
			Marshal.FreeHGlobal(ptr);
		}
		public static void ConvertByteArrToStructure<T>(byte[] input, ref int size,ref T output)
		{
			size = input.Length;
			if (size <= 0) return;
			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.Copy(input,0,ptr, size);
			Marshal.PtrToStructure(ptr, output);
			Marshal.FreeHGlobal(ptr);
		}
	}
}
