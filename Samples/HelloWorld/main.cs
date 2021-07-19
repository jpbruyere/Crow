using System;
using Crow;
using Samples;

namespace HelloWorld
{
	class Program : SampleBase {
		static void Main (string[] args) {
			using (Interface app = new Program ()) {
				//app.Initialized += (sender, e) => (sender as Interface).Load ("#HelloWorld.helloworld.crow").DataSource = sender;
				/*app.Initialized += (sender, e) => (sender as Interface).LoadIMLFragment (@"
<Border Background='Red' Margin='100' Fit='true' BorderWidth='10'>
<Image Path='/mnt/devel/CrowIDE/Crow/Images/screenshot3.png' Width='100' Height='100' />
</Border>
").DataSource = sender;*/
app.Initialized += (sender, e) => (sender as Interface).LoadIMLFragment (@"
<Container Background='Jet' Margin='10'					CacheEnabled='false'>
	<CheckBox Background='Blue' 							CacheEnabled='false'>
		<Template>
			<Border Background='{./Background}' Margin='5'	CacheEnabled='false'>
				<HorizontalStack Margin='30' 				CacheEnabled='true'>
					<Label Background='Pink' Fit='true' 	CacheEnabled='false'/>
				</HorizontalStack>
			</Border>
		</Template>
	</CheckBox>
</Container>
").DataSource = sender;
				app.Run ();
			}
		}
	}
}
