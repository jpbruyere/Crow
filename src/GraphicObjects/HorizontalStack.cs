using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Crow
{
    public class HorizontalStack : GenericStack
    {
        public HorizontalStack()
            : base()
        {
            Orientation = Crow.Orientation.Horizontal;
        }

        [XmlIgnore]
        public override Orientation Orientation
        {
            get { return Orientation.Horizontal; }
        }
    }
}
