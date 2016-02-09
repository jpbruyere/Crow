using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Reflection;
using System.Diagnostics;



namespace Crow
{
	public struct Color
    {
		#region CTOR
		public Color(double _R, double _G, double _B, double _A)
		{
			A = _A.Clamp(0,1);
			R = _R.Clamp(0,1);
			G = _G.Clamp(0,1);
			B = _B.Clamp(0,1);
			Name = "";
		}
		internal Color(double _R, double _G, double _B, double _A, string _name)
		{
			A = _A;
			R = _R;
			G = _G;
			B = _B;
			Name = _name;
			ColorDic.Add(this);
		}        
		#endregion

		public static List<Color> ColorDic = new List<Color>();

        internal string Name;   

		#region public fields
        public double A;
        public double R;
        public double G;
        public double B;        
		#endregion

		#region Operators	
        public static implicit operator string(Color c)
        {
            return c.ToString();
        }
		public static implicit operator Color(string s)
		{
			if (string.IsNullOrEmpty(s))
				return White;

			string[] c = s.Split(new char[] { ';' });

			if (c.Length == 1)
			{
				foreach (Color cr in ColorDic)
				{
					if (string.Equals(cr.Name,s,StringComparison.Ordinal))
						return cr;
				}
			}
			return new Color(
				double.Parse(c[0]),
				double.Parse(c[1]),
				double.Parse(c[2]),
				double.Parse(c[3]));                    
		}

		public static implicit operator OpenTK.Vector4(Color c)
		{
			return new OpenTK.Vector4 ((float)c.R, (float)c.G, (float)c.B, (float)c.A);
		}
		public static implicit operator Color(OpenTK.Vector4 v)
		{
			return new Color (v.X, v.Y, v.Z, v.W);
		}
		public static implicit operator Fill(Color c){
			return new SolidColor (c) as Fill;
		}


		public static bool operator ==(Color left, Color right)
		{
			return left.A != right.A ? false :
				left.R != right.R ? false :
				left.G != right.G ? false :
				left.B != right.B ? false : true;
		}
		public static bool operator !=(Color left, Color right)
		{
			return left.A == right.A ? false :
				left.R == right.R ? false :
				left.G == right.G ? false :
				left.B == right.B ? false : true;

		}
		public static bool operator ==(Color c, string n)
		{
			return string.Equals(c.Name, n, StringComparison.Ordinal);
		}
		public static bool operator !=(Color c, string n)
		{
			return !string.Equals(c.Name, n, StringComparison.Ordinal);
		}
		public static bool operator ==(string n, Color c)
		{
			return string.Equals (c.Name, n, StringComparison.Ordinal);
		}
		public static bool operator !=(string n, Color c)
		{
			return !string.Equals (c.Name, n, StringComparison.Ordinal);
		}
		public static Color operator *(Color c, Double f)
		{
			return new Color(c.R,c.G,c.B,c.A * f);
		}
		public static Color operator +(Color c1, Color c2)
		{
			return new Color(c1.R + c2.R,c1.G + c2.G,c1.B + c2.B,c1.A + c2.A);
		}
		public static Color operator -(Color c1, Color c2)
		{
			return new Color(c1.R - c2.R,c1.G - c2.G,c1.B - c2.B,c1.A - c2.A);
		}
		#endregion


		public float[] floatArray
		{
			get { return new float[]{ (float)R, (float)G, (float)B, (float)A }; }
		}
		public Color AdjustAlpha(double _A)
		{
			return new Color (this.R, this.G, this.B, _A);
		}
			
		#region Predefined colors
        public static readonly Color Transparent = new Color(0, 0, 0, 0, "Transparent");
		public static readonly Color Clear = new Color(-1, -1, -1, -1, "Clear");
		public static readonly Color Green = new Color(0, 1.0, 0, 1.0, "Green");
		public static readonly Color AirForceBlueRaf = new Color(0.364705882352941,0.541176470588235,0.658823529411765,1.0,"AirForceBlueRaf");
		public static readonly Color AirForceBlueUsaf = new Color(0,0.188235294117647,0.56078431372549,1.0,"AirForceBlueUsaf");
		public static readonly Color AirSuperiorityBlue = new Color(0.447058823529412,0.627450980392157,0.756862745098039,1.0,"AirSuperiorityBlue");
		public static readonly Color AlabamaCrimson = new Color(0.63921568627451,0.149019607843137,0.219607843137255,1.0,"AlabamaCrimson");
		public static readonly Color AliceBlue = new Color(0.941176470588235,0.972549019607843,1,1.0,"AliceBlue");
		public static readonly Color AlizarinCrimson = new Color(0.890196078431373,0.149019607843137,0.211764705882353,1.0,"AlizarinCrimson");
		public static readonly Color AlloyOrange = new Color(0.768627450980392,0.384313725490196,0.0627450980392157,1.0,"AlloyOrange");
		public static readonly Color Almond = new Color(0.937254901960784,0.870588235294118,0.803921568627451,1.0,"Almond");
		public static readonly Color Amaranth = new Color(0.898039215686275,0.168627450980392,0.313725490196078,1.0,"Amaranth");
		public static readonly Color Amber = new Color(1,0.749019607843137,0,1.0,"Amber");
		public static readonly Color AmberSaeEce = new Color(1,0.494117647058824,0,1.0,"AmberSaeEce");
		public static readonly Color AmericanRose = new Color(1,0.0117647058823529,0.243137254901961,1.0,"AmericanRose");
		public static readonly Color Amethyst = new Color(0.6,0.4,0.8,1.0,"Amethyst");
		public static readonly Color AndroidGreen = new Color(0.643137254901961,0.776470588235294,0.223529411764706,1.0,"AndroidGreen");
		public static readonly Color AntiFlashWhite = new Color(0.949019607843137,0.952941176470588,0.956862745098039,1.0,"AntiFlashWhite");
		public static readonly Color AntiqueBrass = new Color(0.803921568627451,0.584313725490196,0.458823529411765,1.0,"AntiqueBrass");
		public static readonly Color AntiqueFuchsia = new Color(0.568627450980392,0.36078431372549,0.513725490196078,1.0,"AntiqueFuchsia");
		public static readonly Color AntiqueRuby = new Color(0.517647058823529,0.105882352941176,0.176470588235294,1.0,"AntiqueRuby");
		public static readonly Color AntiqueWhite = new Color(0.980392156862745,0.92156862745098,0.843137254901961,1.0,"AntiqueWhite");
		public static readonly Color AoEnglish = new Color(0,0.501960784313725,0,1.0,"AoEnglish");
		public static readonly Color AppleGreen = new Color(0.552941176470588,0.713725490196078,0,1.0,"AppleGreen");
		public static readonly Color Apricot = new Color(0.984313725490196,0.807843137254902,0.694117647058824,1.0,"Apricot");
		public static readonly Color Aqua = new Color(0,1,1,1.0,"Aqua");
		public static readonly Color Aquamarine = new Color(0.498039215686275,1,0.831372549019608,1.0,"Aquamarine");
		public static readonly Color ArmyGreen = new Color(0.294117647058824,0.325490196078431,0.125490196078431,1.0,"ArmyGreen");
		public static readonly Color Arsenic = new Color(0.231372549019608,0.266666666666667,0.294117647058824,1.0,"Arsenic");
		public static readonly Color ArylideYellow = new Color(0.913725490196078,0.83921568627451,0.419607843137255,1.0,"ArylideYellow");
		public static readonly Color AshGrey = new Color(0.698039215686274,0.745098039215686,0.709803921568627,1.0,"AshGrey");
		public static readonly Color Asparagus = new Color(0.529411764705882,0.662745098039216,0.419607843137255,1.0,"Asparagus");
		public static readonly Color AtomicTangerine = new Color(1,0.6,0.4,1.0,"AtomicTangerine");
		public static readonly Color Auburn = new Color(0.647058823529412,0.164705882352941,0.164705882352941,1.0,"Auburn");
		public static readonly Color Aureolin = new Color(0.992156862745098,0.933333333333333,0,1.0,"Aureolin");
		public static readonly Color Aurometalsaurus = new Color(0.431372549019608,0.498039215686275,0.501960784313725,1.0,"Aurometalsaurus");
		public static readonly Color Avocado = new Color(0.337254901960784,0.509803921568627,0.0117647058823529,1.0,"Avocado");
		public static readonly Color Azure = new Color(0,0.498039215686275,1,1.0,"Azure");
		public static readonly Color AzureMistWeb = new Color(0.941176470588235,1,1,1.0,"AzureMistWeb");
		public static readonly Color BabyBlue = new Color(0.537254901960784,0.811764705882353,0.941176470588235,1.0,"BabyBlue");
		public static readonly Color BabyBlueEyes = new Color(0.631372549019608,0.792156862745098,0.945098039215686,1.0,"BabyBlueEyes");
		public static readonly Color BabyPink = new Color(0.956862745098039,0.76078431372549,0.76078431372549,1.0,"BabyPink");
		public static readonly Color BallBlue = new Color(0.129411764705882,0.670588235294118,0.803921568627451,1.0,"BallBlue");
		public static readonly Color BananaMania = new Color(0.980392156862745,0.905882352941176,0.709803921568627,1.0,"BananaMania");
		public static readonly Color BananaYellow = new Color(1,0.882352941176471,0.207843137254902,1.0,"BananaYellow");
		public static readonly Color BarnRed = new Color(0.486274509803922,0.0392156862745098,0.00784313725490196,1.0,"BarnRed");
		public static readonly Color BattleshipGrey = new Color(0.517647058823529,0.517647058823529,0.509803921568627,1.0,"BattleshipGrey");
		public static readonly Color Bazaar = new Color(0.596078431372549,0.466666666666667,0.482352941176471,1.0,"Bazaar");
		public static readonly Color BeauBlue = new Color(0.737254901960784,0.831372549019608,0.901960784313726,1.0,"BeauBlue");
		public static readonly Color Beaver = new Color(0.623529411764706,0.505882352941176,0.43921568627451,1.0,"Beaver");
		public static readonly Color Beige = new Color(0.96078431372549,0.96078431372549,0.862745098039216,1.0,"Beige");
		public static readonly Color BigDipORuby = new Color(0.611764705882353,0.145098039215686,0.258823529411765,1.0,"BigDipORuby");
		public static readonly Color Bisque = new Color(1,0.894117647058824,0.768627450980392,1.0,"Bisque");
		public static readonly Color Bistre = new Color(0.23921568627451,0.168627450980392,0.12156862745098,1.0,"Bistre");
		public static readonly Color Bittersweet = new Color(0.996078431372549,0.435294117647059,0.368627450980392,1.0,"Bittersweet");
		public static readonly Color BittersweetShimmer = new Color(0.749019607843137,0.309803921568627,0.317647058823529,1.0,"BittersweetShimmer");
		public static readonly Color Black = new Color(0,0,0,1.0,"Black");
		public static readonly Color BlackBean = new Color(0.23921568627451,0.0470588235294118,0.00784313725490196,1.0,"BlackBean");
		public static readonly Color BlackLeatherJacket = new Color(0.145098039215686,0.207843137254902,0.16078431372549,1.0,"BlackLeatherJacket");
		public static readonly Color BlackOlive = new Color(0.231372549019608,0.235294117647059,0.211764705882353,1.0,"BlackOlive");
		public static readonly Color BlanchedAlmond = new Color(1,0.92156862745098,0.803921568627451,1.0,"BlanchedAlmond");
		public static readonly Color BlastOffBronze = new Color(0.647058823529412,0.443137254901961,0.392156862745098,1.0,"BlastOffBronze");
		public static readonly Color BleuDeFrance = new Color(0.192156862745098,0.549019607843137,0.905882352941176,1.0,"BleuDeFrance");
		public static readonly Color BlizzardBlue = new Color(0.674509803921569,0.898039215686275,0.933333333333333,1.0,"BlizzardBlue");
		public static readonly Color Blond = new Color(0.980392156862745,0.941176470588235,0.745098039215686,1.0,"Blond");
		public static readonly Color Blue = new Color(0,0,1,1.0,"Blue");
		public static readonly Color BlueBell = new Color(0.635294117647059,0.635294117647059,0.815686274509804,1.0,"BlueBell");
		public static readonly Color BlueCrayola = new Color(0.12156862745098,0.458823529411765,0.996078431372549,1.0,"BlueCrayola");
		public static readonly Color BlueGray = new Color(0.4,0.6,0.8,1.0,"BlueGray");
		public static readonly Color BlueGreen = new Color(0.0509803921568627,0.596078431372549,0.729411764705882,1.0,"BlueGreen");
		public static readonly Color BlueMunsell = new Color(0,0.576470588235294,0.686274509803922,1.0,"BlueMunsell");
		public static readonly Color BlueNcs = new Color(0,0.529411764705882,0.741176470588235,1.0,"BlueNcs");
		public static readonly Color BluePigment = new Color(0.2,0.2,0.6,1.0,"BluePigment");
		public static readonly Color BlueRyb = new Color(0.00784313725490196,0.27843137254902,0.996078431372549,1.0,"BlueRyb");
		public static readonly Color BlueSapphire = new Color(0.0705882352941176,0.380392156862745,0.501960784313725,1.0,"BlueSapphire");
		public static readonly Color BlueViolet = new Color(0.541176470588235,0.168627450980392,0.886274509803922,1.0,"BlueViolet");
		public static readonly Color Blush = new Color(0.870588235294118,0.364705882352941,0.513725490196078,1.0,"Blush");
		public static readonly Color Bole = new Color(0.474509803921569,0.266666666666667,0.231372549019608,1.0,"Bole");
		public static readonly Color BondiBlue = new Color(0,0.584313725490196,0.713725490196078,1.0,"BondiBlue");
		public static readonly Color Bone = new Color(0.890196078431373,0.854901960784314,0.788235294117647,1.0,"Bone");
		public static readonly Color BostonUniversityRed = new Color(0.8,0,0,1.0,"BostonUniversityRed");
		public static readonly Color BottleGreen = new Color(0,0.415686274509804,0.305882352941176,1.0,"BottleGreen");
		public static readonly Color Boysenberry = new Color(0.529411764705882,0.196078431372549,0.376470588235294,1.0,"Boysenberry");
		public static readonly Color BrandeisBlue = new Color(0,0.43921568627451,1,1.0,"BrandeisBlue");
		public static readonly Color Brass = new Color(0.709803921568627,0.650980392156863,0.258823529411765,1.0,"Brass");
		public static readonly Color BrickRed = new Color(0.796078431372549,0.254901960784314,0.329411764705882,1.0,"BrickRed");
		public static readonly Color BrightCerulean = new Color(0.113725490196078,0.674509803921569,0.83921568627451,1.0,"BrightCerulean");
		public static readonly Color BrightGreen = new Color(0.4,1,0,1.0,"BrightGreen");
		public static readonly Color BrightLavender = new Color(0.749019607843137,0.580392156862745,0.894117647058824,1.0,"BrightLavender");
		public static readonly Color BrightMaroon = new Color(0.764705882352941,0.129411764705882,0.282352941176471,1.0,"BrightMaroon");
		public static readonly Color BrightPink = new Color(1,0,0.498039215686275,1.0,"BrightPink");
		public static readonly Color BrightTurquoise = new Color(0.0313725490196078,0.909803921568627,0.870588235294118,1.0,"BrightTurquoise");
		public static readonly Color BrightUbe = new Color(0.819607843137255,0.623529411764706,0.909803921568627,1.0,"BrightUbe");
		public static readonly Color BrilliantLavender = new Color(0.956862745098039,0.733333333333333,1,1.0,"BrilliantLavender");
		public static readonly Color BrilliantRose = new Color(1,0.333333333333333,0.63921568627451,1.0,"BrilliantRose");
		public static readonly Color BrinkPink = new Color(0.984313725490196,0.376470588235294,0.498039215686275,1.0,"BrinkPink");
		public static readonly Color BritishRacingGreen = new Color(0,0.258823529411765,0.145098039215686,1.0,"BritishRacingGreen");
		public static readonly Color Bronze = new Color(0.803921568627451,0.498039215686275,0.196078431372549,1.0,"Bronze");
		public static readonly Color BrownTraditional = new Color(0.588235294117647,0.294117647058824,0,1.0,"BrownTraditional");
		public static readonly Color BrownWeb = new Color(0.647058823529412,0.164705882352941,0.164705882352941,1.0,"BrownWeb");
		public static readonly Color BubbleGum = new Color(1,0.756862745098039,0.8,1.0,"BubbleGum");
		public static readonly Color Bubbles = new Color(0.905882352941176,0.996078431372549,1,1.0,"Bubbles");
		public static readonly Color Buff = new Color(0.941176470588235,0.862745098039216,0.509803921568627,1.0,"Buff");
		public static readonly Color BulgarianRose = new Color(0.282352941176471,0.0235294117647059,0.0274509803921569,1.0,"BulgarianRose");
		public static readonly Color Burgundy = new Color(0.501960784313725,0,0.125490196078431,1.0,"Burgundy");
		public static readonly Color Burlywood = new Color(0.870588235294118,0.72156862745098,0.529411764705882,1.0,"Burlywood");
		public static readonly Color BurntOrange = new Color(0.8,0.333333333333333,0,1.0,"BurntOrange");
		public static readonly Color BurntSienna = new Color(0.913725490196078,0.454901960784314,0.317647058823529,1.0,"BurntSienna");
		public static readonly Color BurntUmber = new Color(0.541176470588235,0.2,0.141176470588235,1.0,"BurntUmber");
		public static readonly Color Byzantine = new Color(0.741176470588235,0.2,0.643137254901961,1.0,"Byzantine");
		public static readonly Color Byzantium = new Color(0.43921568627451,0.16078431372549,0.388235294117647,1.0,"Byzantium");
		public static readonly Color Cadet = new Color(0.325490196078431,0.407843137254902,0.447058823529412,1.0,"Cadet");
		public static readonly Color CadetBlue = new Color(0.372549019607843,0.619607843137255,0.627450980392157,1.0,"CadetBlue");
		public static readonly Color CadetGrey = new Color(0.568627450980392,0.63921568627451,0.690196078431373,1.0,"CadetGrey");
		public static readonly Color CadmiumGreen = new Color(0,0.419607843137255,0.235294117647059,1.0,"CadmiumGreen");
		public static readonly Color CadmiumOrange = new Color(0.929411764705882,0.529411764705882,0.176470588235294,1.0,"CadmiumOrange");
		public static readonly Color CadmiumRed = new Color(0.890196078431373,0,0.133333333333333,1.0,"CadmiumRed");
		public static readonly Color CadmiumYellow = new Color(1,0.964705882352941,0,1.0,"CadmiumYellow");
		public static readonly Color CafAuLait = new Color(0.650980392156863,0.482352941176471,0.356862745098039,1.0,"CafAuLait");
		public static readonly Color CafNoir = new Color(0.294117647058824,0.211764705882353,0.129411764705882,1.0,"CafNoir");
		public static readonly Color CalPolyGreen = new Color(0.117647058823529,0.301960784313725,0.168627450980392,1.0,"CalPolyGreen");
		public static readonly Color CambridgeBlue = new Color(0.63921568627451,0.756862745098039,0.67843137254902,1.0,"CambridgeBlue");
		public static readonly Color Camel = new Color(0.756862745098039,0.603921568627451,0.419607843137255,1.0,"Camel");
		public static readonly Color CameoPink = new Color(0.937254901960784,0.733333333333333,0.8,1.0,"CameoPink");
		public static readonly Color CamouflageGreen = new Color(0.470588235294118,0.525490196078431,0.419607843137255,1.0,"CamouflageGreen");
		public static readonly Color CanaryYellow = new Color(1,0.937254901960784,0,1.0,"CanaryYellow");
		public static readonly Color CandyAppleRed = new Color(1,0.0313725490196078,0,1.0,"CandyAppleRed");
		public static readonly Color CandyPink = new Color(0.894117647058824,0.443137254901961,0.47843137254902,1.0,"CandyPink");
		public static readonly Color Capri = new Color(0,0.749019607843137,1,1.0,"Capri");
		public static readonly Color CaputMortuum = new Color(0.349019607843137,0.152941176470588,0.125490196078431,1.0,"CaputMortuum");
		public static readonly Color Cardinal = new Color(0.768627450980392,0.117647058823529,0.227450980392157,1.0,"Cardinal");
		public static readonly Color CaribbeanGreen = new Color(0,0.8,0.6,1.0,"CaribbeanGreen");
		public static readonly Color Carmine = new Color(0.588235294117647,0,0.0941176470588235,1.0,"Carmine");
		public static readonly Color CarmineMP = new Color(0.843137254901961,0,0.250980392156863,1.0,"CarmineMP");
		public static readonly Color CarminePink = new Color(0.92156862745098,0.298039215686275,0.258823529411765,1.0,"CarminePink");
		public static readonly Color CarmineRed = new Color(1,0,0.219607843137255,1.0,"CarmineRed");
		public static readonly Color CarnationPink = new Color(1,0.650980392156863,0.788235294117647,1.0,"CarnationPink");
		public static readonly Color Carnelian = new Color(0.701960784313725,0.105882352941176,0.105882352941176,1.0,"Carnelian");
		public static readonly Color CarolinaBlue = new Color(0.6,0.729411764705882,0.866666666666667,1.0,"CarolinaBlue");
		public static readonly Color CarrotOrange = new Color(0.929411764705882,0.568627450980392,0.129411764705882,1.0,"CarrotOrange");
		public static readonly Color CatalinaBlue = new Color(0.0235294117647059,0.164705882352941,0.470588235294118,1.0,"CatalinaBlue");
		public static readonly Color Ceil = new Color(0.572549019607843,0.631372549019608,0.811764705882353,1.0,"Ceil");
		public static readonly Color Celadon = new Color(0.674509803921569,0.882352941176471,0.686274509803922,1.0,"Celadon");
		public static readonly Color CeladonBlue = new Color(0,0.482352941176471,0.654901960784314,1.0,"CeladonBlue");
		public static readonly Color CeladonGreen = new Color(0.184313725490196,0.517647058823529,0.486274509803922,1.0,"CeladonGreen");
		public static readonly Color CelesteColour = new Color(0.698039215686274,1,1,1.0,"CelesteColour");
		public static readonly Color CelestialBlue = new Color(0.286274509803922,0.592156862745098,0.815686274509804,1.0,"CelestialBlue");
		public static readonly Color Cerise = new Color(0.870588235294118,0.192156862745098,0.388235294117647,1.0,"Cerise");
		public static readonly Color CerisePink = new Color(0.925490196078431,0.231372549019608,0.513725490196078,1.0,"CerisePink");
		public static readonly Color Cerulean = new Color(0,0.482352941176471,0.654901960784314,1.0,"Cerulean");
		public static readonly Color CeruleanBlue = new Color(0.164705882352941,0.32156862745098,0.745098039215686,1.0,"CeruleanBlue");
		public static readonly Color CeruleanFrost = new Color(0.427450980392157,0.607843137254902,0.764705882352941,1.0,"CeruleanFrost");
		public static readonly Color CgBlue = new Color(0,0.47843137254902,0.647058823529412,1.0,"CgBlue");
		public static readonly Color CgRed = new Color(0.87843137254902,0.235294117647059,0.192156862745098,1.0,"CgRed");
		public static readonly Color Chamoisee = new Color(0.627450980392157,0.470588235294118,0.352941176470588,1.0,"Chamoisee");
		public static readonly Color Champagne = new Color(0.980392156862745,0.83921568627451,0.647058823529412,1.0,"Champagne");
		public static readonly Color Charcoal = new Color(0.211764705882353,0.270588235294118,0.309803921568627,1.0,"Charcoal");
		public static readonly Color CharmPink = new Color(0.901960784313726,0.56078431372549,0.674509803921569,1.0,"CharmPink");
		public static readonly Color ChartreuseTraditional = new Color(0.874509803921569,1,0,1.0,"ChartreuseTraditional");
		public static readonly Color ChartreuseWeb = new Color(0.498039215686275,1,0,1.0,"ChartreuseWeb");
		public static readonly Color Cherry = new Color(0.870588235294118,0.192156862745098,0.388235294117647,1.0,"Cherry");
		public static readonly Color CherryBlossomPink = new Color(1,0.717647058823529,0.772549019607843,1.0,"CherryBlossomPink");
		public static readonly Color Chestnut = new Color(0.803921568627451,0.36078431372549,0.36078431372549,1.0,"Chestnut");
		public static readonly Color ChinaPink = new Color(0.870588235294118,0.435294117647059,0.631372549019608,1.0,"ChinaPink");
		public static readonly Color ChinaRose = new Color(0.658823529411765,0.317647058823529,0.431372549019608,1.0,"ChinaRose");
		public static readonly Color ChineseRed = new Color(0.666666666666667,0.219607843137255,0.117647058823529,1.0,"ChineseRed");
		public static readonly Color ChocolateTraditional = new Color(0.482352941176471,0.247058823529412,0,1.0,"ChocolateTraditional");
		public static readonly Color ChocolateWeb = new Color(0.823529411764706,0.411764705882353,0.117647058823529,1.0,"ChocolateWeb");
		public static readonly Color ChromeYellow = new Color(1,0.654901960784314,0,1.0,"ChromeYellow");
		public static readonly Color Cinereous = new Color(0.596078431372549,0.505882352941176,0.482352941176471,1.0,"Cinereous");
		public static readonly Color Cinnabar = new Color(0.890196078431373,0.258823529411765,0.203921568627451,1.0,"Cinnabar");
		public static readonly Color Cinnamon = new Color(0.823529411764706,0.411764705882353,0.117647058823529,1.0,"Cinnamon");
		public static readonly Color Citrine = new Color(0.894117647058824,0.815686274509804,0.0392156862745098,1.0,"Citrine");
		public static readonly Color ClassicRose = new Color(0.984313725490196,0.8,0.905882352941176,1.0,"ClassicRose");
		public static readonly Color Cobalt = new Color(0,0.27843137254902,0.670588235294118,1.0,"Cobalt");
		public static readonly Color CocoaBrown = new Color(0.823529411764706,0.411764705882353,0.117647058823529,1.0,"CocoaBrown");
		public static readonly Color Coffee = new Color(0.435294117647059,0.305882352941176,0.215686274509804,1.0,"Coffee");
		public static readonly Color ColumbiaBlue = new Color(0.607843137254902,0.866666666666667,1,1.0,"ColumbiaBlue");
		public static readonly Color CongoPink = new Color(0.972549019607843,0.513725490196078,0.474509803921569,1.0,"CongoPink");
		public static readonly Color CoolBlack = new Color(0,0.180392156862745,0.388235294117647,1.0,"CoolBlack");
		public static readonly Color CoolGrey = new Color(0.549019607843137,0.572549019607843,0.674509803921569,1.0,"CoolGrey");
		public static readonly Color Copper = new Color(0.72156862745098,0.450980392156863,0.2,1.0,"Copper");
		public static readonly Color CopperCrayola = new Color(0.854901960784314,0.541176470588235,0.403921568627451,1.0,"CopperCrayola");
		public static readonly Color CopperPenny = new Color(0.67843137254902,0.435294117647059,0.411764705882353,1.0,"CopperPenny");
		public static readonly Color CopperRed = new Color(0.796078431372549,0.427450980392157,0.317647058823529,1.0,"CopperRed");
		public static readonly Color CopperRose = new Color(0.6,0.4,0.4,1.0,"CopperRose");
		public static readonly Color Coquelicot = new Color(1,0.219607843137255,0,1.0,"Coquelicot");
		public static readonly Color Coral = new Color(1,0.498039215686275,0.313725490196078,1.0,"Coral");
		public static readonly Color CoralPink = new Color(0.972549019607843,0.513725490196078,0.474509803921569,1.0,"CoralPink");
		public static readonly Color CoralRed = new Color(1,0.250980392156863,0.250980392156863,1.0,"CoralRed");
		public static readonly Color Cordovan = new Color(0.537254901960784,0.247058823529412,0.270588235294118,1.0,"Cordovan");
		public static readonly Color Corn = new Color(0.984313725490196,0.925490196078431,0.364705882352941,1.0,"Corn");
		public static readonly Color CornellRed = new Color(0.701960784313725,0.105882352941176,0.105882352941176,1.0,"CornellRed");
		public static readonly Color CornflowerBlue = new Color(0.392156862745098,0.584313725490196,0.929411764705882,1.0,"CornflowerBlue");
		public static readonly Color Cornsilk = new Color(1,0.972549019607843,0.862745098039216,1.0,"Cornsilk");
		public static readonly Color CosmicLatte = new Color(1,0.972549019607843,0.905882352941176,1.0,"CosmicLatte");
		public static readonly Color CottonCandy = new Color(1,0.737254901960784,0.850980392156863,1.0,"CottonCandy");
		public static readonly Color Cream = new Color(1,0.992156862745098,0.815686274509804,1.0,"Cream");
		public static readonly Color Crimson = new Color(0.862745098039216,0.0784313725490196,0.235294117647059,1.0,"Crimson");
		public static readonly Color CrimsonGlory = new Color(0.745098039215686,0,0.196078431372549,1.0,"CrimsonGlory");
		public static readonly Color Cyan = new Color(0,1,1,1.0,"Cyan");
		public static readonly Color CyanProcess = new Color(0,0.717647058823529,0.92156862745098,1.0,"CyanProcess");
		public static readonly Color Daffodil = new Color(1,1,0.192156862745098,1.0,"Daffodil");
		public static readonly Color Dandelion = new Color(0.941176470588235,0.882352941176471,0.188235294117647,1.0,"Dandelion");
		public static readonly Color DarkBlue = new Color(0,0,0.545098039215686,1.0,"DarkBlue");
		public static readonly Color DarkBrown = new Color(0.396078431372549,0.262745098039216,0.129411764705882,1.0,"DarkBrown");
		public static readonly Color DarkByzantium = new Color(0.364705882352941,0.223529411764706,0.329411764705882,1.0,"DarkByzantium");
		public static readonly Color DarkCandyAppleRed = new Color(0.643137254901961,0,0,1.0,"DarkCandyAppleRed");
		public static readonly Color DarkCerulean = new Color(0.0313725490196078,0.270588235294118,0.494117647058824,1.0,"DarkCerulean");
		public static readonly Color DarkChestnut = new Color(0.596078431372549,0.411764705882353,0.376470588235294,1.0,"DarkChestnut");
		public static readonly Color DarkCoral = new Color(0.803921568627451,0.356862745098039,0.270588235294118,1.0,"DarkCoral");
		public static readonly Color DarkCyan = new Color(0,0.545098039215686,0.545098039215686,1.0,"DarkCyan");
		public static readonly Color DarkElectricBlue = new Color(0.325490196078431,0.407843137254902,0.470588235294118,1.0,"DarkElectricBlue");
		public static readonly Color DarkGoldenrod = new Color(0.72156862745098,0.525490196078431,0.0431372549019608,1.0,"DarkGoldenrod");
		public static readonly Color DarkGray = new Color(0.662745098039216,0.662745098039216,0.662745098039216,1.0,"DarkGray");
		public static readonly Color DarkGreen = new Color(0.00392156862745098,0.196078431372549,0.125490196078431,1.0,"DarkGreen");
		public static readonly Color DarkImperialBlue = new Color(0,0.254901960784314,0.415686274509804,1.0,"DarkImperialBlue");
		public static readonly Color DarkJungleGreen = new Color(0.101960784313725,0.141176470588235,0.129411764705882,1.0,"DarkJungleGreen");
		public static readonly Color DarkKhaki = new Color(0.741176470588235,0.717647058823529,0.419607843137255,1.0,"DarkKhaki");
		public static readonly Color DarkLava = new Color(0.282352941176471,0.235294117647059,0.196078431372549,1.0,"DarkLava");
		public static readonly Color DarkLavender = new Color(0.450980392156863,0.309803921568627,0.588235294117647,1.0,"DarkLavender");
		public static readonly Color DarkMagenta = new Color(0.545098039215686,0,0.545098039215686,1.0,"DarkMagenta");
		public static readonly Color DarkMidnightBlue = new Color(0,0.2,0.4,1.0,"DarkMidnightBlue");
		public static readonly Color DarkOliveGreen = new Color(0.333333333333333,0.419607843137255,0.184313725490196,1.0,"DarkOliveGreen");
		public static readonly Color DarkOrange = new Color(1,0.549019607843137,0,1.0,"DarkOrange");
		public static readonly Color DarkOrchid = new Color(0.6,0.196078431372549,0.8,1.0,"DarkOrchid");
		public static readonly Color DarkPastelBlue = new Color(0.466666666666667,0.619607843137255,0.796078431372549,1.0,"DarkPastelBlue");
		public static readonly Color DarkPastelGreen = new Color(0.0117647058823529,0.752941176470588,0.235294117647059,1.0,"DarkPastelGreen");
		public static readonly Color DarkPastelPurple = new Color(0.588235294117647,0.435294117647059,0.83921568627451,1.0,"DarkPastelPurple");
		public static readonly Color DarkPastelRed = new Color(0.76078431372549,0.231372549019608,0.133333333333333,1.0,"DarkPastelRed");
		public static readonly Color DarkPink = new Color(0.905882352941176,0.329411764705882,0.501960784313725,1.0,"DarkPink");
		public static readonly Color DarkPowderBlue = new Color(0,0.2,0.6,1.0,"DarkPowderBlue");
		public static readonly Color DarkRaspberry = new Color(0.529411764705882,0.149019607843137,0.341176470588235,1.0,"DarkRaspberry");
		public static readonly Color DarkRed = new Color(0.545098039215686,0,0,1.0,"DarkRed");
		public static readonly Color DarkSalmon = new Color(0.913725490196078,0.588235294117647,0.47843137254902,1.0,"DarkSalmon");
		public static readonly Color DarkScarlet = new Color(0.337254901960784,0.0117647058823529,0.0980392156862745,1.0,"DarkScarlet");
		public static readonly Color DarkSeaGreen = new Color(0.56078431372549,0.737254901960784,0.56078431372549,1.0,"DarkSeaGreen");
		public static readonly Color DarkSienna = new Color(0.235294117647059,0.0784313725490196,0.0784313725490196,1.0,"DarkSienna");
		public static readonly Color DarkSlateBlue = new Color(0.282352941176471,0.23921568627451,0.545098039215686,1.0,"DarkSlateBlue");
		public static readonly Color DarkSlateGray = new Color(0.184313725490196,0.309803921568627,0.309803921568627,1.0,"DarkSlateGray");
		public static readonly Color DarkSpringGreen = new Color(0.0901960784313725,0.447058823529412,0.270588235294118,1.0,"DarkSpringGreen");
		public static readonly Color DarkTan = new Color(0.568627450980392,0.505882352941176,0.317647058823529,1.0,"DarkTan");
		public static readonly Color DarkTangerine = new Color(1,0.658823529411765,0.0705882352941176,1.0,"DarkTangerine");
		public static readonly Color DarkTaupe = new Color(0.282352941176471,0.235294117647059,0.196078431372549,1.0,"DarkTaupe");
		public static readonly Color DarkTerraCotta = new Color(0.8,0.305882352941176,0.36078431372549,1.0,"DarkTerraCotta");
		public static readonly Color DarkTurquoise = new Color(0,0.807843137254902,0.819607843137255,1.0,"DarkTurquoise");
		public static readonly Color DarkViolet = new Color(0.580392156862745,0,0.827450980392157,1.0,"DarkViolet");
		public static readonly Color DarkYellow = new Color(0.607843137254902,0.529411764705882,0.0470588235294118,1.0,"DarkYellow");
		public static readonly Color DartmouthGreen = new Color(0,0.43921568627451,0.235294117647059,1.0,"DartmouthGreen");
		public static readonly Color DavySGrey = new Color(0.333333333333333,0.333333333333333,0.333333333333333,1.0,"DavySGrey");
		public static readonly Color DebianRed = new Color(0.843137254901961,0.0392156862745098,0.325490196078431,1.0,"DebianRed");
		public static readonly Color DeepCarmine = new Color(0.662745098039216,0.125490196078431,0.243137254901961,1.0,"DeepCarmine");
		public static readonly Color DeepCarminePink = new Color(0.937254901960784,0.188235294117647,0.219607843137255,1.0,"DeepCarminePink");
		public static readonly Color DeepCarrotOrange = new Color(0.913725490196078,0.411764705882353,0.172549019607843,1.0,"DeepCarrotOrange");
		public static readonly Color DeepCerise = new Color(0.854901960784314,0.196078431372549,0.529411764705882,1.0,"DeepCerise");
		public static readonly Color DeepChampagne = new Color(0.980392156862745,0.83921568627451,0.647058823529412,1.0,"DeepChampagne");
		public static readonly Color DeepChestnut = new Color(0.725490196078431,0.305882352941176,0.282352941176471,1.0,"DeepChestnut");
		public static readonly Color DeepCoffee = new Color(0.43921568627451,0.258823529411765,0.254901960784314,1.0,"DeepCoffee");
		public static readonly Color DeepFuchsia = new Color(0.756862745098039,0.329411764705882,0.756862745098039,1.0,"DeepFuchsia");
		public static readonly Color DeepJungleGreen = new Color(0,0.294117647058824,0.286274509803922,1.0,"DeepJungleGreen");
		public static readonly Color DeepLilac = new Color(0.6,0.333333333333333,0.733333333333333,1.0,"DeepLilac");
		public static readonly Color DeepMagenta = new Color(0.8,0,0.8,1.0,"DeepMagenta");
		public static readonly Color DeepPeach = new Color(1,0.796078431372549,0.643137254901961,1.0,"DeepPeach");
		public static readonly Color DeepPink = new Color(1,0.0784313725490196,0.576470588235294,1.0,"DeepPink");
		public static readonly Color DeepRuby = new Color(0.517647058823529,0.247058823529412,0.356862745098039,1.0,"DeepRuby");
		public static readonly Color DeepSaffron = new Color(1,0.6,0.2,1.0,"DeepSaffron");
		public static readonly Color DeepSkyBlue = new Color(0,0.749019607843137,1,1.0,"DeepSkyBlue");
		public static readonly Color DeepTuscanRed = new Color(0.4,0.258823529411765,0.301960784313725,1.0,"DeepTuscanRed");
		public static readonly Color Denim = new Color(0.0823529411764706,0.376470588235294,0.741176470588235,1.0,"Denim");
		public static readonly Color Desert = new Color(0.756862745098039,0.603921568627451,0.419607843137255,1.0,"Desert");
		public static readonly Color DesertSand = new Color(0.929411764705882,0.788235294117647,0.686274509803922,1.0,"DesertSand");
		public static readonly Color DimGray = new Color(0.411764705882353,0.411764705882353,0.411764705882353,1.0,"DimGray");
		public static readonly Color DodgerBlue = new Color(0.117647058823529,0.564705882352941,1,1.0,"DodgerBlue");
		public static readonly Color DogwoodRose = new Color(0.843137254901961,0.0941176470588235,0.407843137254902,1.0,"DogwoodRose");
		public static readonly Color DollarBill = new Color(0.52156862745098,0.733333333333333,0.396078431372549,1.0,"DollarBill");
		public static readonly Color Drab = new Color(0.588235294117647,0.443137254901961,0.0901960784313725,1.0,"Drab");
		public static readonly Color DukeBlue = new Color(0,0,0.611764705882353,1.0,"DukeBlue");
		public static readonly Color EarthYellow = new Color(0.882352941176471,0.662745098039216,0.372549019607843,1.0,"EarthYellow");
		public static readonly Color Ebony = new Color(0.333333333333333,0.364705882352941,0.313725490196078,1.0,"Ebony");
		public static readonly Color Ecru = new Color(0.76078431372549,0.698039215686274,0.501960784313725,1.0,"Ecru");
		public static readonly Color Eggplant = new Color(0.380392156862745,0.250980392156863,0.317647058823529,1.0,"Eggplant");
		public static readonly Color Eggshell = new Color(0.941176470588235,0.917647058823529,0.83921568627451,1.0,"Eggshell");
		public static readonly Color EgyptianBlue = new Color(0.0627450980392157,0.203921568627451,0.650980392156863,1.0,"EgyptianBlue");
		public static readonly Color ElectricBlue = new Color(0.490196078431373,0.976470588235294,1,1.0,"ElectricBlue");
		public static readonly Color ElectricCrimson = new Color(1,0,0.247058823529412,1.0,"ElectricCrimson");
		public static readonly Color ElectricCyan = new Color(0,1,1,1.0,"ElectricCyan");
		public static readonly Color ElectricGreen = new Color(0,1,0,1.0,"ElectricGreen");
		public static readonly Color ElectricIndigo = new Color(0.435294117647059,0,1,1.0,"ElectricIndigo");
		public static readonly Color ElectricLavender = new Color(0.956862745098039,0.733333333333333,1,1.0,"ElectricLavender");
		public static readonly Color ElectricLime = new Color(0.8,1,0,1.0,"ElectricLime");
		public static readonly Color ElectricPurple = new Color(0.749019607843137,0,1,1.0,"ElectricPurple");
		public static readonly Color ElectricUltramarine = new Color(0.247058823529412,0,1,1.0,"ElectricUltramarine");
		public static readonly Color ElectricViolet = new Color(0.56078431372549,0,1,1.0,"ElectricViolet");
		public static readonly Color ElectricYellow = new Color(1,1,0,1.0,"ElectricYellow");
		public static readonly Color Emerald = new Color(0.313725490196078,0.784313725490196,0.470588235294118,1.0,"Emerald");
		public static readonly Color EnglishLavender = new Color(0.705882352941177,0.513725490196078,0.584313725490196,1.0,"EnglishLavender");
		public static readonly Color EtonBlue = new Color(0.588235294117647,0.784313725490196,0.635294117647059,1.0,"EtonBlue");
		public static readonly Color Fallow = new Color(0.756862745098039,0.603921568627451,0.419607843137255,1.0,"Fallow");
		public static readonly Color FaluRed = new Color(0.501960784313725,0.0941176470588235,0.0941176470588235,1.0,"FaluRed");
		public static readonly Color Fandango = new Color(0.709803921568627,0.2,0.537254901960784,1.0,"Fandango");
		public static readonly Color FashionFuchsia = new Color(0.956862745098039,0,0.631372549019608,1.0,"FashionFuchsia");
		public static readonly Color Fawn = new Color(0.898039215686275,0.666666666666667,0.43921568627451,1.0,"Fawn");
		public static readonly Color Feldgrau = new Color(0.301960784313725,0.364705882352941,0.325490196078431,1.0,"Feldgrau");
		public static readonly Color FernGreen = new Color(0.309803921568627,0.474509803921569,0.258823529411765,1.0,"FernGreen");
		public static readonly Color FerrariRed = new Color(1,0.156862745098039,0,1.0,"FerrariRed");
		public static readonly Color FieldDrab = new Color(0.423529411764706,0.329411764705882,0.117647058823529,1.0,"FieldDrab");
		public static readonly Color FireEngineRed = new Color(0.807843137254902,0.125490196078431,0.16078431372549,1.0,"FireEngineRed");
		public static readonly Color Firebrick = new Color(0.698039215686274,0.133333333333333,0.133333333333333,1.0,"Firebrick");
		public static readonly Color Flame = new Color(0.886274509803922,0.345098039215686,0.133333333333333,1.0,"Flame");
		public static readonly Color FlamingoPink = new Color(0.988235294117647,0.556862745098039,0.674509803921569,1.0,"FlamingoPink");
		public static readonly Color Flavescent = new Color(0.968627450980392,0.913725490196078,0.556862745098039,1.0,"Flavescent");
		public static readonly Color Flax = new Color(0.933333333333333,0.862745098039216,0.509803921568627,1.0,"Flax");
		public static readonly Color FloralWhite = new Color(1,0.980392156862745,0.941176470588235,1.0,"FloralWhite");
		public static readonly Color FluorescentOrange = new Color(1,0.749019607843137,0,1.0,"FluorescentOrange");
		public static readonly Color FluorescentPink = new Color(1,0.0784313725490196,0.576470588235294,1.0,"FluorescentPink");
		public static readonly Color FluorescentYellow = new Color(0.8,1,0,1.0,"FluorescentYellow");
		public static readonly Color Folly = new Color(1,0,0.309803921568627,1.0,"Folly");
		public static readonly Color ForestGreenTraditional = new Color(0.00392156862745098,0.266666666666667,0.129411764705882,1.0,"ForestGreenTraditional");
		public static readonly Color ForestGreenWeb = new Color(0.133333333333333,0.545098039215686,0.133333333333333,1.0,"ForestGreenWeb");
		public static readonly Color FrenchBeige = new Color(0.650980392156863,0.482352941176471,0.356862745098039,1.0,"FrenchBeige");
		public static readonly Color FrenchBlue = new Color(0,0.447058823529412,0.733333333333333,1.0,"FrenchBlue");
		public static readonly Color FrenchLilac = new Color(0.525490196078431,0.376470588235294,0.556862745098039,1.0,"FrenchLilac");
		public static readonly Color FrenchLime = new Color(0.8,1,0,1.0,"FrenchLime");
		public static readonly Color FrenchRaspberry = new Color(0.780392156862745,0.172549019607843,0.282352941176471,1.0,"FrenchRaspberry");
		public static readonly Color FrenchRose = new Color(0.964705882352941,0.290196078431373,0.541176470588235,1.0,"FrenchRose");
		public static readonly Color Fuchsia = new Color(1,0,1,1.0,"Fuchsia");
		public static readonly Color FuchsiaCrayola = new Color(0.756862745098039,0.329411764705882,0.756862745098039,1.0,"FuchsiaCrayola");
		public static readonly Color FuchsiaPink = new Color(1,0.466666666666667,1,1.0,"FuchsiaPink");
		public static readonly Color FuchsiaRose = new Color(0.780392156862745,0.262745098039216,0.458823529411765,1.0,"FuchsiaRose");
		public static readonly Color Fulvous = new Color(0.894117647058824,0.517647058823529,0,1.0,"Fulvous");
		public static readonly Color FuzzyWuzzy = new Color(0.8,0.4,0.4,1.0,"FuzzyWuzzy");
		public static readonly Color Gainsboro = new Color(0.862745098039216,0.862745098039216,0.862745098039216,1.0,"Gainsboro");
		public static readonly Color Gamboge = new Color(0.894117647058824,0.607843137254902,0.0588235294117647,1.0,"Gamboge");
		public static readonly Color GhostWhite = new Color(0.972549019607843,0.972549019607843,1,1.0,"GhostWhite");
		public static readonly Color Ginger = new Color(0.690196078431373,0.396078431372549,0,1.0,"Ginger");
		public static readonly Color Glaucous = new Color(0.376470588235294,0.509803921568627,0.713725490196078,1.0,"Glaucous");
		public static readonly Color Glitter = new Color(0.901960784313726,0.909803921568627,0.980392156862745,1.0,"Glitter");
		public static readonly Color GoldMetallic = new Color(0.831372549019608,0.686274509803922,0.215686274509804,1.0,"GoldMetallic");
		public static readonly Color GoldWebGolden = new Color(1,0.843137254901961,0,1.0,"GoldWebGolden");
		public static readonly Color GoldenBrown = new Color(0.6,0.396078431372549,0.0823529411764706,1.0,"GoldenBrown");
		public static readonly Color GoldenPoppy = new Color(0.988235294117647,0.76078431372549,0,1.0,"GoldenPoppy");
		public static readonly Color GoldenYellow = new Color(1,0.874509803921569,0,1.0,"GoldenYellow");
		public static readonly Color Goldenrod = new Color(0.854901960784314,0.647058823529412,0.125490196078431,1.0,"Goldenrod");
		public static readonly Color GrannySmithApple = new Color(0.658823529411765,0.894117647058824,0.627450980392157,1.0,"GrannySmithApple");
		public static readonly Color Gray = new Color(0.501960784313725,0.501960784313725,0.501960784313725,1.0,"Gray");
		public static readonly Color GrayAsparagus = new Color(0.274509803921569,0.349019607843137,0.270588235294118,1.0,"GrayAsparagus");
		public static readonly Color GrayHtmlCssGray = new Color(0.501960784313725,0.501960784313725,0.501960784313725,1.0,"GrayHtmlCssGray");
		public static readonly Color GrayX11Gray = new Color(0.745098039215686,0.745098039215686,0.745098039215686,1.0,"GrayX11Gray");
		public static readonly Color GreenColorWheelX11Green = new Color(0,1,0,1.0,"GreenColorWheelX11Green");
		public static readonly Color GreenCrayola = new Color(0.109803921568627,0.674509803921569,0.470588235294118,1.0,"GreenCrayola");
		public static readonly Color GreenHtmlCssGreen = new Color(0,0.501960784313725,0,1.0,"GreenHtmlCssGreen");
		public static readonly Color GreenMunsell = new Color(0,0.658823529411765,0.466666666666667,1.0,"GreenMunsell");
		public static readonly Color GreenNcs = new Color(0,0.623529411764706,0.419607843137255,1.0,"GreenNcs");
		public static readonly Color GreenPigment = new Color(0,0.647058823529412,0.313725490196078,1.0,"GreenPigment");
		public static readonly Color GreenRyb = new Color(0.4,0.690196078431373,0.196078431372549,1.0,"GreenRyb");
		public static readonly Color GreenYellow = new Color(0.67843137254902,1,0.184313725490196,1.0,"GreenYellow");
		public static readonly Color Grullo = new Color(0.662745098039216,0.603921568627451,0.525490196078431,1.0,"Grullo");
		public static readonly Color GuppieGreen = new Color(0,1,0.498039215686275,1.0,"GuppieGreen");
		public static readonly Color HalayBe = new Color(0.4,0.219607843137255,0.329411764705882,1.0,"HalayBe");
		public static readonly Color HanBlue = new Color(0.266666666666667,0.423529411764706,0.811764705882353,1.0,"HanBlue");
		public static readonly Color HanPurple = new Color(0.32156862745098,0.0941176470588235,0.980392156862745,1.0,"HanPurple");
		public static readonly Color HansaYellow = new Color(0.913725490196078,0.83921568627451,0.419607843137255,1.0,"HansaYellow");
		public static readonly Color Harlequin = new Color(0.247058823529412,1,0,1.0,"Harlequin");
		public static readonly Color HarvardCrimson = new Color(0.788235294117647,0,0.0862745098039216,1.0,"HarvardCrimson");
		public static readonly Color HarvestGold = new Color(0.854901960784314,0.568627450980392,0,1.0,"HarvestGold");
		public static readonly Color HeartGold = new Color(0.501960784313725,0.501960784313725,0,1.0,"HeartGold");
		public static readonly Color Heliotrope = new Color(0.874509803921569,0.450980392156863,1,1.0,"Heliotrope");
		public static readonly Color HollywoodCerise = new Color(0.956862745098039,0,0.631372549019608,1.0,"HollywoodCerise");
		public static readonly Color Honeydew = new Color(0.941176470588235,1,0.941176470588235,1.0,"Honeydew");
		public static readonly Color HonoluluBlue = new Color(0,0.498039215686275,0.749019607843137,1.0,"HonoluluBlue");
		public static readonly Color HookerSGreen = new Color(0.286274509803922,0.474509803921569,0.419607843137255,1.0,"HookerSGreen");
		public static readonly Color HotMagenta = new Color(1,0.113725490196078,0.807843137254902,1.0,"HotMagenta");
		public static readonly Color HotPink = new Color(1,0.411764705882353,0.705882352941177,1.0,"HotPink");
		public static readonly Color HunterGreen = new Color(0.207843137254902,0.368627450980392,0.231372549019608,1.0,"HunterGreen");
		public static readonly Color Iceberg = new Color(0.443137254901961,0.650980392156863,0.823529411764706,1.0,"Iceberg");
		public static readonly Color Icterine = new Color(0.988235294117647,0.968627450980392,0.368627450980392,1.0,"Icterine");
		public static readonly Color ImperialBlue = new Color(0,0.137254901960784,0.584313725490196,1.0,"ImperialBlue");
		public static readonly Color Inchworm = new Color(0.698039215686274,0.925490196078431,0.364705882352941,1.0,"Inchworm");
		public static readonly Color IndiaGreen = new Color(0.0745098039215686,0.533333333333333,0.0313725490196078,1.0,"IndiaGreen");
		public static readonly Color IndianRed = new Color(0.803921568627451,0.36078431372549,0.36078431372549,1.0,"IndianRed");
		public static readonly Color IndianYellow = new Color(0.890196078431373,0.658823529411765,0.341176470588235,1.0,"IndianYellow");
		public static readonly Color Indigo = new Color(0.435294117647059,0,1,1.0,"Indigo");
		public static readonly Color IndigoDye = new Color(0,0.254901960784314,0.415686274509804,1.0,"IndigoDye");
		public static readonly Color IndigoWeb = new Color(0.294117647058824,0,0.509803921568627,1.0,"IndigoWeb");
		public static readonly Color InternationalKleinBlue = new Color(0,0.184313725490196,0.654901960784314,1.0,"InternationalKleinBlue");
		public static readonly Color InternationalOrangeAerospace = new Color(1,0.309803921568627,0,1.0,"InternationalOrangeAerospace");
		public static readonly Color InternationalOrangeEngineering = new Color(0.729411764705882,0.0862745098039216,0.0470588235294118,1.0,"InternationalOrangeEngineering");
		public static readonly Color InternationalOrangeGoldenGateBridge = new Color(0.752941176470588,0.211764705882353,0.172549019607843,1.0,"InternationalOrangeGoldenGateBridge");
		public static readonly Color Iris = new Color(0.352941176470588,0.309803921568627,0.811764705882353,1.0,"Iris");
		public static readonly Color Isabelline = new Color(0.956862745098039,0.941176470588235,0.925490196078431,1.0,"Isabelline");
		public static readonly Color IslamicGreen = new Color(0,0.564705882352941,0,1.0,"IslamicGreen");
		public static readonly Color Ivory = new Color(1,1,0.941176470588235,1.0,"Ivory");
		public static readonly Color Jade = new Color(0,0.658823529411765,0.419607843137255,1.0,"Jade");
		public static readonly Color Jasmine = new Color(0.972549019607843,0.870588235294118,0.494117647058824,1.0,"Jasmine");
		public static readonly Color Jasper = new Color(0.843137254901961,0.231372549019608,0.243137254901961,1.0,"Jasper");
		public static readonly Color JazzberryJam = new Color(0.647058823529412,0.0431372549019608,0.368627450980392,1.0,"JazzberryJam");
		public static readonly Color Jet = new Color(0.203921568627451,0.203921568627451,0.203921568627451,1.0,"Jet");
		public static readonly Color Jonquil = new Color(0.980392156862745,0.854901960784314,0.368627450980392,1.0,"Jonquil");
		public static readonly Color JuneBud = new Color(0.741176470588235,0.854901960784314,0.341176470588235,1.0,"JuneBud");
		public static readonly Color JungleGreen = new Color(0.16078431372549,0.670588235294118,0.529411764705882,1.0,"JungleGreen");
		public static readonly Color KellyGreen = new Color(0.298039215686275,0.733333333333333,0.0901960784313725,1.0,"KellyGreen");
		public static readonly Color KenyanCopper = new Color(0.486274509803922,0.109803921568627,0.0196078431372549,1.0,"KenyanCopper");
		public static readonly Color KhakiHtmlCssKhaki = new Color(0.764705882352941,0.690196078431373,0.568627450980392,1.0,"KhakiHtmlCssKhaki");
		public static readonly Color KhakiX11LightKhaki = new Color(0.941176470588235,0.901960784313726,0.549019607843137,1.0,"KhakiX11LightKhaki");
		public static readonly Color KuCrimson = new Color(0.909803921568627,0,0.0509803921568627,1.0,"KuCrimson");
		public static readonly Color LaSalleGreen = new Color(0.0313725490196078,0.470588235294118,0.188235294117647,1.0,"LaSalleGreen");
		public static readonly Color LanguidLavender = new Color(0.83921568627451,0.792156862745098,0.866666666666667,1.0,"LanguidLavender");
		public static readonly Color LapisLazuli = new Color(0.149019607843137,0.380392156862745,0.611764705882353,1.0,"LapisLazuli");
		public static readonly Color LaserLemon = new Color(0.996078431372549,0.996078431372549,0.133333333333333,1.0,"LaserLemon");
		public static readonly Color LaurelGreen = new Color(0.662745098039216,0.729411764705882,0.615686274509804,1.0,"LaurelGreen");
		public static readonly Color Lava = new Color(0.811764705882353,0.0627450980392157,0.125490196078431,1.0,"Lava");
		public static readonly Color LavenderBlue = new Color(0.8,0.8,1,1.0,"LavenderBlue");
		public static readonly Color LavenderBlush = new Color(1,0.941176470588235,0.96078431372549,1.0,"LavenderBlush");
		public static readonly Color LavenderFloral = new Color(0.709803921568627,0.494117647058824,0.862745098039216,1.0,"LavenderFloral");
		public static readonly Color LavenderGray = new Color(0.768627450980392,0.764705882352941,0.815686274509804,1.0,"LavenderGray");
		public static readonly Color LavenderIndigo = new Color(0.580392156862745,0.341176470588235,0.92156862745098,1.0,"LavenderIndigo");
		public static readonly Color LavenderMagenta = new Color(0.933333333333333,0.509803921568627,0.933333333333333,1.0,"LavenderMagenta");
		public static readonly Color LavenderMist = new Color(0.901960784313726,0.901960784313726,0.980392156862745,1.0,"LavenderMist");
		public static readonly Color LavenderPink = new Color(0.984313725490196,0.682352941176471,0.823529411764706,1.0,"LavenderPink");
		public static readonly Color LavenderPurple = new Color(0.588235294117647,0.482352941176471,0.713725490196078,1.0,"LavenderPurple");
		public static readonly Color LavenderRose = new Color(0.984313725490196,0.627450980392157,0.890196078431373,1.0,"LavenderRose");
		public static readonly Color LavenderWeb = new Color(0.901960784313726,0.901960784313726,0.980392156862745,1.0,"LavenderWeb");
		public static readonly Color LawnGreen = new Color(0.486274509803922,0.988235294117647,0,1.0,"LawnGreen");
		public static readonly Color Lemon = new Color(1,0.968627450980392,0,1.0,"Lemon");
		public static readonly Color LemonChiffon = new Color(1,0.980392156862745,0.803921568627451,1.0,"LemonChiffon");
		public static readonly Color LemonLime = new Color(0.890196078431373,1,0,1.0,"LemonLime");
		public static readonly Color Licorice = new Color(0.101960784313725,0.0666666666666667,0.0627450980392157,1.0,"Licorice");
		public static readonly Color LightApricot = new Color(0.992156862745098,0.835294117647059,0.694117647058824,1.0,"LightApricot");
		public static readonly Color LightBlue = new Color(0.67843137254902,0.847058823529412,0.901960784313726,1.0,"LightBlue");
		public static readonly Color LightBrown = new Color(0.709803921568627,0.396078431372549,0.113725490196078,1.0,"LightBrown");
		public static readonly Color LightCarminePink = new Color(0.901960784313726,0.403921568627451,0.443137254901961,1.0,"LightCarminePink");
		public static readonly Color LightCoral = new Color(0.941176470588235,0.501960784313725,0.501960784313725,1.0,"LightCoral");
		public static readonly Color LightCornflowerBlue = new Color(0.576470588235294,0.8,0.917647058823529,1.0,"LightCornflowerBlue");
		public static readonly Color LightCrimson = new Color(0.96078431372549,0.411764705882353,0.568627450980392,1.0,"LightCrimson");
		public static readonly Color LightCyan = new Color(0.87843137254902,1,1,1.0,"LightCyan");
		public static readonly Color LightFuchsiaPink = new Color(0.976470588235294,0.517647058823529,0.937254901960784,1.0,"LightFuchsiaPink");
		public static readonly Color LightGoldenrodYellow = new Color(0.980392156862745,0.980392156862745,0.823529411764706,1.0,"LightGoldenrodYellow");
		public static readonly Color LightGray = new Color(0.827450980392157,0.827450980392157,0.827450980392157,1.0,"LightGray");
		public static readonly Color LightGreen = new Color(0.564705882352941,0.933333333333333,0.564705882352941,1.0,"LightGreen");
		public static readonly Color LightKhaki = new Color(0.941176470588235,0.901960784313726,0.549019607843137,1.0,"LightKhaki");
		public static readonly Color LightPastelPurple = new Color(0.694117647058824,0.611764705882353,0.850980392156863,1.0,"LightPastelPurple");
		public static readonly Color LightPink = new Color(1,0.713725490196078,0.756862745098039,1.0,"LightPink");
		public static readonly Color LightRedOchre = new Color(0.913725490196078,0.454901960784314,0.317647058823529,1.0,"LightRedOchre");
		public static readonly Color LightSalmon = new Color(1,0.627450980392157,0.47843137254902,1.0,"LightSalmon");
		public static readonly Color LightSalmonPink = new Color(1,0.6,0.6,1.0,"LightSalmonPink");
		public static readonly Color LightSeaGreen = new Color(0.125490196078431,0.698039215686274,0.666666666666667,1.0,"LightSeaGreen");
		public static readonly Color LightSkyBlue = new Color(0.529411764705882,0.807843137254902,0.980392156862745,1.0,"LightSkyBlue");
		public static readonly Color LightSlateGray = new Color(0.466666666666667,0.533333333333333,0.6,1.0,"LightSlateGray");
		public static readonly Color LightTaupe = new Color(0.701960784313725,0.545098039215686,0.427450980392157,1.0,"LightTaupe");
		public static readonly Color LightThulianPink = new Color(0.901960784313726,0.56078431372549,0.674509803921569,1.0,"LightThulianPink");
		public static readonly Color LightYellow = new Color(1,1,0.87843137254902,1.0,"LightYellow");
		public static readonly Color Lilac = new Color(0.784313725490196,0.635294117647059,0.784313725490196,1.0,"Lilac");
		public static readonly Color LimeColorWheel = new Color(0.749019607843137,1,0,1.0,"LimeColorWheel");
		public static readonly Color LimeGreen = new Color(0.196078431372549,0.803921568627451,0.196078431372549,1.0,"LimeGreen");
		public static readonly Color LimeWebX11Green = new Color(0,1,0,1.0,"LimeWebX11Green");
		public static readonly Color Limerick = new Color(0.615686274509804,0.76078431372549,0.0352941176470588,1.0,"Limerick");
		public static readonly Color LincolnGreen = new Color(0.0980392156862745,0.349019607843137,0.0196078431372549,1.0,"LincolnGreen");
		public static readonly Color Linen = new Color(0.980392156862745,0.941176470588235,0.901960784313726,1.0,"Linen");
		public static readonly Color Lion = new Color(0.756862745098039,0.603921568627451,0.419607843137255,1.0,"Lion");
		public static readonly Color LittleBoyBlue = new Color(0.423529411764706,0.627450980392157,0.862745098039216,1.0,"LittleBoyBlue");
		public static readonly Color Liver = new Color(0.325490196078431,0.294117647058824,0.309803921568627,1.0,"Liver");
		public static readonly Color Lust = new Color(0.901960784313726,0.125490196078431,0.125490196078431,1.0,"Lust");
		public static readonly Color Magenta = new Color(1,0,1,1.0,"Magenta");
		public static readonly Color MagentaDye = new Color(0.792156862745098,0.12156862745098,0.482352941176471,1.0,"MagentaDye");
		public static readonly Color MagentaProcess = new Color(1,0,0.564705882352941,1.0,"MagentaProcess");
		public static readonly Color MagicMint = new Color(0.666666666666667,0.941176470588235,0.819607843137255,1.0,"MagicMint");
		public static readonly Color Magnolia = new Color(0.972549019607843,0.956862745098039,1,1.0,"Magnolia");
		public static readonly Color Mahogany = new Color(0.752941176470588,0.250980392156863,0,1.0,"Mahogany");
		public static readonly Color Maize = new Color(0.984313725490196,0.925490196078431,0.364705882352941,1.0,"Maize");
		public static readonly Color MajorelleBlue = new Color(0.376470588235294,0.313725490196078,0.862745098039216,1.0,"MajorelleBlue");
		public static readonly Color Malachite = new Color(0.0431372549019608,0.854901960784314,0.317647058823529,1.0,"Malachite");
		public static readonly Color Manatee = new Color(0.592156862745098,0.603921568627451,0.666666666666667,1.0,"Manatee");
		public static readonly Color MangoTango = new Color(1,0.509803921568627,0.262745098039216,1.0,"MangoTango");
		public static readonly Color Mantis = new Color(0.454901960784314,0.764705882352941,0.396078431372549,1.0,"Mantis");
		public static readonly Color MardiGras = new Color(0.533333333333333,0,0.52156862745098,1.0,"MardiGras");
		public static readonly Color MaroonCrayola = new Color(0.764705882352941,0.129411764705882,0.282352941176471,1.0,"MaroonCrayola");
		public static readonly Color MaroonHtmlCss = new Color(0.501960784313725,0,0,1.0,"MaroonHtmlCss");
		public static readonly Color MaroonX11 = new Color(0.690196078431373,0.188235294117647,0.376470588235294,1.0,"MaroonX11");
		public static readonly Color Mauve = new Color(0.87843137254902,0.690196078431373,1,1.0,"Mauve");
		public static readonly Color MauveTaupe = new Color(0.568627450980392,0.372549019607843,0.427450980392157,1.0,"MauveTaupe");
		public static readonly Color Mauvelous = new Color(0.937254901960784,0.596078431372549,0.666666666666667,1.0,"Mauvelous");
		public static readonly Color MayaBlue = new Color(0.450980392156863,0.76078431372549,0.984313725490196,1.0,"MayaBlue");
		public static readonly Color MeatBrown = new Color(0.898039215686275,0.717647058823529,0.231372549019608,1.0,"MeatBrown");
		public static readonly Color MediumAquamarine = new Color(0.4,0.866666666666667,0.666666666666667,1.0,"MediumAquamarine");
		public static readonly Color MediumBlue = new Color(0,0,0.803921568627451,1.0,"MediumBlue");
		public static readonly Color MediumCandyAppleRed = new Color(0.886274509803922,0.0235294117647059,0.172549019607843,1.0,"MediumCandyAppleRed");
		public static readonly Color MediumCarmine = new Color(0.686274509803922,0.250980392156863,0.207843137254902,1.0,"MediumCarmine");
		public static readonly Color MediumChampagne = new Color(0.952941176470588,0.898039215686275,0.670588235294118,1.0,"MediumChampagne");
		public static readonly Color MediumElectricBlue = new Color(0.0117647058823529,0.313725490196078,0.588235294117647,1.0,"MediumElectricBlue");
		public static readonly Color MediumJungleGreen = new Color(0.109803921568627,0.207843137254902,0.176470588235294,1.0,"MediumJungleGreen");
		public static readonly Color MediumLavenderMagenta = new Color(0.866666666666667,0.627450980392157,0.866666666666667,1.0,"MediumLavenderMagenta");
		public static readonly Color MediumOrchid = new Color(0.729411764705882,0.333333333333333,0.827450980392157,1.0,"MediumOrchid");
		public static readonly Color MediumPersianBlue = new Color(0,0.403921568627451,0.647058823529412,1.0,"MediumPersianBlue");
		public static readonly Color MediumPurple = new Color(0.576470588235294,0.43921568627451,0.858823529411765,1.0,"MediumPurple");
		public static readonly Color MediumRedViolet = new Color(0.733333333333333,0.2,0.52156862745098,1.0,"MediumRedViolet");
		public static readonly Color MediumRuby = new Color(0.666666666666667,0.250980392156863,0.411764705882353,1.0,"MediumRuby");
		public static readonly Color MediumSeaGreen = new Color(0.235294117647059,0.701960784313725,0.443137254901961,1.0,"MediumSeaGreen");
		public static readonly Color MediumSlateBlue = new Color(0.482352941176471,0.407843137254902,0.933333333333333,1.0,"MediumSlateBlue");
		public static readonly Color MediumSpringBud = new Color(0.788235294117647,0.862745098039216,0.529411764705882,1.0,"MediumSpringBud");
		public static readonly Color MediumSpringGreen = new Color(0,0.980392156862745,0.603921568627451,1.0,"MediumSpringGreen");
		public static readonly Color MediumTaupe = new Color(0.403921568627451,0.298039215686275,0.27843137254902,1.0,"MediumTaupe");
		public static readonly Color MediumTurquoise = new Color(0.282352941176471,0.819607843137255,0.8,1.0,"MediumTurquoise");
		public static readonly Color MediumTuscanRed = new Color(0.474509803921569,0.266666666666667,0.231372549019608,1.0,"MediumTuscanRed");
		public static readonly Color MediumVermilion = new Color(0.850980392156863,0.376470588235294,0.231372549019608,1.0,"MediumVermilion");
		public static readonly Color MediumVioletRed = new Color(0.780392156862745,0.0823529411764706,0.52156862745098,1.0,"MediumVioletRed");
		public static readonly Color MellowApricot = new Color(0.972549019607843,0.72156862745098,0.470588235294118,1.0,"MellowApricot");
		public static readonly Color MellowYellow = new Color(0.972549019607843,0.870588235294118,0.494117647058824,1.0,"MellowYellow");
		public static readonly Color Melon = new Color(0.992156862745098,0.737254901960784,0.705882352941177,1.0,"Melon");
		public static readonly Color MidnightBlue = new Color(0.0980392156862745,0.0980392156862745,0.43921568627451,1.0,"MidnightBlue");
		public static readonly Color MidnightGreenEagleGreen = new Color(0,0.286274509803922,0.325490196078431,1.0,"MidnightGreenEagleGreen");
		public static readonly Color MikadoYellow = new Color(1,0.768627450980392,0.0470588235294118,1.0,"MikadoYellow");
		public static readonly Color Mint = new Color(0.243137254901961,0.705882352941177,0.537254901960784,1.0,"Mint");
		public static readonly Color MintCream = new Color(0.96078431372549,1,0.980392156862745,1.0,"MintCream");
		public static readonly Color MintGreen = new Color(0.596078431372549,1,0.596078431372549,1.0,"MintGreen");
		public static readonly Color MistyRose = new Color(1,0.894117647058824,0.882352941176471,1.0,"MistyRose");
		public static readonly Color Moccasin = new Color(0.980392156862745,0.92156862745098,0.843137254901961,1.0,"Moccasin");
		public static readonly Color ModeBeige = new Color(0.588235294117647,0.443137254901961,0.0901960784313725,1.0,"ModeBeige");
		public static readonly Color MoonstoneBlue = new Color(0.450980392156863,0.662745098039216,0.76078431372549,1.0,"MoonstoneBlue");
		public static readonly Color MordantRed19 = new Color(0.682352941176471,0.0470588235294118,0,1.0,"MordantRed19");
		public static readonly Color MossGreen = new Color(0.67843137254902,0.874509803921569,0.67843137254902,1.0,"MossGreen");
		public static readonly Color MountainMeadow = new Color(0.188235294117647,0.729411764705882,0.56078431372549,1.0,"MountainMeadow");
		public static readonly Color MountbattenPink = new Color(0.6,0.47843137254902,0.552941176470588,1.0,"MountbattenPink");
		public static readonly Color MsuGreen = new Color(0.0941176470588235,0.270588235294118,0.231372549019608,1.0,"MsuGreen");
		public static readonly Color Mulberry = new Color(0.772549019607843,0.294117647058824,0.549019607843137,1.0,"Mulberry");
		public static readonly Color Mustard = new Color(1,0.858823529411765,0.345098039215686,1.0,"Mustard");
		public static readonly Color Myrtle = new Color(0.129411764705882,0.258823529411765,0.117647058823529,1.0,"Myrtle");
		public static readonly Color NadeshikoPink = new Color(0.964705882352941,0.67843137254902,0.776470588235294,1.0,"NadeshikoPink");
		public static readonly Color NapierGreen = new Color(0.164705882352941,0.501960784313725,0,1.0,"NapierGreen");
		public static readonly Color NaplesYellow = new Color(0.980392156862745,0.854901960784314,0.368627450980392,1.0,"NaplesYellow");
		public static readonly Color NavajoWhite = new Color(1,0.870588235294118,0.67843137254902,1.0,"NavajoWhite");
		public static readonly Color NavyBlue = new Color(0,0,0.501960784313725,1.0,"NavyBlue");
		public static readonly Color NeonCarrot = new Color(1,0.63921568627451,0.262745098039216,1.0,"NeonCarrot");
		public static readonly Color NeonFuchsia = new Color(0.996078431372549,0.254901960784314,0.392156862745098,1.0,"NeonFuchsia");
		public static readonly Color NeonGreen = new Color(0.223529411764706,1,0.0784313725490196,1.0,"NeonGreen");
		public static readonly Color NewYorkPink = new Color(0.843137254901961,0.513725490196078,0.498039215686275,1.0,"NewYorkPink");
		public static readonly Color NonPhotoBlue = new Color(0.643137254901961,0.866666666666667,0.929411764705882,1.0,"NonPhotoBlue");
		public static readonly Color NorthTexasGreen = new Color(0.0196078431372549,0.564705882352941,0.2,1.0,"NorthTexasGreen");
		public static readonly Color OceanBoatBlue = new Color(0,0.466666666666667,0.745098039215686,1.0,"OceanBoatBlue");
		public static readonly Color Ochre = new Color(0.8,0.466666666666667,0.133333333333333,1.0,"Ochre");
		public static readonly Color OfficeGreen = new Color(0,0.501960784313725,0,1.0,"OfficeGreen");
		public static readonly Color OldGold = new Color(0.811764705882353,0.709803921568627,0.231372549019608,1.0,"OldGold");
		public static readonly Color OldLace = new Color(0.992156862745098,0.96078431372549,0.901960784313726,1.0,"OldLace");
		public static readonly Color OldLavender = new Color(0.474509803921569,0.407843137254902,0.470588235294118,1.0,"OldLavender");
		public static readonly Color OldMauve = new Color(0.403921568627451,0.192156862745098,0.27843137254902,1.0,"OldMauve");
		public static readonly Color OldRose = new Color(0.752941176470588,0.501960784313725,0.505882352941176,1.0,"OldRose");
		public static readonly Color Olive = new Color(0.501960784313725,0.501960784313725,0,1.0,"Olive");
		public static readonly Color OliveDrab7 = new Color(0.235294117647059,0.203921568627451,0.12156862745098,1.0,"OliveDrab7");
		public static readonly Color OliveDrabWebOliveDrab3 = new Color(0.419607843137255,0.556862745098039,0.137254901960784,1.0,"OliveDrabWebOliveDrab3");
		public static readonly Color Olivine = new Color(0.603921568627451,0.725490196078431,0.450980392156863,1.0,"Olivine");
		public static readonly Color Onyx = new Color(0.207843137254902,0.219607843137255,0.223529411764706,1.0,"Onyx");
		public static readonly Color OperaMauve = new Color(0.717647058823529,0.517647058823529,0.654901960784314,1.0,"OperaMauve");
		public static readonly Color OrangeColorWheel = new Color(1,0.498039215686275,0,1.0,"OrangeColorWheel");
		public static readonly Color OrangePeel = new Color(1,0.623529411764706,0,1.0,"OrangePeel");
		public static readonly Color OrangeRed = new Color(1,0.270588235294118,0,1.0,"OrangeRed");
		public static readonly Color OrangeRyb = new Color(0.984313725490196,0.6,0.00784313725490196,1.0,"OrangeRyb");
		public static readonly Color OrangeWebColor = new Color(1,0.647058823529412,0,1.0,"OrangeWebColor");
		public static readonly Color Orchid = new Color(0.854901960784314,0.43921568627451,0.83921568627451,1.0,"Orchid");
		public static readonly Color OtterBrown = new Color(0.396078431372549,0.262745098039216,0.129411764705882,1.0,"OtterBrown");
		public static readonly Color OuCrimsonRed = new Color(0.6,0,0,1.0,"OuCrimsonRed");
		public static readonly Color OuterSpace = new Color(0.254901960784314,0.290196078431373,0.298039215686275,1.0,"OuterSpace");
		public static readonly Color OutrageousOrange = new Color(1,0.431372549019608,0.290196078431373,1.0,"OutrageousOrange");
		public static readonly Color OxfordBlue = new Color(0,0.129411764705882,0.27843137254902,1.0,"OxfordBlue");
		public static readonly Color PakistanGreen = new Color(0,0.4,0,1.0,"PakistanGreen");
		public static readonly Color PalatinateBlue = new Color(0.152941176470588,0.231372549019608,0.886274509803922,1.0,"PalatinateBlue");
		public static readonly Color PalatinatePurple = new Color(0.407843137254902,0.156862745098039,0.376470588235294,1.0,"PalatinatePurple");
		public static readonly Color PaleAqua = new Color(0.737254901960784,0.831372549019608,0.901960784313726,1.0,"PaleAqua");
		public static readonly Color PaleBlue = new Color(0.686274509803922,0.933333333333333,0.933333333333333,1.0,"PaleBlue");
		public static readonly Color PaleBrown = new Color(0.596078431372549,0.462745098039216,0.329411764705882,1.0,"PaleBrown");
		public static readonly Color PaleCarmine = new Color(0.686274509803922,0.250980392156863,0.207843137254902,1.0,"PaleCarmine");
		public static readonly Color PaleCerulean = new Color(0.607843137254902,0.768627450980392,0.886274509803922,1.0,"PaleCerulean");
		public static readonly Color PaleChestnut = new Color(0.866666666666667,0.67843137254902,0.686274509803922,1.0,"PaleChestnut");
		public static readonly Color PaleCopper = new Color(0.854901960784314,0.541176470588235,0.403921568627451,1.0,"PaleCopper");
		public static readonly Color PaleCornflowerBlue = new Color(0.670588235294118,0.803921568627451,0.937254901960784,1.0,"PaleCornflowerBlue");
		public static readonly Color PaleGold = new Color(0.901960784313726,0.745098039215686,0.541176470588235,1.0,"PaleGold");
		public static readonly Color PaleGoldenrod = new Color(0.933333333333333,0.909803921568627,0.666666666666667,1.0,"PaleGoldenrod");
		public static readonly Color PaleGreen = new Color(0.596078431372549,0.984313725490196,0.596078431372549,1.0,"PaleGreen");
		public static readonly Color PaleLavender = new Color(0.862745098039216,0.815686274509804,1,1.0,"PaleLavender");
		public static readonly Color PaleMagenta = new Color(0.976470588235294,0.517647058823529,0.898039215686275,1.0,"PaleMagenta");
		public static readonly Color PalePink = new Color(0.980392156862745,0.854901960784314,0.866666666666667,1.0,"PalePink");
		public static readonly Color PalePlum = new Color(0.866666666666667,0.627450980392157,0.866666666666667,1.0,"PalePlum");
		public static readonly Color PaleRedViolet = new Color(0.858823529411765,0.43921568627451,0.576470588235294,1.0,"PaleRedViolet");
		public static readonly Color PaleRobinEggBlue = new Color(0.588235294117647,0.870588235294118,0.819607843137255,1.0,"PaleRobinEggBlue");
		public static readonly Color PaleSilver = new Color(0.788235294117647,0.752941176470588,0.733333333333333,1.0,"PaleSilver");
		public static readonly Color PaleSpringBud = new Color(0.925490196078431,0.92156862745098,0.741176470588235,1.0,"PaleSpringBud");
		public static readonly Color PaleTaupe = new Color(0.737254901960784,0.596078431372549,0.494117647058824,1.0,"PaleTaupe");
		public static readonly Color PaleVioletRed = new Color(0.858823529411765,0.43921568627451,0.576470588235294,1.0,"PaleVioletRed");
		public static readonly Color PansyPurple = new Color(0.470588235294118,0.0941176470588235,0.290196078431373,1.0,"PansyPurple");
		public static readonly Color PapayaWhip = new Color(1,0.937254901960784,0.835294117647059,1.0,"PapayaWhip");
		public static readonly Color ParisGreen = new Color(0.313725490196078,0.784313725490196,0.470588235294118,1.0,"ParisGreen");
		public static readonly Color PastelBlue = new Color(0.682352941176471,0.776470588235294,0.811764705882353,1.0,"PastelBlue");
		public static readonly Color PastelBrown = new Color(0.513725490196078,0.411764705882353,0.325490196078431,1.0,"PastelBrown");
		public static readonly Color PastelGray = new Color(0.811764705882353,0.811764705882353,0.768627450980392,1.0,"PastelGray");
		public static readonly Color PastelGreen = new Color(0.466666666666667,0.866666666666667,0.466666666666667,1.0,"PastelGreen");
		public static readonly Color PastelMagenta = new Color(0.956862745098039,0.603921568627451,0.76078431372549,1.0,"PastelMagenta");
		public static readonly Color PastelOrange = new Color(1,0.701960784313725,0.27843137254902,1.0,"PastelOrange");
		public static readonly Color PastelPink = new Color(0.870588235294118,0.647058823529412,0.643137254901961,1.0,"PastelPink");
		public static readonly Color PastelPurple = new Color(0.701960784313725,0.619607843137255,0.709803921568627,1.0,"PastelPurple");
		public static readonly Color PastelRed = new Color(1,0.411764705882353,0.380392156862745,1.0,"PastelRed");
		public static readonly Color PastelViolet = new Color(0.796078431372549,0.6,0.788235294117647,1.0,"PastelViolet");
		public static readonly Color PastelYellow = new Color(0.992156862745098,0.992156862745098,0.588235294117647,1.0,"PastelYellow");
		public static readonly Color Patriarch = new Color(0.501960784313725,0,0.501960784313725,1.0,"Patriarch");
		public static readonly Color PayneSGrey = new Color(0.325490196078431,0.407843137254902,0.470588235294118,1.0,"PayneSGrey");
		public static readonly Color Peach = new Color(1,0.898039215686275,0.705882352941177,1.0,"Peach");
		public static readonly Color PeachCrayola = new Color(1,0.796078431372549,0.643137254901961,1.0,"PeachCrayola");
		public static readonly Color PeachOrange = new Color(1,0.8,0.6,1.0,"PeachOrange");
		public static readonly Color PeachPuff = new Color(1,0.854901960784314,0.725490196078431,1.0,"PeachPuff");
		public static readonly Color PeachYellow = new Color(0.980392156862745,0.874509803921569,0.67843137254902,1.0,"PeachYellow");
		public static readonly Color Pear = new Color(0.819607843137255,0.886274509803922,0.192156862745098,1.0,"Pear");
		public static readonly Color Pearl = new Color(0.917647058823529,0.87843137254902,0.784313725490196,1.0,"Pearl");
		public static readonly Color PearlAqua = new Color(0.533333333333333,0.847058823529412,0.752941176470588,1.0,"PearlAqua");
		public static readonly Color PearlyPurple = new Color(0.717647058823529,0.407843137254902,0.635294117647059,1.0,"PearlyPurple");
		public static readonly Color Peridot = new Color(0.901960784313726,0.886274509803922,0,1.0,"Peridot");
		public static readonly Color Periwinkle = new Color(0.8,0.8,1,1.0,"Periwinkle");
		public static readonly Color PersianBlue = new Color(0.109803921568627,0.223529411764706,0.733333333333333,1.0,"PersianBlue");
		public static readonly Color PersianGreen = new Color(0,0.650980392156863,0.576470588235294,1.0,"PersianGreen");
		public static readonly Color PersianIndigo = new Color(0.196078431372549,0.0705882352941176,0.47843137254902,1.0,"PersianIndigo");
		public static readonly Color PersianOrange = new Color(0.850980392156863,0.564705882352941,0.345098039215686,1.0,"PersianOrange");
		public static readonly Color PersianPink = new Color(0.968627450980392,0.498039215686275,0.745098039215686,1.0,"PersianPink");
		public static readonly Color PersianPlum = new Color(0.43921568627451,0.109803921568627,0.109803921568627,1.0,"PersianPlum");
		public static readonly Color PersianRed = new Color(0.8,0.2,0.2,1.0,"PersianRed");
		public static readonly Color PersianRose = new Color(0.996078431372549,0.156862745098039,0.635294117647059,1.0,"PersianRose");
		public static readonly Color Persimmon = new Color(0.925490196078431,0.345098039215686,0,1.0,"Persimmon");
		public static readonly Color Peru = new Color(0.803921568627451,0.52156862745098,0.247058823529412,1.0,"Peru");
		public static readonly Color Phlox = new Color(0.874509803921569,0,1,1.0,"Phlox");
		public static readonly Color PhthaloBlue = new Color(0,0.0588235294117647,0.537254901960784,1.0,"PhthaloBlue");
		public static readonly Color PhthaloGreen = new Color(0.0705882352941176,0.207843137254902,0.141176470588235,1.0,"PhthaloGreen");
		public static readonly Color PiggyPink = new Color(0.992156862745098,0.866666666666667,0.901960784313726,1.0,"PiggyPink");
		public static readonly Color PineGreen = new Color(0.00392156862745098,0.474509803921569,0.435294117647059,1.0,"PineGreen");
		public static readonly Color Pink = new Color(1,0.752941176470588,0.796078431372549,1.0,"Pink");
		public static readonly Color PinkLace = new Color(1,0.866666666666667,0.956862745098039,1.0,"PinkLace");
		public static readonly Color PinkOrange = new Color(1,0.6,0.4,1.0,"PinkOrange");
		public static readonly Color PinkPearl = new Color(0.905882352941176,0.674509803921569,0.811764705882353,1.0,"PinkPearl");
		public static readonly Color PinkSherbet = new Color(0.968627450980392,0.56078431372549,0.654901960784314,1.0,"PinkSherbet");
		public static readonly Color Pistachio = new Color(0.576470588235294,0.772549019607843,0.447058823529412,1.0,"Pistachio");
		public static readonly Color Platinum = new Color(0.898039215686275,0.894117647058824,0.886274509803922,1.0,"Platinum");
		public static readonly Color PlumTraditional = new Color(0.556862745098039,0.270588235294118,0.52156862745098,1.0,"PlumTraditional");
		public static readonly Color PlumWeb = new Color(0.866666666666667,0.627450980392157,0.866666666666667,1.0,"PlumWeb");
		public static readonly Color PortlandOrange = new Color(1,0.352941176470588,0.211764705882353,1.0,"PortlandOrange");
		public static readonly Color PowderBlueWeb = new Color(0.690196078431373,0.87843137254902,0.901960784313726,1.0,"PowderBlueWeb");
		public static readonly Color PrincetonOrange = new Color(1,0.56078431372549,0,1.0,"PrincetonOrange");
		public static readonly Color Prune = new Color(0.43921568627451,0.109803921568627,0.109803921568627,1.0,"Prune");
		public static readonly Color PrussianBlue = new Color(0,0.192156862745098,0.325490196078431,1.0,"PrussianBlue");
		public static readonly Color PsychedelicPurple = new Color(0.874509803921569,0,1,1.0,"PsychedelicPurple");
		public static readonly Color Puce = new Color(0.8,0.533333333333333,0.6,1.0,"Puce");
		public static readonly Color Pumpkin = new Color(1,0.458823529411765,0.0941176470588235,1.0,"Pumpkin");
		public static readonly Color PurpleHeart = new Color(0.411764705882353,0.207843137254902,0.611764705882353,1.0,"PurpleHeart");
		public static readonly Color PurpleHtmlCss = new Color(0.501960784313725,0,0.501960784313725,1.0,"PurpleHtmlCss");
		public static readonly Color PurpleMountainMajesty = new Color(0.588235294117647,0.470588235294118,0.713725490196078,1.0,"PurpleMountainMajesty");
		public static readonly Color PurpleMunsell = new Color(0.623529411764706,0,0.772549019607843,1.0,"PurpleMunsell");
		public static readonly Color PurplePizzazz = new Color(0.996078431372549,0.305882352941176,0.854901960784314,1.0,"PurplePizzazz");
		public static readonly Color PurpleTaupe = new Color(0.313725490196078,0.250980392156863,0.301960784313725,1.0,"PurpleTaupe");
		public static readonly Color PurpleX11 = new Color(0.627450980392157,0.125490196078431,0.941176470588235,1.0,"PurpleX11");
		public static readonly Color Quartz = new Color(0.317647058823529,0.282352941176471,0.309803921568627,1.0,"Quartz");
		public static readonly Color Rackley = new Color(0.364705882352941,0.541176470588235,0.658823529411765,1.0,"Rackley");
		public static readonly Color RadicalRed = new Color(1,0.207843137254902,0.368627450980392,1.0,"RadicalRed");
		public static readonly Color Rajah = new Color(0.984313725490196,0.670588235294118,0.376470588235294,1.0,"Rajah");
		public static readonly Color Raspberry = new Color(0.890196078431373,0.0431372549019608,0.364705882352941,1.0,"Raspberry");
		public static readonly Color RaspberryGlace = new Color(0.568627450980392,0.372549019607843,0.427450980392157,1.0,"RaspberryGlace");
		public static readonly Color RaspberryPink = new Color(0.886274509803922,0.313725490196078,0.596078431372549,1.0,"RaspberryPink");
		public static readonly Color RaspberryRose = new Color(0.701960784313725,0.266666666666667,0.423529411764706,1.0,"RaspberryRose");
		public static readonly Color RawUmber = new Color(0.509803921568627,0.4,0.266666666666667,1.0,"RawUmber");
		public static readonly Color RazzleDazzleRose = new Color(1,0.2,0.8,1.0,"RazzleDazzleRose");
		public static readonly Color Razzmatazz = new Color(0.890196078431373,0.145098039215686,0.419607843137255,1.0,"Razzmatazz");
		public static readonly Color Red = new Color(1,0,0,1.0,"Red");
		public static readonly Color RedBrown = new Color(0.647058823529412,0.164705882352941,0.164705882352941,1.0,"RedBrown");
		public static readonly Color RedDevil = new Color(0.525490196078431,0.00392156862745098,0.0666666666666667,1.0,"RedDevil");
		public static readonly Color RedMunsell = new Color(0.949019607843137,0,0.235294117647059,1.0,"RedMunsell");
		public static readonly Color RedNcs = new Color(0.768627450980392,0.00784313725490196,0.2,1.0,"RedNcs");
		public static readonly Color RedOrange = new Color(1,0.325490196078431,0.286274509803922,1.0,"RedOrange");
		public static readonly Color RedPigment = new Color(0.929411764705882,0.109803921568627,0.141176470588235,1.0,"RedPigment");
		public static readonly Color RedRyb = new Color(0.996078431372549,0.152941176470588,0.0705882352941176,1.0,"RedRyb");
		public static readonly Color RedViolet = new Color(0.780392156862745,0.0823529411764706,0.52156862745098,1.0,"RedViolet");
		public static readonly Color Redwood = new Color(0.670588235294118,0.305882352941176,0.32156862745098,1.0,"Redwood");
		public static readonly Color Regalia = new Color(0.32156862745098,0.176470588235294,0.501960784313725,1.0,"Regalia");
		public static readonly Color ResolutionBlue = new Color(0,0.137254901960784,0.529411764705882,1.0,"ResolutionBlue");
		public static readonly Color RichBlack = new Color(0,0.250980392156863,0.250980392156863,1.0,"RichBlack");
		public static readonly Color RichBrilliantLavender = new Color(0.945098039215686,0.654901960784314,0.996078431372549,1.0,"RichBrilliantLavender");
		public static readonly Color RichCarmine = new Color(0.843137254901961,0,0.250980392156863,1.0,"RichCarmine");
		public static readonly Color RichElectricBlue = new Color(0.0313725490196078,0.572549019607843,0.815686274509804,1.0,"RichElectricBlue");
		public static readonly Color RichLavender = new Color(0.654901960784314,0.419607843137255,0.811764705882353,1.0,"RichLavender");
		public static readonly Color RichLilac = new Color(0.713725490196078,0.4,0.823529411764706,1.0,"RichLilac");
		public static readonly Color RichMaroon = new Color(0.690196078431373,0.188235294117647,0.376470588235294,1.0,"RichMaroon");
		public static readonly Color RifleGreen = new Color(0.254901960784314,0.282352941176471,0.2,1.0,"RifleGreen");
		public static readonly Color RobinEggBlue = new Color(0,0.8,0.8,1.0,"RobinEggBlue");
		public static readonly Color Rose = new Color(1,0,0.498039215686275,1.0,"Rose");
		public static readonly Color RoseBonbon = new Color(0.976470588235294,0.258823529411765,0.619607843137255,1.0,"RoseBonbon");
		public static readonly Color RoseEbony = new Color(0.403921568627451,0.282352941176471,0.274509803921569,1.0,"RoseEbony");
		public static readonly Color RoseGold = new Color(0.717647058823529,0.431372549019608,0.474509803921569,1.0,"RoseGold");
		public static readonly Color RoseMadder = new Color(0.890196078431373,0.149019607843137,0.211764705882353,1.0,"RoseMadder");
		public static readonly Color RosePink = new Color(1,0.4,0.8,1.0,"RosePink");
		public static readonly Color RoseQuartz = new Color(0.666666666666667,0.596078431372549,0.662745098039216,1.0,"RoseQuartz");
		public static readonly Color RoseTaupe = new Color(0.564705882352941,0.364705882352941,0.364705882352941,1.0,"RoseTaupe");
		public static readonly Color RoseVale = new Color(0.670588235294118,0.305882352941176,0.32156862745098,1.0,"RoseVale");
		public static readonly Color Rosewood = new Color(0.396078431372549,0,0.0431372549019608,1.0,"Rosewood");
		public static readonly Color RossoCorsa = new Color(0.831372549019608,0,0,1.0,"RossoCorsa");
		public static readonly Color RosyBrown = new Color(0.737254901960784,0.56078431372549,0.56078431372549,1.0,"RosyBrown");
		public static readonly Color RoyalAzure = new Color(0,0.219607843137255,0.658823529411765,1.0,"RoyalAzure");
		public static readonly Color RoyalBlueTraditional = new Color(0,0.137254901960784,0.4,1.0,"RoyalBlueTraditional");
		public static readonly Color RoyalBlueWeb = new Color(0.254901960784314,0.411764705882353,0.882352941176471,1.0,"RoyalBlueWeb");
		public static readonly Color RoyalFuchsia = new Color(0.792156862745098,0.172549019607843,0.572549019607843,1.0,"RoyalFuchsia");
		public static readonly Color RoyalPurple = new Color(0.470588235294118,0.317647058823529,0.662745098039216,1.0,"RoyalPurple");
		public static readonly Color RoyalYellow = new Color(0.980392156862745,0.854901960784314,0.368627450980392,1.0,"RoyalYellow");
		public static readonly Color RubineRed = new Color(0.819607843137255,0,0.337254901960784,1.0,"RubineRed");
		public static readonly Color Ruby = new Color(0.87843137254902,0.0666666666666667,0.372549019607843,1.0,"Ruby");
		public static readonly Color RubyRed = new Color(0.607843137254902,0.0666666666666667,0.117647058823529,1.0,"RubyRed");
		public static readonly Color Ruddy = new Color(1,0,0.156862745098039,1.0,"Ruddy");
		public static readonly Color RuddyBrown = new Color(0.733333333333333,0.396078431372549,0.156862745098039,1.0,"RuddyBrown");
		public static readonly Color RuddyPink = new Color(0.882352941176471,0.556862745098039,0.588235294117647,1.0,"RuddyPink");
		public static readonly Color Rufous = new Color(0.658823529411765,0.109803921568627,0.0274509803921569,1.0,"Rufous");
		public static readonly Color Russet = new Color(0.501960784313725,0.274509803921569,0.105882352941176,1.0,"Russet");
		public static readonly Color Rust = new Color(0.717647058823529,0.254901960784314,0.0549019607843137,1.0,"Rust");
		public static readonly Color RustyRed = new Color(0.854901960784314,0.172549019607843,0.262745098039216,1.0,"RustyRed");
		public static readonly Color SacramentoStateGreen = new Color(0,0.337254901960784,0.247058823529412,1.0,"SacramentoStateGreen");
		public static readonly Color SaddleBrown = new Color(0.545098039215686,0.270588235294118,0.0745098039215686,1.0,"SaddleBrown");
		public static readonly Color SafetyOrangeBlazeOrange = new Color(1,0.403921568627451,0,1.0,"SafetyOrangeBlazeOrange");
		public static readonly Color Saffron = new Color(0.956862745098039,0.768627450980392,0.188235294117647,1.0,"Saffron");
		public static readonly Color Salmon = new Color(1,0.549019607843137,0.411764705882353,1.0,"Salmon");
		public static readonly Color SalmonPink = new Color(1,0.568627450980392,0.643137254901961,1.0,"SalmonPink");
		public static readonly Color Sand = new Color(0.76078431372549,0.698039215686274,0.501960784313725,1.0,"Sand");
		public static readonly Color SandDune = new Color(0.588235294117647,0.443137254901961,0.0901960784313725,1.0,"SandDune");
		public static readonly Color Sandstorm = new Color(0.925490196078431,0.835294117647059,0.250980392156863,1.0,"Sandstorm");
		public static readonly Color SandyBrown = new Color(0.956862745098039,0.643137254901961,0.376470588235294,1.0,"SandyBrown");
		public static readonly Color SandyTaupe = new Color(0.588235294117647,0.443137254901961,0.0901960784313725,1.0,"SandyTaupe");
		public static readonly Color Sangria = new Color(0.572549019607843,0,0.0392156862745098,1.0,"Sangria");
		public static readonly Color SapGreen = new Color(0.313725490196078,0.490196078431373,0.164705882352941,1.0,"SapGreen");
		public static readonly Color Sapphire = new Color(0.0588235294117647,0.32156862745098,0.729411764705882,1.0,"Sapphire");
		public static readonly Color SapphireBlue = new Color(0,0.403921568627451,0.647058823529412,1.0,"SapphireBlue");
		public static readonly Color SatinSheenGold = new Color(0.796078431372549,0.631372549019608,0.207843137254902,1.0,"SatinSheenGold");
		public static readonly Color Scarlet = new Color(1,0.141176470588235,0,1.0,"Scarlet");
		public static readonly Color ScarletCrayola = new Color(0.992156862745098,0.0549019607843137,0.207843137254902,1.0,"ScarletCrayola");
		public static readonly Color SchoolBusYellow = new Color(1,0.847058823529412,0,1.0,"SchoolBusYellow");
		public static readonly Color ScreaminGreen = new Color(0.462745098039216,1,0.47843137254902,1.0,"ScreaminGreen");
		public static readonly Color SeaBlue = new Color(0,0.411764705882353,0.580392156862745,1.0,"SeaBlue");
		public static readonly Color SeaGreen = new Color(0.180392156862745,0.545098039215686,0.341176470588235,1.0,"SeaGreen");
		public static readonly Color SealBrown = new Color(0.196078431372549,0.0784313725490196,0.0784313725490196,1.0,"SealBrown");
		public static readonly Color Seashell = new Color(1,0.96078431372549,0.933333333333333,1.0,"Seashell");
		public static readonly Color SelectiveYellow = new Color(1,0.729411764705882,0,1.0,"SelectiveYellow");
		public static readonly Color Sepia = new Color(0.43921568627451,0.258823529411765,0.0784313725490196,1.0,"Sepia");
		public static readonly Color Shadow = new Color(0.541176470588235,0.474509803921569,0.364705882352941,1.0,"Shadow");
		public static readonly Color ShamrockGreen = new Color(0,0.619607843137255,0.376470588235294,1.0,"ShamrockGreen");
		public static readonly Color ShockingPink = new Color(0.988235294117647,0.0588235294117647,0.752941176470588,1.0,"ShockingPink");
		public static readonly Color ShockingPinkCrayola = new Color(1,0.435294117647059,1,1.0,"ShockingPinkCrayola");
		public static readonly Color Sienna = new Color(0.533333333333333,0.176470588235294,0.0901960784313725,1.0,"Sienna");
		public static readonly Color Silver = new Color(0.752941176470588,0.752941176470588,0.752941176470588,1.0,"Silver");
		public static readonly Color Sinopia = new Color(0.796078431372549,0.254901960784314,0.0431372549019608,1.0,"Sinopia");
		public static readonly Color Skobeloff = new Color(0,0.454901960784314,0.454901960784314,1.0,"Skobeloff");
		public static readonly Color SkyBlue = new Color(0.529411764705882,0.807843137254902,0.92156862745098,1.0,"SkyBlue");
		public static readonly Color SkyMagenta = new Color(0.811764705882353,0.443137254901961,0.686274509803922,1.0,"SkyMagenta");
		public static readonly Color SlateBlue = new Color(0.415686274509804,0.352941176470588,0.803921568627451,1.0,"SlateBlue");
		public static readonly Color SlateGray = new Color(0.43921568627451,0.501960784313725,0.564705882352941,1.0,"SlateGray");
		public static readonly Color SmaltDarkPowderBlue = new Color(0,0.2,0.6,1.0,"SmaltDarkPowderBlue");
		public static readonly Color SmokeyTopaz = new Color(0.576470588235294,0.23921568627451,0.254901960784314,1.0,"SmokeyTopaz");
		public static readonly Color SmokyBlack = new Color(0.0627450980392157,0.0470588235294118,0.0313725490196078,1.0,"SmokyBlack");
		public static readonly Color Snow = new Color(1,0.980392156862745,0.980392156862745,1.0,"Snow");
		public static readonly Color SpiroDiscoBall = new Color(0.0588235294117647,0.752941176470588,0.988235294117647,1.0,"SpiroDiscoBall");
		public static readonly Color SpringBud = new Color(0.654901960784314,0.988235294117647,0,1.0,"SpringBud");
		public static readonly Color SpringGreen = new Color(0,1,0.498039215686275,1.0,"SpringGreen");
		public static readonly Color StPatrickSBlue = new Color(0.137254901960784,0.16078431372549,0.47843137254902,1.0,"StPatrickSBlue");
		public static readonly Color SteelBlue = new Color(0.274509803921569,0.509803921568627,0.705882352941177,1.0,"SteelBlue");
		public static readonly Color StilDeGrainYellow = new Color(0.980392156862745,0.854901960784314,0.368627450980392,1.0,"StilDeGrainYellow");
		public static readonly Color Stizza = new Color(0.6,0,0,1.0,"Stizza");
		public static readonly Color Stormcloud = new Color(0.309803921568627,0.4,0.415686274509804,1.0,"Stormcloud");
		public static readonly Color Straw = new Color(0.894117647058824,0.850980392156863,0.435294117647059,1.0,"Straw");
		public static readonly Color Sunglow = new Color(1,0.8,0.2,1.0,"Sunglow");
		public static readonly Color Sunset = new Color(0.980392156862745,0.83921568627451,0.647058823529412,1.0,"Sunset");
		public static readonly Color Tan = new Color(0.823529411764706,0.705882352941177,0.549019607843137,1.0,"Tan");
		public static readonly Color Tangelo = new Color(0.976470588235294,0.301960784313725,0,1.0,"Tangelo");
		public static readonly Color Tangerine = new Color(0.949019607843137,0.52156862745098,0,1.0,"Tangerine");
		public static readonly Color TangerineYellow = new Color(1,0.8,0,1.0,"TangerineYellow");
		public static readonly Color TangoPink = new Color(0.894117647058824,0.443137254901961,0.47843137254902,1.0,"TangoPink");
		public static readonly Color Taupe = new Color(0.282352941176471,0.235294117647059,0.196078431372549,1.0,"Taupe");
		public static readonly Color TaupeGray = new Color(0.545098039215686,0.52156862745098,0.537254901960784,1.0,"TaupeGray");
		public static readonly Color TeaGreen = new Color(0.815686274509804,0.941176470588235,0.752941176470588,1.0,"TeaGreen");
		public static readonly Color TeaRoseOrange = new Color(0.972549019607843,0.513725490196078,0.474509803921569,1.0,"TeaRoseOrange");
		public static readonly Color TeaRoseRose = new Color(0.956862745098039,0.76078431372549,0.76078431372549,1.0,"TeaRoseRose");
		public static readonly Color Teal = new Color(0,0.501960784313725,0.501960784313725,1.0,"Teal");
		public static readonly Color TealBlue = new Color(0.211764705882353,0.458823529411765,0.533333333333333,1.0,"TealBlue");
		public static readonly Color TealGreen = new Color(0,0.509803921568627,0.498039215686275,1.0,"TealGreen");
		public static readonly Color Telemagenta = new Color(0.811764705882353,0.203921568627451,0.462745098039216,1.0,"Telemagenta");
		public static readonly Color TennTawny = new Color(0.803921568627451,0.341176470588235,0,1.0,"TennTawny");
		public static readonly Color TerraCotta = new Color(0.886274509803922,0.447058823529412,0.356862745098039,1.0,"TerraCotta");
		public static readonly Color Thistle = new Color(0.847058823529412,0.749019607843137,0.847058823529412,1.0,"Thistle");
		public static readonly Color ThulianPink = new Color(0.870588235294118,0.435294117647059,0.631372549019608,1.0,"ThulianPink");
		public static readonly Color TickleMePink = new Color(0.988235294117647,0.537254901960784,0.674509803921569,1.0,"TickleMePink");
		public static readonly Color TiffanyBlue = new Color(0.0392156862745098,0.729411764705882,0.709803921568627,1.0,"TiffanyBlue");
		public static readonly Color TigerSEye = new Color(0.87843137254902,0.552941176470588,0.235294117647059,1.0,"TigerSEye");
		public static readonly Color Timberwolf = new Color(0.858823529411765,0.843137254901961,0.823529411764706,1.0,"Timberwolf");
		public static readonly Color TitaniumYellow = new Color(0.933333333333333,0.901960784313726,0,1.0,"TitaniumYellow");
		public static readonly Color Tomato = new Color(1,0.388235294117647,0.27843137254902,1.0,"Tomato");
		public static readonly Color Toolbox = new Color(0.454901960784314,0.423529411764706,0.752941176470588,1.0,"Toolbox");
		public static readonly Color Topaz = new Color(1,0.784313725490196,0.486274509803922,1.0,"Topaz");
		public static readonly Color TractorRed = new Color(0.992156862745098,0.0549019607843137,0.207843137254902,1.0,"TractorRed");
		public static readonly Color TrolleyGrey = new Color(0.501960784313725,0.501960784313725,0.501960784313725,1.0,"TrolleyGrey");
		public static readonly Color TropicalRainForest = new Color(0,0.458823529411765,0.368627450980392,1.0,"TropicalRainForest");
		public static readonly Color TrueBlue = new Color(0,0.450980392156863,0.811764705882353,1.0,"TrueBlue");
		public static readonly Color TuftsBlue = new Color(0.254901960784314,0.490196078431373,0.756862745098039,1.0,"TuftsBlue");
		public static readonly Color Tumbleweed = new Color(0.870588235294118,0.666666666666667,0.533333333333333,1.0,"Tumbleweed");
		public static readonly Color TurkishRose = new Color(0.709803921568627,0.447058823529412,0.505882352941176,1.0,"TurkishRose");
		public static readonly Color Turquoise = new Color(0.188235294117647,0.835294117647059,0.784313725490196,1.0,"Turquoise");
		public static readonly Color TurquoiseBlue = new Color(0,1,0.937254901960784,1.0,"TurquoiseBlue");
		public static readonly Color TurquoiseGreen = new Color(0.627450980392157,0.83921568627451,0.705882352941177,1.0,"TurquoiseGreen");
		public static readonly Color TuscanRed = new Color(0.486274509803922,0.282352941176471,0.282352941176471,1.0,"TuscanRed");
		public static readonly Color TwilightLavender = new Color(0.541176470588235,0.286274509803922,0.419607843137255,1.0,"TwilightLavender");
		public static readonly Color TyrianPurple = new Color(0.4,0.00784313725490196,0.235294117647059,1.0,"TyrianPurple");
		public static readonly Color UaBlue = new Color(0,0.2,0.666666666666667,1.0,"UaBlue");
		public static readonly Color UaRed = new Color(0.850980392156863,0,0.298039215686275,1.0,"UaRed");
		public static readonly Color Ube = new Color(0.533333333333333,0.470588235294118,0.764705882352941,1.0,"Ube");
		public static readonly Color UclaBlue = new Color(0.325490196078431,0.407843137254902,0.584313725490196,1.0,"UclaBlue");
		public static readonly Color UclaGold = new Color(1,0.701960784313725,0,1.0,"UclaGold");
		public static readonly Color UfoGreen = new Color(0.235294117647059,0.815686274509804,0.43921568627451,1.0,"UfoGreen");
		public static readonly Color UltraPink = new Color(1,0.435294117647059,1,1.0,"UltraPink");
		public static readonly Color Ultramarine = new Color(0.0705882352941176,0.0392156862745098,0.56078431372549,1.0,"Ultramarine");
		public static readonly Color UltramarineBlue = new Color(0.254901960784314,0.4,0.96078431372549,1.0,"UltramarineBlue");
		public static readonly Color Umber = new Color(0.388235294117647,0.317647058823529,0.27843137254902,1.0,"Umber");
		public static readonly Color UnbleachedSilk = new Color(1,0.866666666666667,0.792156862745098,1.0,"UnbleachedSilk");
		public static readonly Color UnitedNationsBlue = new Color(0.356862745098039,0.572549019607843,0.898039215686275,1.0,"UnitedNationsBlue");
		public static readonly Color UniversityOfCaliforniaGold = new Color(0.717647058823529,0.529411764705882,0.152941176470588,1.0,"UniversityOfCaliforniaGold");
		public static readonly Color UnmellowYellow = new Color(1,1,0.4,1.0,"UnmellowYellow");
		public static readonly Color UpForestGreen = new Color(0.00392156862745098,0.266666666666667,0.129411764705882,1.0,"UpForestGreen");
		public static readonly Color UpMaroon = new Color(0.482352941176471,0.0666666666666667,0.0745098039215686,1.0,"UpMaroon");
		public static readonly Color UpsdellRed = new Color(0.682352941176471,0.125490196078431,0.16078431372549,1.0,"UpsdellRed");
		public static readonly Color Urobilin = new Color(0.882352941176471,0.67843137254902,0.129411764705882,1.0,"Urobilin");
		public static readonly Color UsafaBlue = new Color(0,0.309803921568627,0.596078431372549,1.0,"UsafaBlue");
		public static readonly Color UscCardinal = new Color(0.6,0,0,1.0,"UscCardinal");
		public static readonly Color UscGold = new Color(1,0.8,0,1.0,"UscGold");
		public static readonly Color UtahCrimson = new Color(0.827450980392157,0,0.247058823529412,1.0,"UtahCrimson");
		public static readonly Color Vanilla = new Color(0.952941176470588,0.898039215686275,0.670588235294118,1.0,"Vanilla");
		public static readonly Color VegasGold = new Color(0.772549019607843,0.701960784313725,0.345098039215686,1.0,"VegasGold");
		public static readonly Color VenetianRed = new Color(0.784313725490196,0.0313725490196078,0.0823529411764706,1.0,"VenetianRed");
		public static readonly Color Verdigris = new Color(0.262745098039216,0.701960784313725,0.682352941176471,1.0,"Verdigris");
		public static readonly Color VermilionCinnabar = new Color(0.890196078431373,0.258823529411765,0.203921568627451,1.0,"VermilionCinnabar");
		public static readonly Color VermilionPlochere = new Color(0.850980392156863,0.376470588235294,0.231372549019608,1.0,"VermilionPlochere");
		public static readonly Color Veronica = new Color(0.627450980392157,0.125490196078431,0.941176470588235,1.0,"Veronica");
		public static readonly Color Violet = new Color(0.56078431372549,0,1,1.0,"Violet");
		public static readonly Color VioletBlue = new Color(0.196078431372549,0.290196078431373,0.698039215686274,1.0,"VioletBlue");
		public static readonly Color VioletColorWheel = new Color(0.498039215686275,0,1,1.0,"VioletColorWheel");
		public static readonly Color VioletRyb = new Color(0.525490196078431,0.00392156862745098,0.686274509803922,1.0,"VioletRyb");
		public static readonly Color VioletWeb = new Color(0.933333333333333,0.509803921568627,0.933333333333333,1.0,"VioletWeb");
		public static readonly Color Viridian = new Color(0.250980392156863,0.509803921568627,0.427450980392157,1.0,"Viridian");
		public static readonly Color VividAuburn = new Color(0.572549019607843,0.152941176470588,0.141176470588235,1.0,"VividAuburn");
		public static readonly Color VividBurgundy = new Color(0.623529411764706,0.113725490196078,0.207843137254902,1.0,"VividBurgundy");
		public static readonly Color VividCerise = new Color(0.854901960784314,0.113725490196078,0.505882352941176,1.0,"VividCerise");
		public static readonly Color VividTangerine = new Color(1,0.627450980392157,0.537254901960784,1.0,"VividTangerine");
		public static readonly Color VividViolet = new Color(0.623529411764706,0,1,1.0,"VividViolet");
		public static readonly Color WarmBlack = new Color(0,0.258823529411765,0.258823529411765,1.0,"WarmBlack");
		public static readonly Color Waterspout = new Color(0.643137254901961,0.956862745098039,0.976470588235294,1.0,"Waterspout");
		public static readonly Color Wenge = new Color(0.392156862745098,0.329411764705882,0.32156862745098,1.0,"Wenge");
		public static readonly Color Wheat = new Color(0.96078431372549,0.870588235294118,0.701960784313725,1.0,"Wheat");
		public static readonly Color White = new Color(1,1,1,1.0,"White");
		public static readonly Color WhiteSmoke = new Color(0.96078431372549,0.96078431372549,0.96078431372549,1.0,"WhiteSmoke");
		public static readonly Color WildBlueYonder = new Color(0.635294117647059,0.67843137254902,0.815686274509804,1.0,"WildBlueYonder");
		public static readonly Color WildStrawberry = new Color(1,0.262745098039216,0.643137254901961,1.0,"WildStrawberry");
		public static readonly Color WildWatermelon = new Color(0.988235294117647,0.423529411764706,0.52156862745098,1.0,"WildWatermelon");
		public static readonly Color Wine = new Color(0.447058823529412,0.184313725490196,0.215686274509804,1.0,"Wine");
		public static readonly Color WineDregs = new Color(0.403921568627451,0.192156862745098,0.27843137254902,1.0,"WineDregs");
		public static readonly Color Wisteria = new Color(0.788235294117647,0.627450980392157,0.862745098039216,1.0,"Wisteria");
		public static readonly Color WoodBrown = new Color(0.756862745098039,0.603921568627451,0.419607843137255,1.0,"WoodBrown");
		public static readonly Color Xanadu = new Color(0.450980392156863,0.525490196078431,0.470588235294118,1.0,"Xanadu");
		public static readonly Color YaleBlue = new Color(0.0588235294117647,0.301960784313725,0.572549019607843,1.0,"YaleBlue");
		public static readonly Color Yellow = new Color(1,1,0,1.0,"Yellow");
		public static readonly Color YellowGreen = new Color(0.603921568627451,0.803921568627451,0.196078431372549,1.0,"YellowGreen");
		public static readonly Color YellowMunsell = new Color(0.937254901960784,0.8,0,1.0,"YellowMunsell");
		public static readonly Color YellowNcs = new Color(1,0.827450980392157,0,1.0,"YellowNcs");
		public static readonly Color YellowOrange = new Color(1,0.682352941176471,0.258823529411765,1.0,"YellowOrange");
		public static readonly Color YellowProcess = new Color(1,0.937254901960784,0,1.0,"YellowProcess");
		public static readonly Color YellowRyb = new Color(0.996078431372549,0.996078431372549,0.2,1.0,"YellowRyb");
		public static readonly Color Zaffre = new Color(0,0.0784313725490196,0.658823529411765,1.0,"Zaffre");
		public static readonly Color ZinnwalditeBrown = new Color(0.172549019607843,0.0862745098039216,0.0313725490196078,1.0,"ZinnwalditeBrown");
		#endregion
		        
		#region IXmlSerializable
		public void ReadXml(System.Xml.XmlReader reader)
        {
            string[] c = reader["Color"].Split(new char[] { ';' });            
            R = double.Parse(c[0]);
            G = double.Parse(c[1]);
            B = double.Parse(c[2]);
			A = double.Parse(c[3]);
        }
        public void WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteAttributeString("Color", this.ToString());
        }
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }
		#endregion

		public override string ToString()
		{
			if (!string.IsNullOrEmpty(Name))
				return Name;

			foreach (Color cr in ColorDic)
			{
				if (cr == this)
				{
					Name = cr.Name;
					return cr.Name;
				}
			}

			return string.Format("{0};{1};{2};{3}", R, G, B, A);
		}

        public static object Parse(string s)
        {
            return (Color)s;
        }
    }
}
