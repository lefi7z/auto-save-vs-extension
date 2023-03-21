using System;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

using EnvDTE;


[assembly: InternalsVisibleTo("AutoSaveFileTests")]
namespace AutoSaveFile
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(AutoSaveFilePackage.PackageGuidString)]
    [ProvideService(typeof(AutoSaveFilePackage), IsAsyncQueryable = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasMultipleProjects_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasSingleProject_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(OptionPageGrid), "Auto Save File", "General", 0, 0, true)]
    public sealed class AutoSaveFilePackage : AsyncPackage
    {
        /// <summary>
        /// AutoSaveFilePackage GUID string.
        /// </summary>
        public const string PackageGuidString = "45762a8e-f0a3-45d6-a93c-3d3a770229cf";

        public string PackageName => nameof(AutoSaveFilePackage);

        public OptionPageGrid Options => (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));

        /// <summary>
        /// Initialisation of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the Initialisation code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for Initialisation cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package Initialisation, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            // When Initialised asynchronously, the current thread may be a background thread at this point.
            // Do any Initialisation that requires the UI thread after switching to the UI thread.
            //await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await base.InitializeAsync(cancellationToken, progress);

            DTE dte = await GetServiceAsync(typeof(DTE)) as DTE;
            if (dte != null)
            {
                dte.Events.WindowEvents.WindowActivated += OnWindowActivated;
                //dte.Events.TextEditorEvents.LineChanged += OnLineChanged;

                System.Windows.Application.Current.Deactivated += OnAppDeactivated;
                //System.Windows.Application.Current.Exit += OnDeactivated;
            }

            IVsOutputWindow outWindow = GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            Guid guid = new Guid(PackageGuidString);
            outWindow.CreatePane(ref guid, PackageName, fInitVisible: 1, fClearWithSolution: 1);
            outWindow.GetPane(ref guid, out customPane);
            //customPane.Activate();  // brings this pane into view
        }

        IVsOutputWindowPane customPane;

        private bool SaveMaybe(Window window)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (window == null)
                return false;

            Document doc = window.Document;
            if (doc == null)
                return false;

            if (doc.Saved)
                return false;

            if (doc.ReadOnly)
            {
                Log($"skipping read-only file {doc.FullName}");
                return false;
            }

            foreach (string ext in Options.IgnoredFileTypes.Split(',', ';', ':'))
            {
                if (Options.UseRegex)
                {
                    if (Regex.IsMatch(doc.FullName, ext))
                        return false;
                }
                else if (doc.FullName.EndsWith(ext))
                    return false;
            }

            Log($"saving {doc.FullName}");
            doc.Save();
            return true;
        }

        private void Log(string msg)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            customPane.OutputString(msg + "\r\n");
        }

        #region events

        private void OnAppDeactivated(object sender, System.EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (Options.SaveAllFilesWhenVSLosesFocus)
            {
                try
                {
                    DTE dte = GetService(typeof(DTE)) as DTE;
                    if (dte != null)
                    {
                        foreach (object window in dte.Windows)
                        {
                            if (window is Window win)
                                SaveMaybe(win);
                        }
                        // shortcut (wenn man davon ausgeht, dass alle inaktiven Fenster
                        // durch einen lost-focus eh gespeichert worden sind):
                        //SaveMaybe(dte.ActiveWindow);
                    }
                }
                catch (Exception exc)
                {
                    Log(exc.ToString());
                }
            }
        }

        private void OnWindowActivated(Window gotFocus, Window lostFocus)
        {
            SaveMaybe(lostFocus);
        }

        #endregion

    }
}
