using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RADFE
{
    public partial class Form2 : Form
    {

        public String mapname = null;
        public List<String> list = new List<String>();

        public Form2()
        {
            InitializeComponent();
            this.listView1.Columns.Add("Name");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            mapname = listView1.SelectedItems[0].Text;            
        }

        internal void initiallist(List<string> maplist)
        {
            foreach(String temp in maplist){

                this.listView1.Items.Add(temp);

            }
        }
    }
}
