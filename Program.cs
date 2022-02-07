using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Resources;
using System.Reflection;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

[assembly: AssemblyVersion("0.0.1.0")]
[assembly: AssemblyTitle("LSM")]
[assembly: AssemblyCompany("")]
[assembly: NeutralResourcesLanguage("en")]
[assembly: AssemblyFileVersion("0.0.1.0")]
[assembly: AssemblyProduct("LSM")]
[assembly: AssemblyDescription("This is a test version!")]
[assembly: AssemblyCopyright("")]
[assembly: AssemblyTrademark("")]

namespace Lan_School_Monitor
{
	public class setting
	{
		public setting() //public setting(Toggle_WIFI_ = false, Notify = true, Hide = true)
		{
			Toggle_WIFI = false;
			Notify = true;
			Hide = true;
			CloseTaskManager = false;
		}
		public bool Toggle_WIFI { get; set; }
		public bool Notify { get; set; }
		public bool Hide { get; set; }
		public bool CloseTaskManager { get; set;}
	}


	public class BSOD
	{
		[DllImport("ntdll.dll")]
		public static extern uint RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool PreviousValue);

		[DllImport("ntdll.dll")]
		public static extern uint NtRaiseHardError(uint ErrorStatus, uint NumberOfParameters, uint UnicodeStringParameterMask, IntPtr Parameters, uint ValidResponseOption, out uint Response);

		public static void BSOD_()
		{
			Boolean t1;
			uint t2;
			RtlAdjustPrivilege(19, true, false, out t1);
			NtRaiseHardError(0xc0000022, 0, 0, IntPtr.Zero, 6, out t2);
		}
	}
		
	public class Program
	{
		public static setting Settings = new setting();
		public static List<Process> ProcessList = new List<Process>();
		public static List<PerformanceCounter> instances = new List<PerformanceCounter>();


		#region Form Designer
		partial class Form1
		{
			/// <summary>
			/// Required designer variable.
			/// </summary>
			private System.ComponentModel.IContainer components = null;

			/// <summary>
			/// Clean up any resources being used.
			/// </summary>
			/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
			protected override void Dispose(bool disposing)
			{
				if (disposing && (components != null))
				{
					components.Dispose();
				}
				base.Dispose(disposing);
			}

			#region Windows Form Designer generated code

			/// <summary>
			/// Required method for Designer support - do not modify
			/// the contents of this method with the code editor.
			/// </summary>
			private void InitializeComponent()
			{
				this.components = new System.ComponentModel.Container();
				this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
				this.ClientSize = new System.Drawing.Size(800, 450);
				this.Text = "Form1";
			}

			#endregion
		}
		#endregion

		public partial class Form1 : Form
		{
			NotifyIcon notifyicon;
			Thread Lan_School_Monitor_Thread;
			
			public Form1()
			{
				InitializeComponent();
				//Application.ApplicationExit += new EventHandler(this.OnApplicationExit);
				// Create an event handler that will be called when the application exits via Ctrl-C or Alt-F4.
				Application.ApplicationExit += new EventHandler(this.OnApplicationExit);

				notifyicon = new NotifyIcon();
				notifyicon.Icon = SystemIcons.Application;
				notifyicon.Visible = true;

				// Create the context menu items and add them to the notication tray icon.
				//MenuItem programNameMenuItem = new MenuItem("Program Name");
				MenuItem quitMenuItem = new MenuItem("Quit");
				MenuItem BSODMenuItem = new MenuItem("");
				if (!Settings.Hide) { BSODMenuItem.Text = "BSOD"; }
				ContextMenu contextMenu = new ContextMenu();
				contextMenu.MenuItems.Add(BSODMenuItem);
				contextMenu.MenuItems.Add(quitMenuItem);
				notifyicon.ContextMenu = contextMenu;

				quitMenuItem.Click += quitMenuItem_Click;
				BSODMenuItem.Click += BSODMenuItem_Click;


				// Hide the form.
				this.WindowState = FormWindowState.Minimized;
				this.ShowInTaskbar = false;

				Lan_School_Monitor_Thread = new Thread(new ThreadStart(Monitor_Thread));
				Lan_School_Monitor_Thread.Start();
			}

			void OnApplicationExit(object sender, EventArgs e)
			{
				notifyicon.Dispose();
			}

			void quitMenuItem_Click(object sender,EventArgs e)
			{
				Lan_School_Monitor_Thread.Abort();
				notifyicon.Dispose();
				this.Close();
			}

			void BSODMenuItem_Click(object sender, EventArgs e)
			{
				BSOD.BSOD_();
				Lan_School_Monitor_Thread.Abort();
				notifyicon.Dispose();
				this.Close();
			}

			void Monitor_Thread()
			{
				DateTime last_notified = DateTime.Now;
				this.WindowState = FormWindowState.Minimized;
				this.ShowInTaskbar = false;
				try
				{
					//
					Console.WriteLine("Monitoring...");
					foreach (Process p in ProcessList)
					{
						instances.Add(new PerformanceCounter("Process", "IO Other Bytes/sec", p.ProcessName));
						//https://docs.microsoft.com/en-us/previous-versions/windows/it-pro/windows-server-2003/cc780836(v=ws.10)?redirectedfrom=MSDN
						//instances.Add(new PerformanceCounter("Network Interface", "Bytes Total/sec", p.ProcessName));
					}
					//PerformanceCounter net = new PerformanceCounter("Process", "IO Read Bytes/sec", ProcessList[0].ProcessName);
					while(true)
					{
						foreach (PerformanceCounter net in instances)
						{
							float value = net.NextValue();
							//Console.WriteLine("Process: {1} Net: {0}", net.NextValue(), ProcessList[0].ProcessName);
							if(value != 0.0)
							{
								notifyicon.Icon = SystemIcons.Warning;
								// Notify if 8 seconds have passed since the last notification.
								if(DateTime.Now.Subtract(last_notified).TotalSeconds > 8)
								{
									last_notified = DateTime.Now;
									// Make the message as a string.
									string message = String.Format("Network activity detected on \"{0}\" at {1}.", net.InstanceName, DateTime.Now.ToString());
									if(Settings.Notify){	notifyicon.ShowBalloonTip(1000, "Network Usage", message, ToolTipIcon.Info);	}
									Console.WriteLine(message);
									// If 'CloseTaskManager' is true, close task manager.
									if(Settings.CloseTaskManager)
									{
										Process[] task_managers = Process.GetProcessesByName("taskmgr");
										foreach(Process task_manager in task_managers)
										{
											Console.WriteLine("Closing task manager.");
											task_manager.Kill();
										}
									}
									if(Settings.Toggle_WIFI)
									{
										// execute 'netsh wlan disconnect' command.
										Process _process = new Process();
										_process.StartInfo.FileName = "cmd.exe";
										_process.StartInfo.Arguments = "/c netsh wlan disconnect";
										_process.StartInfo.UseShellExecute = false;
										_process.StartInfo.RedirectStandardOutput = true;
										_process.StartInfo.CreateNoWindow = true;
										_process.Start();

										// Read the output (or the error)
										//string output = _process.StandardOutput.ReadToEnd();

										// Wait for the process to finish.
										//_process.WaitForExit();
										//Console.WriteLine(output);
									}
								}
							}
							else
							{ notifyicon.Icon = SystemIcons.Application; }
						}
						Thread.Sleep(1000); // To decrease cpu load.
					}
				}
				catch( ThreadAbortException e )
				{
					foreach (PerformanceCounter net in instances)
					{
						net.Dispose();
					}
					Console.WriteLine("Thread aborted: {0}", e.Message);
				}
			}
		}

		[STAThread]
		static void Main(string[] args)
		{
			// First argument
			if(args.Length > 0)
			{
				if(args[0] == "--help" || args[0] == "-h" || args[0] == "/h" || args[0] == "-?" || args[0] == "-help" || args[0] == "/help")
				{
					Console.WriteLine("Usage: LSM.exe [--help] [--close-task-manager] [--disable-notify] [--toggle-wifi]");
					Console.WriteLine("--help: Show this help message.");
					Console.WriteLine("--close-task-manager: Close task manager when network activity is detected.");
					Console.WriteLine("--disable-notify: Disable notifications.");
					Console.WriteLine("--toggle-wifi: Toggle wifi on/off when network activity is detected.");
					Console.WriteLine("--create-settings: Create a settings file.");
					Console.WriteLine("--BSOD");
					return;
				}
				// Combine all arguments together.
				string arg = "";
				foreach(string a in args)
				{
					arg += a;
				}

				if (arg.Contains("--close-task-manager"))
				{
					Settings.CloseTaskManager = true;
				}
				if (arg.Contains("--disable-notify"))
				{
					Settings.Notify = false;
				}
				if (arg.Contains("--toggle-wifi"))
				{
					Settings.Toggle_WIFI = true;
				}
				if (arg.Contains("--BSOD"))
				{
					BSOD.BSOD_();
					return;
				}
				// Create a 'Settings.xml' file by '--create-settings' argument.
				if (arg.Contains("--create-settings"))
				{
					// If 'Settings.xml' file already exists, tell the user.
					if(File.Exists("Settings.xml"))
					{
						Console.WriteLine("'Settings.xml' already exists.");
						return;
					}
					XmlTextWriter xW = new XmlTextWriter("Settings.xml", Encoding.UTF8);
					xW.Formatting = Formatting.Indented;
					xW.WriteStartElement("Settings");
					xW.WriteStartElement("Toggle_WIFI");
					xW.WriteString("false");
					xW.WriteEndElement();
					xW.WriteStartElement("Notify");
					xW.WriteString("true");
					xW.WriteEndElement();
					xW.WriteStartElement("Hide");
					xW.WriteString("true");
					xW.WriteEndElement();
					xW.WriteStartElement("CloseTaskManager");
					xW.WriteString("false");
					xW.WriteEndElement();
					xW.WriteEndElement();
					xW.Close();
					Console.WriteLine("Settings.xml file created.");
					return;
				}
			}

			if(File.Exists("Settings.xml"))
			{
				try
				{
					XmlDocument xDoc = new XmlDocument();
					xDoc.Load("Settings.xml");
					if (xDoc.SelectSingleNode("Settings/Toggle_WIFI").InnerText.ToLower() == "true")
					{	Settings.Toggle_WIFI = true;}
					if (xDoc.SelectSingleNode("Settings/Notify").InnerText.ToLower() == "false")
					{	Settings.Notify = false;}
					if (xDoc.SelectSingleNode("Settings/Hide").InnerText.ToLower() == "false")
					{	Settings.Hide = false; }
					if (xDoc.SelectSingleNode("Settings/CloseTaskManager").InnerText.ToLower() == "true")
					{	Settings.CloseTaskManager = true; }
				}
				catch(Exception ex)
				{
					Console.WriteLine("There was an error.");
					Console.WriteLine(ex.Message);
				}
			}

			if (Settings.Hide == true)
			{	Console.WriteLine("LSM");	}
			else {	Console.WriteLine("Lan School Monitor");	}


			foreach (Process theprocess in Process.GetProcesses())
			{
				if (//theprocess.ProcessName.Contains("firefox") // <For testing only.
					theprocess.ProcessName.Contains("Lsk") ||
					theprocess.ProcessName.Contains("student") ||
					theprocess.ProcessName.Contains("lskHlpr64") ||
					theprocess.ProcessName.Contains("Isk")
					)
				{
					ProcessList.Add(theprocess);
					Console.WriteLine("Process: \"{0}\" ID: {1}", theprocess.ProcessName, theprocess.Id);
				}
			}

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			//Application.ApplicationExit += new EventHandler(OnApplicationExit);
			Application.Run(new Form1());
		}
	}
}
