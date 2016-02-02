using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crow
{
    public class VerticalStack : GenericStack
    {
        public VerticalStack()
            : base()
        {
            Orientation = Crow.Orientation.Vertical;
        }

        [System.Xml.Serialization.XmlIgnore]
        public override Orientation Orientation
        {
            get { return Orientation.Vertical; }
            //set {  }
        }


    }
}
