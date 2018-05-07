//
// enums.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// Copyright (c) 2018 jp
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Runtime.InteropServices;

namespace vkh
{
	[Flags]
	public enum QueueFlags : uint{
		Graphic = 0x01,
		Compute = 0x02,
		Transfer = 0x04,
		SparseBinding = 0x08
	}
		
	[StructLayout(LayoutKind.Sequential)]
	public struct Extent3D {
		public uint width;
		public uint height;
		public uint depth;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Extent2D {
		public uint width;
		public uint height;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct QueueFamilyProperties {
		public QueueFlags	queueFlags;
		public uint			queueCount;
		public uint			timestampValidBits;
		public Extent3D		minImageTransferGranularity;
	}
}

