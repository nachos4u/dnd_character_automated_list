using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DnD_character_list
{
    public partial class Form3 : Form
    {

        public Form3(Form1 form1)
        {
            InitializeComponent();
            this.button1.MouseClick += button1_Click;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Form1 form1 = new Form1();
            form1.Show();
            this.Hide();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(1);
            form4.Show();
            this.Hide();
        }

        private void Form3_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void Form3_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult result = MessageBox.Show(
                    "Вы уверены что хотите закрыть приложение?",
                    "Подтверждение выход",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question);
                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }

        }

        private void Form3_Load(object sender, EventArgs e)
        {
            using (var bd = new DDInformationContext()) {
                var character = bd.Characters.ToList();
                var listlLable = new List<string>();
                for (int i = 1; i < 21; i++)
                {
                    var charka = bd.Characters.Where(c => c.IdCharacter == i).Select(c => new { c.Name }).FirstOrDefault();
                    listlLable.Add(charka.Name);
                }
                label1.Text = listlLable[0];
                label2.Text = listlLable[1];
                label3.Text = listlLable[2];
                label4.Text = listlLable[3];
                label5.Text = listlLable[4];
                label6.Text = listlLable[5];
                label7.Text = listlLable[6];
                label8.Text = listlLable[7];
                label9.Text = listlLable[8];
                label10.Text = listlLable[9];
                label11.Text = listlLable[10];
                label12.Text = listlLable[11];
                label13.Text = listlLable[12];
                label14.Text = listlLable[13];
                label15.Text = listlLable[14];
                label16.Text = listlLable[15];
                label17.Text = listlLable[16];
                label18.Text = listlLable[17];
                label19.Text = listlLable[18];
                label20.Text = listlLable[19];
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(2);
            form4.Show();
            this.Hide();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(3);
            form4.Show();
            this.Hide();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(4);
            form4.Show();
            this.Hide();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(5);
            form4.Show();
            this.Hide();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(6);
            form4.Show();
            this.Hide();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(7);
            form4.Show();
            this.Hide();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(8);
            form4.Show();
            this.Hide();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(9);
            form4.Show();
            this.Hide();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(10);
            form4.Show();
            this.Hide();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(11);
            form4.Show();
            this.Hide();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(12);
            form4.Show();
            this.Hide();
        }

        private void button13_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(13);
            form4.Show();
            this.Hide();
        }

        private void button14_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(14);
            form4.Show();
            this.Hide();
        }

        private void button15_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(15);
            form4.Show();
            this.Hide();
        }

        private void button16_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(16);
            form4.Show();
            this.Hide();
        }

        private void button17_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(17);
            form4.Show();
            this.Hide();
        }

        private void button18_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(18);
            form4.Show();
            this.Hide();
        }

        private void button19_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(19);
            form4.Show();
            this.Hide();
        }

        private void button20_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(20);
            form4.Show();
            this.Hide();
        }
    }
}
