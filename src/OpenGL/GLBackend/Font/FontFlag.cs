using System;

namespace go
{
	[Flags]
	public enum FontFlag
	{
		None = 0x00,
		Sans = 0x01,
		Serif = 0x02,
		Mono = 0x04,
		Condensed = 0x08,
		Medium = 0x16,
		Book = 0x32,
		ExtraLight = 0x64
	}
}

