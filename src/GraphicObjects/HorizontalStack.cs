using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace go
{
    public class HorizontalStack : GenericStack
    {
        public HorizontalStack()
            : base()
        {
            Orientation = go.Orientation.Horizontal;
        }

        [XmlIgnore]
        public override Orientation Orientation
        {
            get { return Orientation.Horizontal; }
            //set {  }
        }



    }
}
