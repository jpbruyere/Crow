//from Mono.Cairo
//fake FontOptions

using System;

namespace vkvg
{
	public enum SubpixelOrder
	{
		Default,
		Rgb,
		Bgr,
		Vrgb,
		Vbgr,
	}
	public enum HintMetrics
	{
		Default,
		Off,
		On,
	}
	public enum HintStyle
	{
		Default,
		None,
		Slight,
		Medium,
		Full,
	}		
	public class FontOptions : IDisposable
	{
		public FontOptions () {	}


		public FontOptions Copy () => default;


		public IntPtr Handle {
			get ;
		}

	
		public void Merge (FontOptions other)
		{
		}

		public void Dispose() {}

		public Antialias Antialias {
			get ;
			set ;
		}

		public HintMetrics HintMetrics {
			get ;
			set ;
		}

		public HintStyle HintStyle {
			get ;
			set ;
		}

		public Status Status {
			get ;
		}

		public SubpixelOrder SubpixelOrder {
			get ;
			set ;
		}
	}
}

