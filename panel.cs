using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using System.Windows.Forms;

namespace datare
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }
  
        private void button1_Click(object sender, EventArgs e)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            int sliceNum = Convert.ToInt32(File.ReadAllText(path + "\\dimension.txt"));
            DataProcess dp = new DataProcess(path,sliceNum );      
            dp.Slicing();
            MessageBox.Show("分片备份完成");

        }

        private void button2_Click(object sender, EventArgs e)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            int sliceNum = Convert.ToInt32(File.ReadAllText(path + "\\dimension.txt"));
            DataProcess dp = new DataProcess(path,sliceNum);
          string message=  dp.Recovery();
            MessageBox.Show(message );
        }
    }
}
