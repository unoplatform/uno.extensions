using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UnoProjectWizard
{
	public partial class UserInputForm : Form
	{
		private static string customMessage;
		private TextBox textBox1;
		private Button button1;

		public UserInputForm()
		{
			this.Size = new System.Drawing.Size(300, 300);

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
