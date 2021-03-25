// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)


namespace Crow
{
	/// <summary>
	/// group control stacking its children horizontally
	/// </summary>
	public class HorizontalStack : GenericStack
    {
		#region CTOR
		protected HorizontalStack () : base(){}
		public HorizontalStack(Interface iface) : base(iface)
        {            
        }
		#endregion

        [XmlIgnore]
        public override Orientation Orientation
        {
            get => Orientation.Horizontal;
            set { base.Orientation = Orientation.Horizontal; }
        }
    }
}
