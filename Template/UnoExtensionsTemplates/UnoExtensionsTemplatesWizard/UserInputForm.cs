using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.TemplateWizard;
using EnvDTE;
using System.Drawing;

namespace UnoExtensionsTemplatesWizard
{
	//public partial class UserInputForm : Form
	//{
	//	private static string customMessage;
	//	private TextBox textBox1;
	//	private Button button1;

	//	public UserInputForm()
	//	{

	//		this.Size = new System.Drawing.Size(800, 800);


	//		PanelMenu();
	//		PanelRight();
	//		PanelContent();
	//		PanelTop();

	//	}
	//	public static string CustomMessage
	//	{
	//		get
	//		{
	//			return customMessage;
	//		}
	//		set
	//		{
	//			customMessage = value;
	//		}
	//	}
	//	private void button1_Click(object sender, EventArgs e)
	//	{
	//		customMessage = textBox1.Text;
	//		Close();
	//	}

	//	public void PanelMenu()
	//	{

	//		var PanelMenu = new Panel();
	//		PanelMenu.Location = new System.Drawing.Point(100, 650);
	//		PanelMenu.Size = new System.Drawing.Size(150, 700);
	//		PanelMenu.BackColor = System.Drawing.ColorTranslator.FromHtml("#6f7c86");
	//		PanelMenu.Dock = System.Windows.Forms.DockStyle.Left;
	//		this.Controls.Add(PanelMenu);


	//		var PanelMenuContainner = new Panel();
	//		PanelMenuContainner.Dock = System.Windows.Forms.DockStyle.Fill;
	//		PanelMenuContainner.Padding = new System.Windows.Forms.Padding(20);
	//		PanelMenu.Controls.Add(PanelMenuContainner);

	//		var LabelMenu1 = new Label();
	//		LabelMenu1.Text = "1 - Menu 1";
	//		//LabelMenu1.Dock = System.Windows.Forms.DockStyle.Left;
	//		LabelMenu1.Size = new System.Drawing.Size(100, 20);
	//		LabelMenu1.Location = new System.Drawing.Point(10, 10);
	//		LabelMenu1.Font = new Font(FontFamily.GenericSerif, 12, FontStyle.Bold);
	//		LabelMenu1.ForeColor = System.Drawing.ColorTranslator.FromHtml("#FFFFFF");
	//		PanelMenuContainner.Controls.Add(LabelMenu1);


	//		var LabelMenu2 = new Label();
	//		LabelMenu2.Text = "2 - Menu 2";
	//		//LabelMenu2.Dock = System.Windows.Forms.DockStyle.Left;
	//		LabelMenu2.Location = new System.Drawing.Point(10, 50);
	//		LabelMenu2.Size = new System.Drawing.Size(200, 20);
	//		LabelMenu2.Font = new Font(FontFamily.GenericSerif, 12, FontStyle.Bold);
	//		LabelMenu2.ForeColor = System.Drawing.ColorTranslator.FromHtml("#FFFFFF");
	//		PanelMenuContainner.Controls.Add(LabelMenu2);


	//		PanelMenuContainner.Controls.Add(LabelMenu2);
	//	}
	//	public void PanelRight()
	//	{
	//		var PanelRight = new Panel();
	//		PanelRight.Location = new System.Drawing.Point(0, 800);
	//		PanelRight.Size = new System.Drawing.Size(250, 700);
	//		PanelRight.BackColor = System.Drawing.ColorTranslator.FromHtml("#b7bdc2");
	//		PanelRight.Dock = System.Windows.Forms.DockStyle.Right;
	//		this.Controls.Add(PanelRight);

	//		ListBox listBox1 = new ListBox();
	//		listBox1.Size = new System.Drawing.Size(200, 100);
	//		listBox1.Location = new System.Drawing.Point(10, 10);
	//		PanelRight.Controls.Add(listBox1);
	//		listBox1.SelectionMode = SelectionMode.MultiExtended;

	//		listBox1.BeginUpdate();
	//		for (int x = 1; x <= 50; x++)
	//		{
	//			listBox1.Items.Add("Item " + x.ToString());
	//		}
	//		listBox1.EndUpdate();

	//		listBox1.SetSelected(1, true);
	//		listBox1.SetSelected(3, true);
	//		listBox1.SetSelected(5, true);
	//	}
	//	public void PanelContent()
	//	{

	//		var PanelContent = new Panel();
	//		PanelContent.Location = new System.Drawing.Point(0, 800);
	//		PanelContent.Size = new System.Drawing.Size(500, 150);
	//		PanelContent.BackColor = System.Drawing.ColorTranslator.FromHtml("#939da4");
	//		PanelContent.Dock = System.Windows.Forms.DockStyle.Fill;
	//		this.Controls.Add(PanelContent);

	//		button1 = new Button();
	//		button1.Location = new System.Drawing.Point(90, 25);
	//		button1.Size = new System.Drawing.Size(50, 25);
	//		button1.Location = new System.Drawing.Point(170, 80);
	//		button1.Size = new System.Drawing.Size(200, 20);
	//		button1.Font = new Font(FontFamily.GenericSerif, 12, FontStyle.Bold);
	//		button1.ForeColor = System.Drawing.ColorTranslator.FromHtml("#FFFFFF");

	//		button1.Click += button1_Click;

	//		PanelContent.Controls.Add(button1);

	//		textBox1 = new TextBox();
	//		textBox1.Size = new System.Drawing.Size(100, 20);
	//		textBox1.Location = new System.Drawing.Point(170, 80);
	//		textBox1.Font = new Font(FontFamily.GenericSerif, 12, FontStyle.Bold);
	//		textBox1.ForeColor = System.Drawing.ColorTranslator.FromHtml("#FFFFFF");

	//		PanelContent.Controls.Add(textBox1);

	//	}
	//	public void PanelTop()
	//	{
	//		var PanelTop = new Panel();
	//		PanelTop.Location = new System.Drawing.Point(0, 800);
	//		PanelTop.Size = new System.Drawing.Size(800, 70);
	//		PanelTop.BackColor = System.Drawing.ColorTranslator.FromHtml("#4b5c68");
	//		PanelTop.Dock = System.Windows.Forms.DockStyle.Top;
	//		this.Controls.Add(PanelTop);

	//		var LabelTitle = new Label();
	//		LabelTitle.Text = "New Uno Template Configuration";
	//		LabelTitle.Padding = new System.Windows.Forms.Padding(20, 25, 0, 0);
	//		LabelTitle.Dock = System.Windows.Forms.DockStyle.Fill;
	//		LabelTitle.Font = new Font(FontFamily.GenericSerif, 18, FontStyle.Bold);
	//		LabelTitle.ForeColor = System.Drawing.ColorTranslator.FromHtml("#FFFFFF");
	//		PanelTop.Controls.Add(LabelTitle);


	//	}
	//}

	public partial class UserInputForm : Form
	{
		private static string customMessage;
		private TextBox textBox1;
		private Button button1;

		public UserInputForm()
		{
			this.Size = new System.Drawing.Size(155, 265);

			button1 = new Button();
			button1.Location = new System.Drawing.Point(90, 25);
			button1.Size = new System.Drawing.Size(50, 25);
			button1.Click += button1_Click;
			this.Controls.Add(button1);

			textBox1 = new TextBox();
			textBox1.Location = new System.Drawing.Point(10, 25);
			textBox1.Size = new System.Drawing.Size(70, 20);
			this.Controls.Add(textBox1);
		}
		public static string CustomMessage
		{
			get
			{
				return customMessage;
			}
			set
			{
				customMessage = value;
			}
		}
		private void button1_Click(object sender, EventArgs e)
		{
			customMessage = textBox1.Text;
			this.Close();
		}
	}
}
