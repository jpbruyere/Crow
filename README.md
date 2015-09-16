GOLib
=====

GOLib stands for 'Graphic Object Library' which is a pure c# widget toolkit with templates and bindings.
Running under Mono, With multi-platform libraries (Cairo, OpenTK) it should run on any target.

The main advantage of this toolkit is it's simplicity and it's coherence. Thanks to the job done by 
OpenTK team on linux drm/kms support, GOLib may run without a X server directely in console.

Graphic Rendering stack could easily be changed by implementing IGOLibHost, and a custom (and lighter) opengl rendering replacement for cairo is on the stack.

FEATURES
========

- Use OpenTK as top container for device abstraction layer by default, (other container: GTK, GDK)
- Curent drawing routines use Mono.Cairo
- Allow easy creation of XAML like interface under linux directely in console mode, without X
  It only required Mono with cairo libraries, OpenTK, Mesa, GBM and DRM libraries.
- Templated controls, with dynamic binding.
- Inlined delegate in XML

Example
-------

```
<Window Width="800" Height="600" Title="Decks">	
	<HorizontalStack>
		<ListBox Height="0" Width="150" Background="0,5;0,5;0,5;0,5" Name="deckList"
			Data="{DeckList}" HorizontalAlignment="Left" ValueChanged="onDeckListValueChange"
			ItemTemplate="#Magic3D.ui.DeckItemTemplate.goml">
			<Template>
				<Border BorderWidth="1" MinimumSize="0;100" Height="{TemplatedHeight}" Width="{TemplatedWidth}">
					<HorizontalStack Margin="1" Height="{TemplatedHeight}" Width="{TemplatedWidth}">
						<Scroller  Name="scroller1" Height="{TemplatedHeight}" Width="{TemplatedWidth}" 
						 Margin="2" VerticalScrolling="true" ScrollY="{../scrollbar1.Scroll}">
							<VerticalStack Width="{TemplatedWidth}" Height="-1" Name="List" Margin="0" VerticalAlignment="Top"/>
						</Scroller>
						<Scrollbar Name="scrollbar1" Scroll="{../scroller1.ScrollY}" MaximumScroll="{../scroller1.MaximumScroll}"
							Orientation="Vertical" Width="10" Height="{TemplatedHeight}" />
					</HorizontalStack>
				</Border>
			</Template>
		</ListBox>
		<HorizontalStack Width="0" Height="0" Background="DimGray" Name="hsDeck"/>
	</HorizontalStack>
</Window>
```
Building
========

#####Build latest OpenTK:
```
git clone https://github.com/opentk/opentk   # Download source code from git
cd opentk                                    # Enter the source directory
msbuild /p:Configuration=Release OpenTK.sln  # Build on .Net (Windows)
xbuild  /p:Configuration=Release OpenTK.sln  # Build on Mono (Linux / Mac OS X)
```
#####Install Cairo and RSVG cli bindings
######On Debian:

```
sudo apt-get install libmono-cairo4.0-cil libglib3.0-cil librsvg2-2.18-cil
```
#####Build GOLib
```
git clone https://github.com/jpbruyere/GOLib.git   	# Download source code from git
cd GOLib                                    		# Enter the source directory
msbuild /p:Configuration=Release GOLib.sln  		# Build on .Net (Windows)
xbuild  /p:Configuration=Release GOLib.sln  		# Build on Mono (Linux / Mac OS X)
```
#####GOLib in action

![GOLib in action](/magic3d.png?raw=true "Magic3d")

![GOLib in action](/screenshot1.png?raw=true "golib")

![GOLib in action](/screenshot2.png?raw=true "golib")


