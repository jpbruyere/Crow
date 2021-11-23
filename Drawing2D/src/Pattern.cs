// Copyright (c) 2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
namespace Drawing2D
{
	public interface IPattern : IDisposable
	{
		Extend Extend { get; set; }
		Filter Filter { get; set; }
	}
}