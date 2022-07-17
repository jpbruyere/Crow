// Copyright (c) 2015-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

namespace vke
{
	public delegate void AnimationEventHandler(Animation a);

    public delegate float GetterDelegate();
    public delegate void SetterDelegate(float value);

    public class Animation
    {
		public event AnimationEventHandler AnimationFinished;

		public static Random random = new Random ();
		public static int DelayMs = 0;

        protected GetterDelegate getValue;
        protected SetterDelegate setValue;

        public string propertyName;

        protected Stopwatch timer = new Stopwatch();
        protected int delayStartMs = 0;
		/// <summary>
		/// Delay before firing ZnimationFinished event.
		/// </summary>
		protected int delayFinishMs = 0;
        public static List<Animation> AnimationList = new List<Animation>();
		public static bool HasAnimations => AnimationList.Count > 0;
        //public FieldInfo member;
        public Object AnimatedInstance;

		#region CTOR
		public Animation (){}
		public Animation(Object instance, string _propertyName)
		{
			propertyName = _propertyName;
			AnimatedInstance = instance;
			PropertyInfo pi = instance.GetType().GetProperty(propertyName);
			try {
				getValue = (GetterDelegate)Delegate.CreateDelegate(typeof(GetterDelegate), instance, pi.GetGetMethod());
				setValue = (SetterDelegate)Delegate.CreateDelegate(typeof(SetterDelegate), instance, pi.GetSetMethod());
			} catch (Exception ex) {
				Debug.WriteLine (ex.ToString ());
			}
		}
		#endregion

		public static void StartAnimation(Animation a, int delayMs = 0, AnimationEventHandler OnEnd = null)
        {
			lock (AnimationList) {
				Animation aa = null;
				if (Animation.GetAnimation (a.AnimatedInstance, a.propertyName, ref aa)) {
					aa.CancelAnimation ();
				}

				//a.AnimationFinished += onAnimationFinished;

				a.AnimationFinished += OnEnd;
				a.delayStartMs = delayMs + DelayMs;


				if (a.delayStartMs > 0)
					a.timer.Start ();

				AnimationList.Add (a);
			}

        }

        static Stack<Animation> anims = new Stack<Animation>();
		static int frame = 0;
        public static void ProcessAnimations()
        {
			frame++;

//			#region FLYING anim
//			if (frame % 20 == 0){
//				foreach (Player p in MagicEngine.CurrentEngine.Players) {
//					foreach (CardInstance c in p.InPlay.Cards.Where(ci => ci.HasAbility(AbilityEnum.Flying) && ci.z < 0.4f)) {
//
//					}
//				}
//			}
//			#endregion
            //Stopwatch animationTime = new Stopwatch();
            //animationTime.Start();

			const int maxAnim = 200000;
			int count = 0;


			lock (AnimationList) {
				if (anims.Count == 0)
					anims = new Stack<Animation> (AnimationList);
			}

			while (anims.Count > 0 && count < maxAnim) {
				Animation a = anims.Pop ();
				if (a == null)
					continue;
				if (a.timer.IsRunning) {
					if (a.timer.ElapsedMilliseconds > a.delayStartMs)
						a.timer.Stop ();
					else
						continue;
				}

				a.Process ();
				count++;
			}

            //animationTime.Stop();
            //Debug.WriteLine("animation: {0} ticks \t {1} ms ", animationTime.ElapsedTicks,animationTime.ElapsedMilliseconds);
        }
        public static bool GetAnimation(object instance, string PropertyName, ref Animation a)
        {
			for (int i = 0; i < AnimationList.Count; i++) {
				Animation anim = AnimationList [i];
				if (anim == null) {
					continue;
				}
				if (anim.AnimatedInstance == instance && anim.propertyName == PropertyName) {
					a = anim;
					return true;
				}
			}

            return false;
        }
		public virtual void Process () {}
        public void CancelAnimation()
        {
			//Debug.WriteLine("Cancel anim: " + this.ToString());
            AnimationList.Remove(this);
        }
		public void RaiseAnimationFinishedEvent()
		{
			if (AnimationFinished != null)
				AnimationFinished (this);
		}

		public static void onAnimationFinished(Animation a)
		{
			Debug.WriteLine ("\t\tAnimation finished: " + a.ToString ());
		}
    }
}
