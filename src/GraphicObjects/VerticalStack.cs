using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace go
{
    public class VerticalStack : GenericStack
    {
        public VerticalStack()
            : base()
        {
            Orientation = go.Orientation.Vertical;
        }

        [System.Xml.Serialization.XmlIgnore]
        public override Orientation Orientation
        {
            get { return Orientation.Vertical; }
            //set {  }
        }


    }
}
