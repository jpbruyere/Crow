// Copyright (c) 2013-2019  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Glfw;
using Crow.Text;

namespace Crow
{
	public class Editor : TextBox {
		public override void onKeyDown(object sender, KeyEventArgs e)
		{
			TextSpan selection = Selection;
			if (e.Key == Key.Tab && !selection.IsEmpty) {
				int lineStart = lines.GetLocation (selection.Start).Line;
				int lineEnd = lines.GetLocation (selection.End).Line;

				if (IFace.Shift) {
					for (int l = lineStart; l <= lineEnd; l++) {				
						if (Text[lines[l].Start] == '\t')
							update (new TextChange (lines[l].Start, 1, ""));
						else if (Char.IsWhiteSpace (Text[lines[l].Start])) {
							int i = 1;
							while (i < lines[l].Length && i < Interface.TAB_SIZE && Char.IsWhiteSpace (Text[i]))
								i++;
							update (new TextChange (lines[l].Start, i, ""));
						}
					}

				}else{
					for (int l = lineStart; l <= lineEnd; l++)		
						update (new TextChange (lines[l].Start, 0, "\t"));				
				}

                selectionStart = new CharLocation (lineStart, 0);
                CurrentLoc = new CharLocation (lineEnd, lines[lineEnd].Length);

				return;
			}
			base.onKeyDown(sender, e);			
		}
	}
}