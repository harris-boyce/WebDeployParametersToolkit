﻿//------------------------------------------------------------------------------
// <copyright file="AddParameterizationTargetCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using WebDeployParametersToolkit.Extensions;

namespace WebDeployParametersToolkit
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class AddParameterizationTargetCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 260;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("b451bf5d-476a-43b7-8a00-11671601fdaa");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddParameterizationTargetCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private AddParameterizationTargetCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            this.package = package;

            var commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new OleMenuCommand(MenuItemCallback, menuCommandID);
                menuItem.BeforeQueryStatus += MenuItem_BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static AddParameterizationTargetCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new AddParameterizationTargetCommand(package);
        }

        private void MenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menuItem = (OleMenuCommand)sender;
            menuItem.Visible = false;

            SolutionExplorerExtensions.LoadSelectedItemPath();

            if (NeedsInitialization())
            {
                menuItem.Visible = true;
            }
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var filePath = SolutionExplorerExtensions.SelectedItemPath;

            if (!string.IsNullOrEmpty(filePath) && "Parameters.xml".Equals(Path.GetFileName(SolutionExplorerExtensions.SelectedItemPath), StringComparison.OrdinalIgnoreCase))
            {
                var projectFullName = VSPackage.DteInstance.Solution.FindProjectItem(filePath).ContainingProject.FullName;
                var project = new ParameterizationProject(projectFullName);
                project.Initialize();

                var parent = VSPackage.DteInstance.Solution.FindProjectItem(filePath);

                foreach (var item in parent.ProjectItems)
                {
                    var child = item as ProjectItem;
                    if (child != null)
                    {
                        child.Properties.Item("ItemType").Value = "Parameterization";
                    }
                }
            }
        }

        private bool NeedsInitialization()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var filePath = SolutionExplorerExtensions.SelectedItemPath;

            if (!string.IsNullOrEmpty(filePath) && "Parameters.xml".Equals(Path.GetFileName(SolutionExplorerExtensions.SelectedItemPath), StringComparison.OrdinalIgnoreCase))
            {
                var projectFullName = VSPackage.DteInstance.Solution.FindProjectItem(filePath).ContainingProject.FullName;
                var project = new ParameterizationProject(projectFullName);
                return project.NeedsInitialization;
            }

            return false;
        }
    }
}
