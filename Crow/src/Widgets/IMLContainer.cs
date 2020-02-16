// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Crow
{
	public class IMLContainer : PrivateContainer
	{
		public IMLContainer () : base()
		{
		}

		string path;

		public string Path {
			get { return path; }
			set {
				if (path == value)
					return;
				path = value;
				this.SetChild (IFace.CreateInstance (path));
				NotifyValueChanged ("Path", path);
			}
		}
	}
}

