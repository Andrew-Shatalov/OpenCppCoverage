﻿using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.VSSDK.Tools.VsIdeTesting;
using OpenCppCoverage.VSPackage;
using System;
using System.Collections;
using System.IO;
using System.Linq;

namespace VSPackage_IntegrationTests
{
    class TestHelpers
    {
        static public readonly string CppConsoleApplication = @"CppConsoleApplication\CppConsoleApplication.vcxproj";
        static public readonly string CppConsoleApplication2 = @"CppConsoleApplication2\CppConsoleApplication2.vcxproj";
        static public readonly string CSharpConsoleApplication = @"CSharpConsoleApplication\CSharpConsoleApplication.csproj";
        static public readonly string ApplicationName = "CppConsoleApplication.exe";
        static public readonly string ApplicationName2 = "CppConsoleApplication2.exe";
        
        //---------------------------------------------------------------------
        static public string GetOpenCppCoverageMessage()
        {                        
            var uiShell = GetService<IVsUIShell>();

            using (var dialogBoxMessageRetriever = new DialogBoxMessageRetriever(uiShell, TimeSpan.FromSeconds(10)))
            {
                ExecuteOpenCppCoverage();
                return dialogBoxMessageRetriever.GetMessage();
            }
        }

        //---------------------------------------------------------------------
        static public void OpenDefaultSolution(params string[] startupProjects)
        {
            OpenDefaultSolution(startupProjects, ConfigurationName.Debug, PlatFormName.Win32);
        }

        //---------------------------------------------------------------------
        static public EnvDTE80.SolutionConfiguration2 OpenDefaultSolution(
            string startupProjects,
            ConfigurationName configurationName,
            PlatFormName platformName)
        {
            return OpenDefaultSolution(new string[] { startupProjects }, configurationName, platformName);            
        }

        //---------------------------------------------------------------------
        static public T GetService<T>() where T : class
        {
            var service = VsIdeTestHostContext.ServiceProvider.GetService(typeof(T)) as T;
            if (service == null)
                throw new Exception("Service is null");

            return service;
        }

        //---------------------------------------------------------------------
        public static void ExecuteOpenCppCoverage()
        {
            object Customin = null;
            object Customout = null;
            var commandGuid = OpenCppCoverage.VSPackage.GuidList.guidVSPackageCmdSet;
            string guidString = commandGuid.ToString("B").ToUpper();
            int cmdId = (int)OpenCppCoverage.VSPackage.PkgCmdIDList.RunOpenCppCoverageCommand;
            DTE dte = VsIdeTestHostContext.Dte;

            dte.Commands.Raise(guidString, cmdId, ref Customin, ref Customout);
        }

        //---------------------------------------------------------------------
        public static void WaitForActiveDocument(string documentCaption, TimeSpan timeout)
        {                     
            Wait(timeout, "Cannot get html coverage", () =>
                {
                    var actionWindows = VsIdeTestHostContext.Dte.ActiveWindow;

                    return actionWindows != null && actionWindows.Caption == ApplicationName;                    
                });
        }

        //---------------------------------------------------------------------
        public static string GetOpenCppCoverageOutput()
        {
            var dte2 = (EnvDTE80.DTE2)VsIdeTestHostContext.Dte;
            var panes = dte2.ToolWindows.OutputWindow.OutputWindowPanes.Cast<OutputWindowPane>();
            var openCppCoveragePane = panes.First( p => Guid.Parse(p.Guid) == OutputWindowWriter.OpenCppCoverageOutputPaneGuid);
            var textDocument = openCppCoveragePane.TextDocument;
            var editPoint = textDocument.CreateEditPoint();

            return editPoint.GetText(textDocument.EndPoint);                       
        }

        //---------------------------------------------------------------------
        public static void Wait(TimeSpan timeout, string timeoutMessage, Func<bool> action)
        {
            const int partCount = 50;
            var smallTimeout = new TimeSpan(timeout.Ticks / partCount);
                
            for (int nbTry = 0; nbTry < partCount; ++nbTry)
            {
                if (action())
                    return;
                System.Threading.Thread.Sleep(smallTimeout);
            }

            throw new Exception(timeoutMessage);
        }

        //---------------------------------------------------------------------
        public static string GetIntegrationTestsSolutionFolder()
        {
            var currentLocation = typeof(TestHelpers).Assembly.Location;
            var currentDirectory = Path.GetDirectoryName(currentLocation);
            return Path.Combine(currentDirectory, "IntegrationTestsSolution");
        }

        //---------------------------------------------------------------------
        static EnvDTE80.SolutionConfiguration2 OpenDefaultSolution(
            string[] startupProjects,
            ConfigurationName configurationName,
            PlatFormName platformName)
        {
            OpenDefaultSolution();
            var startupProjectObjects = new object[startupProjects.Length];
            Array.Copy(startupProjects, startupProjectObjects, startupProjectObjects.Length);
            VsIdeTestHostContext.Dte.Solution.SolutionBuild.StartupProjects = startupProjectObjects;
            return SolutionConfigurationHelpers.SetActiveSolutionConfiguration(configurationName, platformName);
        }

        //---------------------------------------------------------------------
        static void OpenDefaultSolution()
        {
            var solutionService = GetService<IVsSolution>();
            var solutionPath = Path.Combine(GetIntegrationTestsSolutionFolder(), "IntegrationTestsSolution.sln");
            
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(
                solutionService.OpenSolutionFile((uint)__VSSLNOPENOPTIONS.SLNOPENOPT_Silent, solutionPath));
            WaitForSolutionLoading(TimeSpan.FromSeconds(10));
        }
        
        //---------------------------------------------------------------------
        static void WaitForSolutionLoading(TimeSpan timeout)
        {
            TestHelpers.Wait(timeout, "Solution not loaded", () =>
                {                    
                    foreach (Project p in VsIdeTestHostContext.Dte.Solution.Projects)
                    {
                        if (p.Object == null)
                            return false;
                    }

                    return true;
                });
        }        
    }
}