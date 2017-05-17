//
// VT.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
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

namespace Linux.VT {
	public enum KDMode : byte {
		TEXT	= 0x00,
		GRAPHICS= 0x01,
		TEXT0	= 0x02,	/* obsolete */
		TEXT1	= 0x03	/* obsolete */
	}
	public enum SwitchMode : sbyte {
		AUTO	= 0x00,	/* auto vt switching */
		PROCESS	= 0x01,	/* process controls switching */
		ACKACQ	= 0x02	/* acknowledge switch */
	}
	public enum KbdMode{
		RAW			= 0x00,
		XLATE		= 0x01,
		MEDIUMRAW	= 0x02,
		UNICODE		= 0x03,
		OFF			= 0x04,
	}
	public struct vt_mode {
		public SwitchMode mode;		/* vt mode */
		public sbyte waitv;		/* if set, hang on writes if not active */
		public short relsig;	/* signal to raise on release req */
		public short acqsig;	/* signal to raise on acquisition */
		public short frsig;		/* unused (set to 0) */
	}
	public struct State {
		public ushort v_active;	/* active vt */
		public ushort v_signal;	/* signal to send */
		public ushort v_state;	/* vt bitmask */
	}
	public struct Sizes {
		public ushort v_rows;		/* number of rows */
		public ushort v_cols;		/* number of columns */
		public ushort v_scrollsize;	/* number of lines of scrollback */
	}

	public struct KbEntry {
		public byte kb_table;
		public byte kb_index;
		public ushort kb_value;
	}

	public class VTControler : IDisposable {
		public int fd = -1;

		#region ctor
		public VTControler (string path = "/dev/tty") {			
			fd = Libc.open (path, OpenFlags.ReadWrite);
			if (fd <= 0)
				throw new Exception ("VTControler: unable to open " + path);
		}
		#endregion

		/// <summary>set Graphic or Text mode for VT. </summary>
		unsafe public KDMode KDMode {
			get {
				KDMode m = 0;
				if (ioctl (fd, KDGETMODE, ref m) < 0)
					throw new Exception ("VTControler: failed to get current TTY mode");				
				return m;
			}
			set {				
				if (ioctl (fd, KDSETMODE, value) < 0)
					throw new Exception ("VTControler: failed to set current TTY mode");
			}
		}
		/// <summary>set AUTO or PROCESS mode for VT. </summary>
		unsafe public vt_mode VTMode {
			get {
				vt_mode m = new vt_mode();
				if (ioctl (fd, VT_GETMODE, ref m) < 0)
					throw new Exception ("VTControler: failed to get VTMode for current");				
				return m;
			}
			set {				
				if (ioctl (fd, VT_SETMODE, ref value) < 0)
					throw new Exception ("VTControler: failed to set VTMode for current VT");
			}
		}

		/// <summary>
		/// Switchs to V.
		/// </summary>
		/// <param name="vt">Virtual Terminal to switch to</param>
		/// <param name="waitCompleted">If set to <c>true</c> wait until switch is completed.</param>
		public void SwitchTo (int vt, bool waitCompleted = true){
			if (ioctl (fd, VT_ACTIVATE, vt) < 0)
				throw new Exception ("VTControler: failed to activate TTY" + vt);
			if (waitCompleted) {				
				if (ioctl (fd, VT_WAITACTIVE, vt) < 0)
					throw new Exception ("VTControler: error waiting for activation of TTY" + vt);
			}
		}
		public State State {
			get {
				State vtstats = new State ();
				if (ioctl (fd, VT_GETSTATE, ref vtstats) < 0)
					throw new Exception ("VirtualTerminal: unable to get TTY Stats");
				return vtstats;
			}
		}
		public int FirstAvailableVT {
			get {
				long freeVT = 0;
				ioctl (fd, VT_OPENQRY, ref freeVT);
				return (int)freeVT;
			}
		}
		public int CurrentVT {
			get {				
				return (int)State.v_active;
			}
		}

		const int ACKACQ = 0x02;

		public void AcknoledgeSwitchRequest (){
			if (ioctl (fd, VT_RELDISP, ACKACQ)<0)
				throw new Exception ("VTControler: failed to acknowledge switch with VT_RELDISP");
		}

		#region IDisposable implementation
		~VTControler(){
			Dispose (false);
		}
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		protected virtual void Dispose (bool disposing){
			if (fd > 0)
				Libc.close (fd);
			fd = -1;
		}
		#endregion

		#region IOCTLS constants
		const uint KDSETMODE	= 0x4B3A;	/* set text/graphics mode */
		const uint KDGETMODE	= 0x4B3B;	/* get current mode */

		const uint KDGKBMODE	= 0x4B44;	/* gets current keyboard mode */
		const uint KDSKBMODE	= 0x4B45;	/* sets current keyboard mode */
		const uint KDGKBENT		= 0x4B46;	/* gets one entry in translation table */
		const uint KDSKBENT		= 0x4B47;	/* sets one entry in translation table */

		const uint VT_OPENQRY	= 0x5600;	/* find available vt */
		const uint VT_GETMODE	= 0x5601;	/* get mode of active vt */
		const uint VT_SETMODE	= 0x5602;	/* set mode of active vt */

		const uint VT_GETSTATE	= 0x5603;	/* get global vt state info */
		const uint VT_SENDSIG	= 0x5604;	/* signal to send to bitmask of vts */

		const uint VT_RELDISP	= 0x5605;	/* release display */

		const uint VT_ACTIVATE	= 0x5606;	/* make vt active */
		const uint VT_WAITACTIVE= 0x5607;	/* wait for vt active */
		const uint VT_DISALLOCATE= 0x5608;	/* free memory associated to vt */

		const uint VT_RESIZE	= 0x5609;	/* set kernel's idea of screensize */
		#endregion

		#region ioctl overrides
		[DllImport("libc")]
		static extern int ioctl(int d, uint request, ref KbEntry entry);
		[DllImport("libc")]
		static extern int ioctl(int d, uint request, ref KbdMode value);
		[DllImport("libc")]
		static extern int ioctl(int d, uint request, ref long value);
		[DllImport("libc")]
		static extern int ioctl(int d, uint request, int value);
		[DllImport("libc")]
		static extern int ioctl(int d, uint request, ref int value);
		[DllImport("libc")]
		static extern int ioctl(int d, uint request, ref State value);
		[DllImport("libc")]
		static extern int ioctl(int d, uint request, ref SwitchMode value);
		[DllImport("libc")]
		static extern int ioctl(int d, uint request, KDMode value);
		[DllImport("libc")]
		static extern int ioctl(int d, uint request, ref KDMode value);
		[DllImport("libc")]
		static extern int ioctl(int d, uint request, vt_mode value);
		[DllImport("libc")]
		static extern int ioctl(int d, uint request, ref vt_mode value);
		#endregion
	}	
}