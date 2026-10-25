using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Warn_editor
{
	public partial class Editor : Form
	{
		ToolStrip toolbar;
		PropertyGrid propertyGrid;

		public Editor()
		{
			toolbar = new ToolStrip()
			{
				Dock = DockStyle.Top,
			};

			var LoadButton = new ToolStripDropDownButton("Load");

			var LoadNewest = new ToolStripButton("Newest");
			LoadNewest.Click+= LoadNewestDialog;
			var LoadV0 = new ToolStripButton("Version 0");
			LoadV0.Click+= LoadV0Dialog;

			var SaveButton = new ToolStripDropDownButton("Save");

			var SaveNewest = new ToolStripButton("Newest");
			SaveNewest.Click+= SaveNewestDialog;
			var SaveV0 = new ToolStripButton("Version 0");
			SaveV0.Click+= SaveV0Dialog;

			var UpgradeDropDown = new ToolStripDropDownButton("Upgrade");

			var UpgradeFromV0 = new ToolStripButton("Version 0");
			UpgradeFromV0.Click+= UpgradeFromV0Dialog;

			toolbar.Items.Add(LoadButton);
			toolbar.Items.Add(SaveButton);
			toolbar.Items.Add(UpgradeDropDown);


			LoadButton.DropDown.Items.Add(LoadNewest);
			LoadButton.DropDown.Items.Add(LoadV0);

			SaveButton.DropDown.Items.Add(SaveNewest);
			SaveButton.DropDown.Items.Add(SaveV0);

			UpgradeDropDown.DropDown.Items.Add(UpgradeFromV0);

			propertyGrid = new PropertyGrid()
			{
				Dock = DockStyle.Fill,	
			};

			//Add the controls.
			toolbar.Parent = this;
			propertyGrid.Parent = this;

			Text = "Chairbot warnings editor";
			ShowIcon = false;
		}

		protected SaveFileDialog save = new SaveFileDialog();
		protected OpenFileDialog load = new OpenFileDialog();

		public class Warnings
		{
			public byte PenaltyPoints;

			public struct Warning
			{
				public ulong WarnerID;
				public byte Penalty;
				public string Reason;
				public ulong Timestamp;
				public byte[] EX;


				[DisplayName("Warner User ID")]
				public ulong warnerID
				{
					get => WarnerID;
					set => WarnerID = value;
				}
				[DisplayName("Penalty")]
				public byte penalty
				{
					get => Penalty;
					set => Penalty = value;
				}
				[DisplayName("Warning Reason")]
				public string reason
				{
					get => Reason;
					set => Reason = value;
				}
				[DisplayName("Timestamp")]
				public ulong timestamp
				{
					get => Timestamp;
					set => Timestamp = value;
				}
				[DisplayName("EX Data")]
				public byte[] eX
				{
					get => EX;
					set => EX = value;
				}
			}

			public Warning[] Warns;
			

			[DisplayName("Total Penalty Points")]
			public byte penaltyPoints
			{
				get => PenaltyPoints;
				set => PenaltyPoints = value;
			}
			[DisplayName("Warnings")]
			public Warning[] warns
			{
				get => Warns;
				set => Warns = value;
			}
		}

		public static Warnings LoadNewest(Stream file)
		{
			Warnings warn = new Warnings();
			warn.PenaltyPoints = (byte)file.ReadByte();
			List<Warnings.Warning> warnings = new List<Warnings.Warning>();
			var Warn = new Warnings.Warning();

			byte[] bytes = new byte[sizeof(ulong)];
			byte ExBytes;
			while(file.Position != file.Length)
			{
				file.Read(bytes, 0, sizeof(ulong));
				Warn.WarnerID = BitConverter.ToUInt64(bytes, 0);
				Warn.Penalty = (byte)file.ReadByte();
				file.Read(bytes, 0, sizeof(ushort));

				byte[] Reason = new byte[BitConverter.ToUInt16(bytes, 0)];
				file.Read(Reason, 0, Reason.Length);
				Warn.Reason = Encoding.UTF8.GetString(Reason, 0, Reason.Length);

				file.Read(bytes, 0, sizeof(ulong));
				Warn.Timestamp = BitConverter.ToUInt64(bytes, 0);

				ExBytes = (byte)file.ReadByte();
				Warn.EX = new byte[ExBytes];
				file.Read(Warn.EX, 0, ExBytes);

				warnings.Add(Warn);
			}

			warn.Warns = warnings.ToArray();

			return warn;
		}
		public static void SaveNewest(Stream file, Warnings warn)
		{
			file.WriteByte(warn.PenaltyPoints);

			foreach(var i in warn.Warns)
			{
				file.Write(BitConverter.GetBytes(i.WarnerID), 0, sizeof(ulong));
				file.WriteByte(i.Penalty);
				var Reason = Encoding.UTF8.GetBytes(i.Reason);
				file.Write(BitConverter.GetBytes((ushort)Reason.Length), 0, sizeof(ushort));
				file.Write(Reason, 0, Reason.Length);
				file.Write(BitConverter.GetBytes(i.Timestamp), 0, sizeof(ulong));
				file.WriteByte((byte)i.EX.Length);
				file.Write(i.EX, 0, i.EX.Length);
			}
		}

		public static Warnings LoadV0(Stream file)
		{
			Warnings warn = new Warnings();
			warn.PenaltyPoints = (byte)file.ReadByte();
			List<Warnings.Warning> warnings = new List<Warnings.Warning>();

			byte[] bytes = new byte[sizeof(ulong)];
			var Warn = new Warnings.Warning();

			while(file.Position != file.Length)
			{
				Warn.WarnerID = 0;
				Warn.Penalty = (byte)file.ReadByte();
				file.Read(bytes, 0, sizeof(ushort));

				byte[] Reason = new byte[BitConverter.ToUInt16(bytes, 0)];
				file.Read(Reason, 0, Reason.Length);
				Warn.Reason = Encoding.UTF8.GetString(Reason, 0, Reason.Length);

				file.Read(bytes, 0, sizeof(ulong));
				Warn.Timestamp = BitConverter.ToUInt64(bytes, 0);

				Warn.EX = new byte[0];

				warnings.Add(Warn);
			}

			warn.Warns = warnings.ToArray();

			return warn;
		}
		public static void SaveV0(Stream file, Warnings warn)
		{
			file.WriteByte(warn.PenaltyPoints);

			foreach(var i in warn.Warns)
			{
				file.WriteByte(i.Penalty);
				var Reason = Encoding.UTF8.GetBytes(i.Reason);
				file.Write(BitConverter.GetBytes((ushort)Reason.Length), 0, sizeof(ushort));
				file.Write(Reason, 0, Reason.Length);
				file.Write(BitConverter.GetBytes(i.Timestamp), 0, sizeof(ulong));
			}
		}

		protected void LoadNewestDialog(object sender, EventArgs e)
		{
			if(load.ShowDialog(sender as IWin32Window ?? this) == DialogResult.OK)
			{
				FileStream file = new FileStream(load.FileName, FileMode.Open, FileAccess.Read);
				propertyGrid.SelectedObject = LoadNewest(file);
				file.Dispose();
			}
		}
		protected void LoadV0Dialog(object sender, EventArgs e)
		{
			if(load.ShowDialog(sender as IWin32Window ?? this) == DialogResult.OK)
			{
				FileStream file = new FileStream(load.FileName, FileMode.Open, FileAccess.Read);
				propertyGrid.SelectedObject = LoadV0(file);
				file.Dispose();
			}
		}

		protected void SaveNewestDialog(object sender, EventArgs e)
		{
			if(save.ShowDialog(sender as IWin32Window ?? this) == DialogResult.OK)
			{
				FileStream file = new FileStream(save.FileName, FileMode.Truncate, FileAccess.Write);
				SaveNewest(file, (Warnings)propertyGrid.SelectedObject);
				file.Dispose();
			}
		}
		protected void SaveV0Dialog(object sender, EventArgs e)
		{
			if(save.ShowDialog(sender as IWin32Window ?? this) == DialogResult.OK)
			{
				FileStream file = new FileStream(save.FileName, FileMode.Truncate, FileAccess.Write);
				SaveV0(file, (Warnings)propertyGrid.SelectedObject);
				file.Dispose();
			}
		}

		protected void UpgradeFromV0Dialog(object sender, EventArgs e)
		{
			FolderBrowserDialog FolderBrowser = new FolderBrowserDialog();
			
			if(FolderBrowser.ShowDialog(sender as IWin32Window ?? this) == DialogResult.OK)
			{
				DirectoryInfo Directory = new DirectoryInfo(FolderBrowser.SelectedPath);
				var Files = Directory.GetFiles("Warns", SearchOption.AllDirectories);
				foreach(var i in Files)
				{
					Console.WriteLine("\"{0}\"", i.FullName);
					FileStream file = new FileStream(i.FullName, FileMode.Open, FileAccess.ReadWrite);
					var data = LoadV0(file);
					file.SetLength(0);
					SaveNewest(file, data);

					file.Dispose();
				}
			}

			FolderBrowser.Dispose();
		}
	}
}