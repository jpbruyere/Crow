using System;
using OpenTK.Graphics.OpenGL;

namespace Crow
{
	public class TexturedShader : Shader
	{
		public TexturedShader ()
		{

			fragSource = @"
				#version 130
				precision highp float;

				uniform sampler2D tex;

				in vec2 texCoord;
				out vec4 out_frag_color;

				void main(void)
				{
					out_frag_color = texture( tex, texCoord);
				}";

			Compile ();

		}

		protected override void BindSamplesSlots ()
		{
			base.BindSamplesSlots ();

			GL.Uniform1(GL.GetUniformLocation (pgmId, "stencil"),1);
		}
		public override void Enable ()
		{
			base.Enable ();

		}
	}
}

