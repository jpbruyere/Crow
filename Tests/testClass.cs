
using System;

/* this is a block comment
 * on several lines
 */

namespace testRoslyn {
    public class testClass {
		public testClass self;
		int a = 0;
		int b;
		public string str = "this is a test string"

		[XmlIgnore]
		public string Str {
			get { return str; }
			set { str = value; }
		}
		[SecuritySafeCritical]
        public testClass (int _a, int _b) {
			a = _a;
        }
    }
}
