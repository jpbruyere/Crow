using System;
using System.Xml.Serialization;
using System.Reflection;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Diagnostics;

namespace Crow
{
    public class Container : PrivateContainer, IXmlSerializable
    {
		#region CTOR
		public Container()
			: base()
		{
		}
		public Container(Rectangle _bounds)
			: base(_bounds)
		{
		}
		#endregion

		[XmlIgnore]
		public GraphicObject Child {
			get { return child; }
			set { child = value; }
		}
		public virtual void SetChild(GraphicObject _child)
		{
			Debug.WriteLine ("container setChild");
			base.SetChild (_child);
		}

		#region IXmlSerializable

        public override System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }
        public override void ReadXml(System.Xml.XmlReader reader)
        {
			//only read attributes in GraphicObject IXmlReader implementation
            base.ReadXml(reader);


            using (System.Xml.XmlReader subTree = reader.ReadSubtree())
            {
                subTree.Read(); //skip current node
                subTree.Read(); //read first child

                if (!subTree.IsStartElement())
                    return;

                Type t = Type.GetType("Crow." + subTree.Name);
				if (t == null) {
					Assembly a = Assembly.GetEntryAssembly ();
					foreach (Type expT in a.GetExportedTypes ()) {
						if (expT.Name == subTree.Name)
							t = expT;
					}
				}
				GraphicObject go = (GraphicObject)Activator.CreateInstance(t);

				(go as IXmlSerializable).ReadXml(subTree);

				SetChild(go);

                subTree.Read();//closing tag
            }
        }
        public override void WriteXml(System.Xml.XmlWriter writer)
        {
            base.WriteXml(writer);

            if (Child == null)
                return;

            writer.WriteStartElement(Child.GetType().Name);
            (Child as IXmlSerializable).WriteXml(writer);
            writer.WriteEndElement();
        }
    
		#endregion
	}
}

