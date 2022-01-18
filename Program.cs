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

namespace Lan_School_Monitor
{
	public class Program
	{
		
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
				Application.ApplicationExit += new EventHandler(this.OnApplicationExit);
				// Create an event handler that will be called when the application exits via Ctrl-C or Alt-F4.
				//Application.ApplicationExit += new EventHandler(this.OnApplicationExit);

				notifyicon = new NotifyIcon();
				notifyicon.Icon = SystemIcons.Application;
				notifyicon.Visible = true;

				// Create the context menu items and add them to the notication tray icon.
				//MenuItem programNameMenuItem = new MenuItem("Program Name");
				MenuItem quitMenuItem = new MenuItem("Quit");
				ContextMenu contextMenu = new ContextMenu();
				//contextMenu.MenuItems.Add(programNameMenuItem);
				contextMenu.MenuItems.Add(quitMenuItem);
				notifyicon.ContextMenu = contextMenu;

				quitMenuItem.Click += quitMenuItem_Click;


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

			void Monitor_Thread()
			{
				DateTime last_notified = DateTime.Now;
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
								// Notify if 8 seconds have passed since the last notification.
								if(DateTime.Now.Subtract(last_notified).TotalSeconds > 8)
								{
									last_notified = DateTime.Now;
									notifyicon.ShowBalloonTip(1000, "Network Usage", "Network usage detected on " + net.InstanceName, ToolTipIcon.Info);
									Console.WriteLine("Network usage detected on " + net.InstanceName+" "+DateTime.Now.ToString());
								}
							}
						}
						Thread.Sleep(200); // To decrease cpu load.
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
		static void Main()
		{
			Console.WriteLine("Lan_School_Monitor");

			foreach (Process theprocess in Process.GetProcesses())
			{
				if (//theprocess.ProcessName.Contains("python") //||
					theprocess.ProcessName.Contains("Lsk") ||
					theprocess.ProcessName.Contains("student") ||
					theprocess.ProcessName.Contains("Isk")
					)
				{
					ProcessList.Add(theprocess);
					Console.WriteLine("Process: {0} ID: {1}", theprocess.ProcessName, theprocess.Id);
				}
			}


			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1());
		}
	}
}
