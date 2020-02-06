// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using Crow;
using Microsoft.Build.Framework;

namespace Crow.Coding
{
	public class IdeLogger : ILogger
	{
		public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Normal;
		public string Parameters { get; set; }

		SolutionView solution;

		public IdeLogger (SolutionView solution)
		{
			this.solution = solution;
		}

		public void Initialize (IEventSource eventSource)
		{
			eventSource.BuildStarted += EventSource_BuildStarted; ;

			switch (Verbosity) {
			case LoggerVerbosity.Quiet:
				eventSource.BuildFinished += EventSource_BuildFinished;
				break;
			case LoggerVerbosity.Minimal:
				eventSource.MessageRaised += (sender, e) => { if (e.Importance == MessageImportance.High) solution.BuildEvents.Add (e); };
				break;
			case LoggerVerbosity.Normal:
				eventSource.MessageRaised += (sender, e) => { if (e.Importance != MessageImportance.Low) solution.BuildEvents.Add (e); };
				break;
			case LoggerVerbosity.Detailed:
			case LoggerVerbosity.Diagnostic:
				eventSource.AnyEventRaised += (sender, e) => { solution.BuildEvents.Add (e); };
				eventSource.MessageRaised += (sender, e) => { solution.BuildEvents.Add (e); };
				break;			
			}

			eventSource.BuildFinished+= (sender, e) => { solution.BuildEvents.Add (e); };

			eventSource.ProjectStarted += EventSource_ProjectStarted;
			eventSource.ProjectFinished += EventSource_ProjectFinished;
			eventSource.ErrorRaised += EventSource_ErrorRaised;
		}

		void EventSource_BuildStarted (object sender, BuildStartedEventArgs e)
		{
			solution.BuildEvents.Clear ();
		}
		void EventSource_BuildFinished (object sender, BuildFinishedEventArgs e)
		{
		}

		void EventSource_ProjectStarted (object sender, ProjectStartedEventArgs e)
		{
		}
		void EventSource_ProjectFinished (object sender, ProjectFinishedEventArgs e)
		{
		}
		void EventSource_ErrorRaised (object sender, BuildErrorEventArgs e)
		{
			solution.BuildEvents.Add (e);
		}



		public void Shutdown ()
		{
		}
	}
}
