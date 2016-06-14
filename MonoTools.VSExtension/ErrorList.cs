using System;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;

namespace MonoTools.VSExtension {

	public static class ErrorList {

		private static ErrorListProvider provider;
		private static IVsSolution solutions;

		public static void Initialize(IServiceProvider serviceProvider) {
			provider = new ErrorListProvider(serviceProvider);
			solutions = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
		}

		public static void AddError(string message, string key, string document, int line, int column, string project) {
			AddTask(message, TaskErrorCategory.Error, document, line, column, key, project);
		}

		public static void AddWarning(string message, string key, string document, int line, int column, string project) {
			AddTask(message, TaskErrorCategory.Warning, document, line, column, key, project);
		}

		public static void AddMessage(string message, string document, int line, int column, string project) {
			AddTask(message, TaskErrorCategory.Message, document, line, column, null, project);
		}

		private static void AddTask(string message, TaskErrorCategory category, string document, int line, int column, string key = null, string project = null) {
			IVsHierarchy h = null;
			if (project != null && solutions.GetProjectOfUniqueName(project, out h) == 0) ;

			var image = (int)category;
			var err = new ErrorTask {
				Category = TaskCategory.BuildCompile,
				ErrorCategory = category,
				Text = message,
				Document = document,
				Column = column,
				Line = line,
				ImageIndex = image
			};
			if (key != null) err.HelpKeyword = key;
			if (h != null) err.HierarchyItem = h;
			/* TODO 
			err.Navigate += (sender, args) => {
				var e = (ErrorTask)sender;
			} */
			provider.Tasks.Add(err);
		}

		public static void Clear() {
			provider.Tasks.Clear();
		}
	}
}