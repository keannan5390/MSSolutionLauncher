using System;
using System.Collections.Generic;
using System.Diagnostics;
using AppKit;
using Foundation;

namespace SolutionLauncher
{
    public static class LauncherUtil
    {
        static readonly Dictionary<SlnOpenWith, List<string>> APP_IDS = new Dictionary<SlnOpenWith, List<string>> {
            {
                SlnOpenWith.VisualStudio,
                new List<string> {
                    "com.microsoft.visual-studio"
                }
            },
        };

        const string MAC_OPEN = "/usr/bin/open";
        const string FIND_APP = "/usr/bin/mdfind";
        const string FIND_APP_ARGS = "kMDItemCFBundleIdentifier=\"{0}\"";


        public static void Launch()
        {
            Launch(null);
        }

        static string visualStudioBundleId = null;
        static string VisualStudioBundleId
        {
            get
            {
                if (visualStudioBundleId == null)
                    visualStudioBundleId = FirstBundleIdThatExists(APP_IDS[SlnOpenWith.VisualStudio].ToArray());
                return visualStudioBundleId;
            }
        }

        public static void Launch (string slnFile)
        {
            string appId = appId = VisualStudioBundleId;


			// If we didn't find a bundle ID from our choice or preferences
			if (string.IsNullOrEmpty(appId))
			{
				// First let's look for VS4Mac
				appId = VisualStudioBundleId;

				// If neither exist, we have no IDE installed so don't bother trying
				// to launch anything, just return.
				if (string.IsNullOrEmpty(appId))
				{
					Alert("No IDE Found", "Visual Studio for Mac could be found.  Please make sure it is installed in your Applications folder");
					return;
				}
			}

            var args = new List<string> {
                "-n",        // New Instance
                "-b", appId, // Open by bundle id
            };

            // See if we are asked to open a file
            if (!string.IsNullOrEmpty(slnFile))
            {
                args.Add("--args");
                args.Add("\"" + slnFile + "\""); // .sln file requested to open, quoted
            }

            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = MAC_OPEN,
                    Arguments = string.Join(" ", args),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    ErrorDialog = false,
                    //RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };

            var path = "";
            if (p.StartInfo.EnvironmentVariables.ContainsKey("PATH"))
                path = p.StartInfo.EnvironmentVariables["PATH"] ?? "";
            path = path.TrimEnd(':') + ":/usr/local/bin";
            path = path.TrimStart(':');

            if (!p.StartInfo.EnvironmentVariables.ContainsKey("PATH"))
                p.StartInfo.EnvironmentVariables.Add("PATH", path);
            else
                p.StartInfo.EnvironmentVariables["PATH"] = path;

            p.Start();
            p.WaitForExit();

            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();

            if (!string.IsNullOrEmpty(stdout))
                Console.WriteLine(stdout);

            if (!string.IsNullOrEmpty(stderr))
                Console.WriteLine(stderr);
        }


        public static string FirstBundleIdThatExists(params string[] possibleBundleIds)
        {
            foreach (var bundleId in possibleBundleIds)
            {
                var o = RunTask(FIND_APP, string.Format(FIND_APP_ARGS, bundleId));

                if (!string.IsNullOrWhiteSpace(o))
                    return bundleId;
            }
            return null;
        }

        public static string RunTask(string task, params string[] args)
        {
            var r = string.Empty;

            try
            {
                var pipeOut = new NSPipe();

                var t = new NSTask();
                t.LaunchPath = task;
                if (args != null)
                    t.Arguments = args;

                var path = "/usr/local/bin";
                var env = new NSMutableDictionary();
                env.SetValueForKey(new NSString(path), new NSString("PATH"));

                t.Environment = env;

                t.StandardOutput = pipeOut;
                t.StandardError = pipeOut;

                t.Launch();
                t.WaitUntilExit();
                //t.Release ();

                r = pipeOut.ReadHandle.ReadDataToEndOfFile().ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(task + " failed: " + ex);
            }
            return r;
        }

		public static void Alert(string title, string message)
		{
			var alert = new NSAlert();
			alert.InformativeText = message;
			alert.MessageText = title;

			alert.AddButton("OK");

			alert.RunModal();
		}
    }
}
