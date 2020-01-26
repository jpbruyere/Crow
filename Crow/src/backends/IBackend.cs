// Copyright (c) 2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;

namespace Crow
{
	public interface IBackend
	{
		void Init(Interface iFace);
		void CleanUp();
		void Flush();
		void ProcessEvents();

		MouseCursor Cursor { set; }
		bool IsDown (Key key);
		bool Shift { get; }
		bool Ctrl { get; }
		bool Alt { get; }
	}
}

