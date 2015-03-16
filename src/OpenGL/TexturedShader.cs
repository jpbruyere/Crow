using System;
using OpenTK.Graphics.OpenGL;

namespace go.GLBackend
{
	public class TexturedShader : Shader
	{
		public TexturedShader ()
		{
			vertSource = @"
				#version 130

				precision highp float;

				uniform mat4 projection_matrix;
				uniform mat4 modelview_matrix;

				in vec2 in_position;
				in vec2 in_tex;
				out vec2 texCoord;


				void main(void)
				{
					texCoord = in_tex;
					gl_Position = projection_matrix * modelview_matrix * vec4(in_position,0, 1);
				}";

			fragSource = @"
				#version 130
				precision highp float;

				uniform vec4 color;
				uniform sampler2D tex;
				uniform sampler2D stencil;

				in vec2 texCoord;
				out vec4 out_frag_color;

				void main(void)
				{
//					vec4 s = texture( stencil, texCoord);
//					if (s.r == 0)
//						discard;
					vec4 t = texture( tex, texCoord);
					out_frag_color = t;
				}";

			Compile ();

		}

		protected override void BindVertexAttributes ()
		{
			base.BindVertexAttributes ();
			GL.BindAttribLocation(pgmId, 1, "in_tex");
		}
		protected override void BindSamplesSlots ()
		{
			GL.Uniform1(GL.GetUniformLocation (pgmId, "tex"),0);
			GL.Uniform1(GL.GetUniformLocation (pgmId, "stencil"),1);
		}
	}
}

