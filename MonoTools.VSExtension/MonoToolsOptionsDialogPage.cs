namespace MonoTools
{
	using System;
	using System.ComponentModel;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.Shell;

	[ClassInterface(ClassInterfaceType.AutoDual)]
	[CLSCompliant(false), ComVisible(true)]
	[System.ComponentModel.DesignerCategory("")]
	public class MonoToolsOptionsDialogPage : DialogPage
	{
		[Category("Mono Runtime Settings")]
		[DisplayName("Installation Path")]
		[Description("Installation Path")]
		public string MonoInstallationPath { get; set; }

		[Category("Mono Debugger Settings")]
		[DisplayName("Mono Debugger Ports")]
		[Description("Mono debugger ports (for example \"12002;12003;12004\").\nIf you set the ports here, you must pass them as argument to MonoDebugger.exe")]
		public string MonoDebuggerPorts { get; set; }

	}
}