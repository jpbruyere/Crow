// Copyright (c) 2021  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Drawing2D
{
	public enum RegionOverlap {
		In,
		Out,
		Part,
	}

	public interface IRegion : IDisposable, IEquatable<IRegion>
	{
		bool IsEmpty { get; }
		int NumRectangles { get; }
		Rectangle GetRectangle (int nth);
		void UnionRectangle (Rectangle r);
		bool OverlapOut (Rectangle r);
		RegionOverlap Contains (Rectangle rectangle);
		void Reset ();
	}
}
