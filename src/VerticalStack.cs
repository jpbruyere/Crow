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
    }
}
