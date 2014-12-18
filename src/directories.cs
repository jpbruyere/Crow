using System;

namespace go
{
	public static class directories
	{
#if _WIN32 || _WIN64
        public const string rootDir = @"d:/";
#elif __linux__
        public const string rootDir = @"/mnt/data/";
#endif
		
	}
}

