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

		CrowIDE ide;

		public IdeLogger (CrowIDE ide)
		{
			this.ide = ide;
		}

		public void Initialize (IEventSource eventSource)
		{
			eventSource.BuildStarted += EventSource_BuildStarted; ;

			switch (Verbosity) {
			case LoggerVerbosity.Quiet:
				eventSource.BuildFinished += EventSource_BuildFinished;
				break;
			case LoggerVerbosity.Minimal:
				eventSource.MessageRaised += (sender, e) => { if (e.Importance == MessageImportance.High) ide.BuildEvents.Add (e); };
				break;
			case LoggerVerbosity.Normal:
				eventSource.MessageRaised += (sender, e) => { if (e.Importance != MessageImportance.Low) ide.BuildEvents.Add (e); };
				eventSource.ProjectStarted += EventSource_ProjectStarted;
				eventSource.ProjectFinished += EventSource_ProjectFinished;
				break;
			case LoggerVerbosity.Detailed:
				break;
			case LoggerVerbosity.Diagnostic:
				eventSource.AnyEventRaised += (sender, e) => { ide.BuildEvents.Add (e); };
				eventSource.MessageRaised += (sender, e) => { ide.BuildEvents.Add (e); };
				break;			
			}

			eventSource.BuildFinished += EventSource_BuildFinished;

			eventSource.ErrorRaised += EventSource_ErrorRaised;
		}

		void EventSource_BuildStarted (object sender, BuildStartedEventArgs e)
		{
			ide.BuildEvents.Clear ();
			ide.BuildEvents.Add (e);
		}
		void EventSource_BuildFinished (object sender, BuildFinishedEventArgs e)
		{
			ide.BuildEvents.Add (e);
		}

		void EventSource_ProjectStarted (object sender, ProjectStartedEventArgs e)
		{
			ide.BuildEvents.Add (e);
		}
		void EventSource_ProjectFinished (object sender, ProjectFinishedEventArgs e)
		{
			ide.BuildEvents.Add (e);
		}
		void EventSource_ErrorRaised (object sender, BuildErrorEventArgs e)
		{
			ide.BuildEvents.Add (e);
		}



		public void Shutdown ()
		{
		}
	}
}
