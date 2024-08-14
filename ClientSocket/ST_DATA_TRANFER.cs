using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClientSocket
{
	[StructLayout(LayoutKind.Sequential,Pack =1)]
	public struct ST_DATA_TRANFER
	{
		public uint DataInt;
		public ushort DataUshort;
		[MarshalAs(UnmanagedType.ByValTStr,SizeConst =10)]
		public string DataString;
		public byte[] DataByteArr;
		public bool DataBool;
	}
}
