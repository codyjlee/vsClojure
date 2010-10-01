﻿/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.ClojureExtension;
using Microsoft.ClojureExtension.Configuration;
using Microsoft.ClojureExtension.Project.Hierarchy;
using Microsoft.ClojureExtension.Project.Menu;
using Microsoft.ClojureExtension.Repl;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Project.Samples.CustomProject
{
    [Guid(PackageGuid)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [DefaultRegistryRoot("Software\\Microsoft\\VisualStudio\\10.0")]
    [ProvideObject(typeof (GeneralPropertyPage))]
    [ProvideProjectFactory(typeof (ClojureProjectFactory), "Clojure", "Clojure Project Files (*.cljproj);*.cljproj", "cljproj", "cljproj", @"Templates\Projects\Clojure", LanguageVsTemplate = "Clojure", NewProjectRequireNewFolderVsTemplate = false)]
    [ProvideProjectItem(typeof (ClojureProjectFactory), "Clojure Items", @"Templates\ProjectItems\Clojure", 500)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof (ReplToolWindow))]
    [ProvideOptionPage(typeof(FrameworkOptionsDialogPage), "Clojure", "Frameworks", 0, 0, true)]
    public sealed class ClojurePackage : ProjectPackage
    {
        public const string PackageGuid = "40953a10-3425-499c-8162-a90059792d13";
        //private CommandEvents _commandEvents;

        protected override void Initialize()
        {
            base.Initialize();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            ReplStorageProvider.Storage = new ReplStorage();
            createSettingsStore();
            intializeMenuItems();
            RegisterProjectFactory(new ClojureProjectFactory(this));
        }

        private void createSettingsStore()
        {
            IVsWritableSettingsStore settingsStore;
            var settingsManager = (IVsSettingsManager) GetService(typeof(SVsSettingsManager));
            settingsManager.GetWritableSettingsStore((uint)__VsSettingsScope.SettingsScope_UserSettings, out settingsStore);
            SettingsStoreProvider.Store = new SettingsStore("Clojure", settingsStore);
        }

        private void intializeMenuItems()
        {
            OleMenuCommandService menuCommandService = (OleMenuCommandService) GetService(typeof (IMenuCommandService));
            ReplToolWindow replToolWindow = (ReplToolWindow) FindToolWindow(typeof (ReplToolWindow), 0, true);
            IVsWindowFrame replToolWindowFrame = (IVsWindowFrame) replToolWindow.Frame;
            DTE2 dte = (DTE2)GetService(typeof(DTE));
           // _commandEvents = dte.Events.CommandEvents["ClassViewContentMenus.ClassViewMultiselectProjectreferencesItems.Properties", -1];
            //_commandEvents.BeforeExecute += ClojurePackage_BeforeExecute;

            menuCommandService.AddCommand(
                new MenuCommand(
                    (sender, args) =>
                        new LoadFilesIntoRepl(
                            new ReplWriter(),
                            new SelectedReplProvider(replToolWindow.TabControl, ReplStorageProvider.Storage),
                            new SelectedFilesProvider(dte.ToolWindows.SolutionExplorer),
                            replToolWindowFrame).Execute(),
                        new CommandID(Guids.GuidClojureExtensionCmdSet, CommandIds.LoadFileIntoActiveRepl)));

            menuCommandService.AddCommand(
                new MenuCommand(
                    (sender, args) =>
                        new LoadFilesIntoRepl(
                            new ReplWriter(),
                            new SelectedReplProvider(replToolWindow.TabControl, ReplStorageProvider.Storage),
                            new ProjectFilesProvider(
                                new SelectedProjectProvider(dte.Solution, dte.ToolWindows.SolutionExplorer)),
                            replToolWindowFrame).Execute(),
                    new CommandID(Guids.GuidClojureExtensionCmdSet, CommandIds.LoadProjectIntoActiveRepl)));

            menuCommandService.AddCommand(
                new MenuCommand(
                    (sender, args) =>
                        new StartReplUsingProjectVersion(
                            ReplStorageProvider.Storage,
                            replToolWindow.TabControl,
                            new ReplTabFactory(),
                            new ReplLauncher(),
                            replToolWindowFrame,
                            new GetFrameworkFromSelectedProject(
                                new SelectedProjectProvider(dte.Solution, dte.ToolWindows.SolutionExplorer),
                                SettingsStoreProvider.Store)).Execute(),
                         new CommandID(Guids.GuidClojureExtensionCmdSet, CommandIds.StartReplUsingProjectVersion)));
        }

        public override string ProductUserContext
        {
            get { return "ClojureProj"; }
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.FullName == args.Name)
                    return assembly;

            return null;
        }
    }
}