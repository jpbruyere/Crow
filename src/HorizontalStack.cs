using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace go
{
    public class HorizontalStack : GenericStack
    {
        public HorizontalStack()
            : base()
        {
            Orientation = go.Orientation.Horizontal;
        }
    }
}
