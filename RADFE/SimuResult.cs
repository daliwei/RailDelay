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
    public partial class SimuResult : Form
    {
        public SimuResult()
        {
            InitializeComponent();

            this.listView1.Items.Clear();
        }

        public void AddEvent(EventNode ln,EventNode aln, RAnetwork RAN)
        {
            ListViewItem item = new ListViewItem();

            //first column is the route number
            string route = RAN.linename[ln.lineno-1];
            string arrival = ln.arrival==true?"Arrival":"Departure";
            string station = RAN.Stations[ln.stationid - 1].name;
            string scheduletime = ln.st.ToString();
            string actualtime = aln.st.ToString();
            string delay = (aln.relativetime - ln.relativetime).ToString();
            if (ln.relativetime <0)
                return;

            string[] row1 = { arrival, station,scheduletime,actualtime,delay };
            listView1.Items.Add(route).SubItems.AddRange(row1);
        }
    }
}
