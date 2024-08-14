using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClientSocket
{
	[StructLayout(LayoutKind.Sequential)]
	public struct ST_DATA_TRANFER
	{
		public uint DataInt;
		public ushort DataUshort;
		[MarshalAs(UnmanagedType.ByValTStr,SizeConst =100)]
		public string DataString;
		public byte[] DataByteArr;
		public bool DataBool;
		public override string ToString()
		{
			return $" dataInt {DataInt}, dataUshort {DataUshort}, dataString {DataString}, dataByteArrLenght {DataByteArr.Length}, DataBool {DataBool}";
		}
	}
}
