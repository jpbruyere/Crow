using Crow;
using System;

namespace PathPainter
{
	class Program : SampleBase
	{
		static void Main (string[] args) {
			using (Program app = new Program ())				
				app.Run ();			
		}

        protected override void OnInitialized () {
            base.OnInitialized ();
			Load ("#ui.main.crow").DataSource = this;
		}
		string currentPath = "M 5.5,0.5 L 10.5,10.5 L 0.5,10.5 Z F";
		int currentSize = 11;
		double zoom = 1.0;
		double strokeWidth = 1.0;
		Color foreground = Colors.Black;
		Color background = Colors.White;

		public Measure DesignSize => (int)(zoom * currentSize); 

		public double StrokeWidth {
			get => strokeWidth;
			set {
				if (strokeWidth == value)
					return;
				strokeWidth = value;
				NotifyValueChanged (strokeWidth);
            }
        }
		public string CurrentPath {
			get => currentPath;
			set {
				if (currentPath == value)
					return;
				currentPath = value;
				NotifyValueChanged (currentPath);
			}
		}
		public Size Size => new Size (currentSize);
		public int CurrentSize {
			get => currentSize;
			set {
				if (currentSize == value)
					return;
				currentSize = value;
				NotifyValueChanged (currentSize);
				NotifyValueChanged ("Size", (object)new Size (currentSize));
				NotifyValueChanged ("CurrentPath", (object)CurrentPath);
			}
		}
		public Color Foreground {
			get => foreground;
			set {
				if (foreground == value)
					return;
				foreground = value;
				NotifyValueChanged (foreground);
			}
		}
		public Color Background {
			get => background;
			set {
				if (background == value)
					return;
				background = value;
				NotifyValueChanged (background);
			}
		}

	}
}
