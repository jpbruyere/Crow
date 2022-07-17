// Copyright (c) 2015-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using MiscUtil;
using System.Reflection;

namespace vke
{
	public class Animation<T> : Animation
	{
		public delegate T GetterDelegate();
		public delegate void SetterDelegate(T value);

		public T TargetValue;
		T initialValue;
		T zero;
		public T Step;
		public bool Cycle;

		protected GetterDelegate getValue;
		protected SetterDelegate setValue;

		#region CTOR
		public Animation(Object instance, string _propertyName)
		{
			propertyName = _propertyName;
			AnimatedInstance = instance;
			PropertyInfo pi = instance.GetType().GetProperty(propertyName);
			getValue = (GetterDelegate)Delegate.CreateDelegate(typeof(GetterDelegate), instance, pi.GetGetMethod());
			setValue = (SetterDelegate)Delegate.CreateDelegate(typeof(SetterDelegate), instance, pi.GetSetMethod());
		}
		public Animation(Object instance, string _propertyName, T Target, T step)
		{
			propertyName = _propertyName;
			AnimatedInstance = instance;
			PropertyInfo pi = instance.GetType().GetProperty(propertyName);
			getValue = (GetterDelegate)Delegate.CreateDelegate(typeof(GetterDelegate), instance, pi.GetGetMethod());
			setValue = (SetterDelegate)Delegate.CreateDelegate(typeof(SetterDelegate), instance, pi.GetSetMethod());

			TargetValue = Target;

			T value = getValue();
			initialValue = value;
			Type t = typeof(T);

			if (t.IsPrimitive) {
				Step = (T)Convert.ChangeType (step, t);
				zero = (T)Convert.ChangeType (0, t);
			}else {
				Step = (T)Activator.CreateInstance (typeof(T), new Object[] { step });
				zero = (T)Activator.CreateInstance (typeof(T), 0f);
			}
			T test = (T)Operator.SubtractAlternative (value, TargetValue);

			if (Operator.LessThan(test, zero))
			{
				if (Operator.LessThan (Step, zero))
					Step = Operator.Negate (Step);
			}
			else if (Operator.GreaterThan(Step, zero))
				Step = Operator.Negate (Step);
		}
		#endregion

		public override void Process()
		{
			T value = getValue();

			//Debug.WriteLine ("Anim: {0} <= {1}", value, this.ToString ());

			if (Operator.GreaterThan(Step, zero))
			{
				value = Operator.Add (value, Step);
				setValue(value);
				//Debug.WriteLine(value);
				if (Operator.GreaterThan(Operator.Subtract(TargetValue, value), zero))
					return;
			}
			else
			{
				value = Operator.Add (value, Step);
				setValue(value);

				if (Operator.LessThan(Operator.Subtract(TargetValue, value), zero))
					return;
			}

			if (Cycle) {
				Step = Operator.Negate (Step);
				TargetValue = initialValue;
				Cycle = false;
				return;
			}

			setValue(TargetValue);
			AnimationList.Remove(this);

			RaiseAnimationFinishedEvent ();
		}

		public override string ToString ()
		{
			return string.Format ("{0}:->{1}:{2}",base.ToString(),TargetValue,Step);
		}
	}

}

