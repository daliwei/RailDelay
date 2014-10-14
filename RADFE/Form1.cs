using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.OleDb;
using QuickGraph;
using QuickGraph.Algorithms.ShortestPath;
using System.IO;
using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;
using System.Collections;
using System.Diagnostics;

namespace RADFE
{    

    public partial class Form1 : Form
    {
        public System.Data.OleDb.OleDbConnection conn;
        public List<String> maplist = new List<String>();
        private string Mapname;

        public RAnetwork RAN = new RAnetwork();

        public DateTime RefTime = new DateTime();
        
        // constraint
        public int ADheadway;
        public int AAheadway;
        public int Dwell;
        public int DDheadway;

        BidirectionalGraph<int, TaggedEdge<int, int>> graph = new BidirectionalGraph<int, TaggedEdge<int, int>>(true);
        BidirectionalGraph<int, TaggedEdge<int, int>> sp_graph = new BidirectionalGraph<int, TaggedEdge<int, int>>(true);
           
 
        public Form1()
        {
            InitializeComponent();

            Form3 f = new Form3();

            if (f.ShowDialog() == DialogResult.OK)
            {
                Intial(f.getradio());
            }
            else
            {
                return;
            }
            
        }

        private void Intial(int check)
        {
            ConnectDB(check);

            RefTime = DateTime.Today;

            InitialContraints(check);

            ReadIntialMap();
            this.Mapname = "Map1";
            LoadMap("Map1");

            
        }

        private void InitialContraints(int check)
        {
            if (check == 1)
            {
                this.AAheadway = 10;
                this.DDheadway = 10;
                this.Dwell = 2;
                this.ADheadway = 10;
            }

            else
            {
                this.AAheadway = 15;
                this.DDheadway = 15;
                this.Dwell = 10;
                this.ADheadway = 15;
            }
        }

        private void ReadIntialMap()
        {
            string sql = "SELECT * FROM TableName";

            OleDbCommand cmd = new OleDbCommand(sql, conn);

            conn.Open();

            OleDbDataReader reader;
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                maplist.Add(reader.GetString(1));
            }

            reader.Close();
        }

        private void ConnectDB(int check)
        {
            conn = new 
                System.Data.OleDb.OleDbConnection();
            // TODO: Modify the connection string and include any
            // additional required properties for your database.
            string location = Application.StartupPath;
            if (check == 1)
            { location = location + "\\RailwayNetwork.mdb"; }
            else
            { location = location + "\\RailwayNetwork-1.mdb"; }

            conn.ConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;" +
                @"Data source=" + location;
            try
            {
                conn.Open();
                // Insert code to process data.
            }
                catch (Exception ex)
            {
                MessageBox.Show("Failed to connect to data source");
            }
            finally
            {
                conn.Close();
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Newnetwork newform = new Newnetwork();
            newform.ShowDialog();
            return;
        }

        private void lOADToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 newform = new Form2();
            newform.initiallist(maplist);            
            if(newform.ShowDialog()==DialogResult.OK)
            {
                this.Mapname = newform.mapname;
                this.LoadMap(Mapname);
            }
            return;
        }

        private void LoadMap(string Mapname)
        {
            String linename = "Line_" + Mapname;
            String Stationname = "Station_" + Mapname;
            String Eventname = "Event_" + Mapname;

            ReadTB(linename, Stationname, Eventname,RAN);

            this.constructsp();

            this.showshortpath();

            this.showevents();
        }

        private void showevents()
        {
            foreach(int i in this.RAN.events.stageno)
            {
                this.listView4.Items.Add("Event Stage " + i);
                this.listView4.Items.Add("      Begin at "+this.RAN.events.intervals[i-1].relativest);
                this.listView4.Items.Add("      End at "+this.RAN.events.intervals[i-1].relativeet);
                String affectsections="      Affected sections are ";
                for (int index = 0; index < RAN.events.sections.Count();index++ )
                {
                    List<Section> j = this.RAN.events.sections[i - 1];
                    foreach (Section k in j)
                    {
                        affectsections = affectsections + " <Id: " + k.sectionid + " and Intensity: " + k.intensity+">;";
                    }
                }
                this.listView4.Items.Add(affectsections);
            }
        }

        private void ReadTB(string linename, string Stationname, string Eventname, RAnetwork RAN)
        {
            OleDbDataReader reader;
            string sql = null;
            OleDbCommand cmd = new OleDbCommand(sql, conn);
            
            //read railway lines
            for (int i = 1; i < 100; i++)
            {
                cmd.CommandText = "SELECT * FROM "+ linename +" where LineNo="+System.Convert.ToString(i)+" order by Sq ASC";
                       
                
                reader = cmd.ExecuteReader();

                List<LineNode> templist = new List<LineNode>();

                int j = 0;
           
                while (reader.Read())
                {
                    

                    LineNode node = new LineNode();
                    node.lineid = i;
                    node.fromno = reader.GetInt32(2);
                    node.tono = reader.GetInt32(3);
                    node.speedlimit = reader.GetInt32(5);
                    node.sectionno = reader.GetInt32(4);
                    node.travetime = reader.GetInt32(8);
                    node.at = reader.GetDateTime(6);
                    node.dt = reader.GetDateTime(7);
                    node.length = reader.GetInt32(10);
                    templist.Add(node);

                    
                    if (RefTime > node.dt)
                    {
                        RefTime = node.dt;
                    }
                    
                    //departure event
                    EventNode enode = new EventNode();
                    enode.departure = true;
                    enode.stationid = node.fromno;
                    enode.sectionid = node.sectionno;
                    enode.squence = j*2 + 1;
                    enode.lineno = node.lineid;
                    enode.st = node.dt;                    
                    this.RAN.eventlines.Add(enode);
                    enode.aevent = this.RAN.eventlines.Count;

                    //arrival event
                    EventNode enodd = new EventNode();
                    enodd.arrival = true;
                    enodd.sectionid = node.sectionno;
                    enodd.squence = (j + 1) * 2;
                    enodd.lineno = node.lineid;
                    enodd.stationid = node.tono;
                    enodd.st = node.at;
                    enodd.speedlimit = node.speedlimit;
                    enodd.length = node.length;
                    enodd.devent = this.RAN.eventlines.Count-1;
                    this.RAN.eventlines.Add(enodd);
                    
                    if (enodd.lineno == 1)
                    {
                        int tony = 0;
                    }
                    
                    j = j + 1;
                }

                reader.Close();

                if (j == 0)
                    break;

                this.RAN.lines.Add(templist);
                
            }

            //read stations
            cmd.CommandText= "SELECT * FROM " + Stationname;
                
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {;
                this.RAN.stations.Add(reader.GetInt32(1));
            }

            reader.Close();

            cmd.CommandText= "SELECT * FROM LineName";
                
            reader = cmd.ExecuteReader();

            this.RAN.linename.Clear();
            while (reader.Read())
            {;
                this.RAN.linename.Add(reader.GetString(1));
            }

            reader.Close();

            //read events
            for (int i = 1; i < 100; i++)
            {
                cmd.CommandText = "SELECT * FROM " + Eventname + " where StageNo=" + System.Convert.ToString(i);


                reader = cmd.ExecuteReader();

                int j = 0;

                while (reader.Read())
                {
                    interval node = new interval();
                    node.last = reader.GetInt32(7);
                    node.st = reader.GetDateTime(5);
                    node.et = reader.GetDateTime(6);

                    TimeSpan tempsp = new TimeSpan();
                    tempsp = node.st - RefTime;
                    node.relativest = (int)tempsp.TotalMinutes;
                    //if (tempsp.TotalMinutes < 0)
                    //{
                    //    if (tempsp.TotalMinutes < -720)
                    //    {
                    //        node.relativest = 1440 + (int)(tempsp.TotalMinutes);
                    //    }
                    //    else
                    //    {
                    //        node.relativest = -(int)(tempsp.TotalMinutes);
                    //    }
                    //}
                    //else
                    //{
                    //    node.relativest =(int) tempsp.TotalMinutes;
                    //}

                    node.relativeet = node.relativest + node.last;
                        

                    this.RAN.events.intervals.Add(node);
                    this.RAN.events.stageno.Add(reader.GetInt32(3));

                    String tempstring;
                    tempstring = reader.GetString(2);
                    String temp2;
                    temp2 = reader.GetString(4);

                    List<Section> single = new List<Section>();

                    while (tempstring.IndexOf(";") >= 0 && temp2.IndexOf(";")>=0)
                    {
                        Section news = new Section();
                        String temp=tempstring.Substring(0, tempstring.IndexOf(";"));
                        String tempi = temp2.Substring(0, temp2.IndexOf(";"));

                        news.sectionid = System.Convert.ToInt32(temp);
                        news.intensity = System.Convert.ToInt32(tempi);

                        tempstring = tempstring.Substring(tempstring.IndexOf(";")+1);
                        temp2 = temp2.Substring(temp2.IndexOf(";") + 1);

                        single.Add(news);
                    }

                    Section newitem = new Section();
                    newitem.sectionid = System.Convert.ToInt32(tempstring);
                    newitem.intensity = System.Convert.ToInt32(temp2);
                    single.Add(newitem);

                    this.RAN.events.sections.Add(single);

                    j = j + 1;
                }

                reader.Close();

                if (j == 0)
                    break;

            }

            this.listView1.Items.Add(Mapname + " Loaded");
            this.listView1.Items.Add("Station # " + this.RAN.stations.Count.ToString());
            this.listView1.Items.Add("Line # " + this.RAN.lines.Count.ToString());
            this.listView1.Items.Add("Event stages # " + this.RAN.events.stageno.Count.ToString());

            buildeventgraph();

            Showdependent();

            ShowGraph();
        }

        /// <summary>
        /// use graphviz to show the graph
        /// </summary>
        private void ShowGraph()
        {
            
        }

        private void Showdependent()
        {
            foreach (EventNode i in this.RAN.eventlines)
            {
                if (i.selfdependencyid.Count == 0 && i.dependencyid.Count==0)
                {
                    if (i.arrival == true)
                    {
                        this.listView1.Items.Add("Arrival Event of line " + i.lineno + " at Station " + i.stationid + " has no Dependency Events"+" Relative time "+ i.relativetime);
                    }
                    else
                    {
                        this.listView1.Items.Add("Departure Event of line " + i.lineno + " at Station " + i.stationid + " has no Dependency Events" + " Relative time " + i.relativetime);
                    }
                    continue;
                }
                
                if (i.arrival == true)
                {
                    this.listView1.Items.Add("Arrival Event of line " + i.lineno + " at Station " + i.stationid + "'s Dependency Events are" + " Relative time " + i.relativetime);
                }
                else
                {
                    this.listView1.Items.Add("Departure Event of line " + i.lineno + " at Station " + i.stationid + "'s Dependency Events are" + " Relative time " + i.relativetime);
                }

                int id = 0;
                for (int j = 0; j < i.dependencyid.Count();j++ )
                {
                    id = i.dependencyid[j];
                    EventNode k = this.RAN.eventlines[id];
                    if (k.arrival == true)
                    {
                        this.listView1.Items.Add("              Arrival Event of line " + k.lineno + " at station " + k.stationid + " Relative time " + k.relativetime + " and Buffer Time " + i.dependentweight[j]);
                    }
                    else
                    {
                        this.listView1.Items.Add("              Departure Event of line " + k.lineno + " at station " + k.stationid + " Relative time " + k.relativetime + " and Buffer Time " + i.dependentweight[j]);
                    }
                }
                for (int j = 0; j < i.selfdependencyid.Count();j++)
                {
                    id = i.selfdependencyid[j];
                    EventNode k2 = this.RAN.eventlines[id];
                    if (k2.arrival == true)
                    {
                        this.listView1.Items.Add("              Arrival Event of line " + k2.lineno + " at station " + k2.stationid + " Relative time " + k2.relativetime + " and Buffer Time " + i.selfweight[j]);
                    }
                    else
                    {

                        this.listView1.Items.Add("              Departure Event of line " + k2.lineno + " at station " + k2.stationid + " Relative time " + k2.relativetime + " and Buffer Time " + i.selfweight[j]);
                    }
                }
            }

            

            listView1.Items[this.listView1.Items.Count - 1].EnsureVisible();
        }

        private void buildeventgraph()
        {
            //DEFINE RELATIVE TIME
            //int templineno = 0;
            //DateTime dt = new DateTime();
            //int temprelative = 0;
            //for (int i = 0; i < this.RAN.eventlines.Count(); i++)
            //{                
            //    if (RAN.eventlines[i].lineno != templineno)
            //    {
            //        templineno = RAN.eventlines[i].lineno;
            //        TimeSpan SP = this.RAN.eventlines[i].st - RefTime;
            //        dt = this.RAN.eventlines[i].st;
            //        if (SP.TotalMinutes < 0)
            //        {
            //            this.RAN.eventlines[i].relativetime = (System.Convert.ToInt32(SP.TotalMinutes) + 60 * 24);
            //            temprelative = this.RAN.eventlines[i].relativetime;
            //        }
            //        else
            //        {
            //            this.RAN.eventlines[i].relativetime = System.Convert.ToInt32(SP.TotalMinutes);
            //            temprelative = this.RAN.eventlines[i].relativetime;
            //        }
            //    }
            //    else
            //    {
            //        if (templineno == 2)
            //        {
            //            int tony = 0;
            //        }
            //        TimeSpan SP1 = this.RAN.eventlines[i].st - dt;
            //        if (SP1.TotalMinutes > 0)
            //        {
            //            this.RAN.eventlines[i].relativetime = System.Convert.ToInt32(SP1.TotalMinutes);
            //            this.RAN.eventlines[i].relativetime = temprelative + this.RAN.eventlines[i].relativetime;
            //        }
            //        else 
            //        {
            //            int timetemp = (24-dt.Hour)*60-dt.Minute;
            //            this.RAN.eventlines[i].relativetime = (this.RAN.eventlines[i].st.Hour * 60 + this.RAN.eventlines[i].st.Minute) + timetemp + temprelative;

            //            //if (SP1.TotalMinutes < -720)
            //            //{
            //            //    this.RAN.eventlines[i].relativetime = 60*24 + System.Convert.ToInt32(SP1.TotalMinutes);
            //            //}
            //            //else
            //            //{
            //            //    this.RAN.eventlines[i].relativetime = 0 - System.Convert.ToInt32(SP1.TotalMinutes);
            //            //}
            //            //this.RAN.eventlines[i].relativetime = temprelative + this.RAN.eventlines[i].relativetime;
            //        }
            //    }                
            //}

            int templineno = 0;
            DateTime dt = new DateTime();
            int temprelative = 0;
            dt=DateTime.Now;
            foreach (EventNode en in this.RAN.eventlines)
            {
                if (en.st < dt)
                {
                    dt = en.st;
                }
            }
            foreach (EventNode en in this.RAN.eventlines)
            {
                TimeSpan ts = en.st - dt;
                en.relativetime = (int)(ts.TotalMinutes);
            }

            //re order the list by the relative time
            for (int i = 0; i < this.RAN.eventlines.Count - 1; i++)
            {
                this.RAN.eventlines[i].realretime = this.RAN.eventlines[i].relativetime + this.RAN.eventlines[i].delay;
                for (int j = i + 1; j < this.RAN.eventlines.Count; j++)
                {
                    if (this.RAN.eventlines[i].relativetime>this.RAN.eventlines[j].relativetime)
                    {
                        EventNode temp = null;
                        temp = this.RAN.eventlines[i];
                        this.RAN.eventlines[i] = this.RAN.eventlines[j];
                        this.RAN.eventlines[j] = temp;
                    }
                }
            }

            //*************************************
            //add periodical events
            //*************************************
            int count = this.RAN.eventlines.Count;
            int additional = 0;
            for (int index = 0; index < additional; index++)
            {
                for (int i = 0; i < count; i++)
                {
                    EventNode en = new EventNode(this.RAN.eventlines[i]);                
                    en.relativetime = this.RAN.eventlines[i].relativetime + 24 *(index+1)* 60;
                    this.RAN.eventlines.Add(en);
                    en.cycleNo = index + 1;
                }
            }

                //*************************************

                //reset the ref arrival dept index
                for (int i = 0; i < this.RAN.eventlines.Count; i++)
                {
                    if (this.RAN.eventlines[i].departure == true)
                    {
                        for (int j = i; j < this.RAN.eventlines.Count; j++)
                        {

                            if (this.RAN.eventlines[i].lineno == this.RAN.eventlines[j].lineno && this.RAN.eventlines[j].sectionid == this.RAN.eventlines[i].sectionid && this.RAN.eventlines[j].arrival == true)
                            {
                                this.RAN.eventlines[i].aevent = j;
                                this.RAN.eventlines[j].devent = i;
                            }
                        }
                    }
                }

            for (int i = 0; i < this.RAN.eventlines.Count - 1; i++)
            {
                this.RAN.eventlines[i].realretime = this.RAN.eventlines[i].relativetime + this.RAN.eventlines[i].delay;
            }

            for (int i = 0; i < this.RAN.eventlines.Count; i++)
            {
                EventNode temp = this.RAN.eventlines[i];
                //search the event in the past
                for (int j = 0; j < i; j++)
                {
                    int st = 0;
                    int buff = 0;
                    EventNode compareitem = this.RAN.eventlines[j];
                    //if it is an arrival event, its dependency may be the departure event, the train at the platform.
                    if (temp.arrival == true)
                    {
                        if (temp.lineno == 3 && temp.stationid == 3)
                        {
                            int tony = 0;
                        }

                        //running time
                        if (compareitem.departure == true && compareitem.lineno == temp.lineno && compareitem.sectionid == temp.sectionid)
                        {
                            temp.selfdependencyid.Add(j);
                            //buffer time (scheduled time-minimu time)
                            st = temp.relativetime - compareitem.relativetime;
                            buff = st-System.Convert.ToInt32((float)temp.length *1000/ ((float)temp.speedlimit/3.6)/60);
                            if (buff < 0)
                            {
                                int tony = 0;
                            }
                            temp.selfweight.Add(buff);
                        }
                        //arrival/departure headway
                        if (compareitem.departure == true && compareitem.stationid == temp.stationid && compareitem.lineno != temp.lineno)
                        {
                            if(temp.dependencyid.Count==0)
                            {
                                temp.dependencyid.Add(j);
                                st = temp.relativetime - compareitem.relativetime;
                                buff = st - ADheadway;
                                if (buff < 0)
                                {
                                    int tony = 0;
                                }
                                temp.dependentweight.Add(buff);
                            }
                            else
                            {
                                for(int k=0; k<temp.dependencyid.Count();k++)
                                {
                                    if (this.RAN.eventlines[temp.dependencyid[k]].departure == true)
                                    {
                                        temp.dependencyid[k]=j;
                                        //buffer time
                                        st = temp.relativetime - compareitem.relativetime;
                                        buff = st - ADheadway;
                                        if (buff < 0)
                                        {
                                            int tony = 0;
                                        }
                                        temp.dependentweight[k]=buff;

                                        break;
                                    }
                                }
                            }
                        }
                        //arrival headway
                         if (compareitem.arrival == true && compareitem.sectionid == temp.sectionid && compareitem.lineno != temp.lineno)
                        {
                            if(temp.dependencyid.Count==0)
                            {
                                temp.dependencyid.Add(j);
                                st = temp.relativetime - compareitem.relativetime;
                                buff = st - AAheadway;
                                if (buff < 0)
                                {
                                    int tony = 0;
                                }
                                temp.dependentweight.Add(buff);
                            }
                            else
                            {
                                for (int k=0; k<temp.dependencyid.Count();k++)
                                {
                                    if (this.RAN.eventlines[temp.dependencyid[k]].arrival == true)
                                    {
                                        temp.dependencyid[k]=j;
                                        st = temp.relativetime - compareitem.relativetime;
                                        buff = st - AAheadway;
                                        if (buff < 0)
                                        {
                                            int tony = 0;
                                        }
                                        temp.dependentweight[k]=buff;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (temp.departure == true)
                    {

                        if (temp.lineno == 3 && temp.stationid==3)
                        {
                            int tony = 0;
                        }

                        //dewell constraint
                        if (compareitem.arrival == true && compareitem.lineno == temp.lineno && compareitem.stationid == temp.stationid)
                        {                            
                            temp.selfdependencyid.Add(j);
                            st = temp.relativetime - compareitem.relativetime;
                            buff = st - Dwell;
                            //let dwell constraint equals to zero
                            //buff = 0;
                            if (buff < 0)
                            {
                                buff = buff;
                            }
                            temp.selfweight.Add(buff);
                        }
                        //departure headway
                        
                        if (compareitem.departure == true && compareitem.sectionid == temp.sectionid && compareitem.lineno != temp.lineno)
                        {
                            if (temp.lineno == 8)
                            {
                                int tony = 0;
                            }

                            if (temp.dependencyid.Count == 0)
                            {
                                temp.dependencyid.Add(j);
                                st = temp.relativetime - compareitem.relativetime;
                                buff = st - DDheadway;
                                if (buff < 0)
                                {
                                    int tony = 0;
                                }
                                temp.dependentweight.Add(buff);
                            }
                            else
                            {
                                for (int k = 0; k < temp.dependencyid.Count(); k++)
                                {
                                    if (this.RAN.eventlines[temp.dependencyid[k]].departure == true)
                                    {
                                        temp.dependencyid[k] = j;
                                        st = temp.relativetime - compareitem.relativetime;
                                        buff = st - DDheadway;
                                        if (buff < 0)
                                        {
                                            int tony = 0;
                                        }
                                        temp.dependentweight[k]=buff;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                for (int k = 0; k < temp.dependencyid.Count; k++)
                {
                    this.RAN.eventlines[temp.dependencyid[k]].updependencyid.Add(i);
                    this.RAN.eventlines[temp.dependencyid[k]].updependentweight.Add(temp.dependentweight[k]);
                }

                for (int k = 0; k < temp.selfdependencyid.Count; k++)
                {
                    this.RAN.eventlines[temp.selfdependencyid[k]].upselfdependencyid.Add(i);
                    this.RAN.eventlines[temp.selfdependencyid[k]].upselfweight.Add(temp.selfweight[k]);
                }
            }  
        }

        private void addLineToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void addStationsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.conn.Close();
        }

        private void calToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearPreviousResults();

            //using the iterative method to calculate the delay

            List<int> H = new List<int>();
            List<int> K = new List<int>();

            List<int> templist = new List<int>();
            List<int> constraint = new List<int>();

            int lastindex=0;
            for (int i = 0; i < this.RAN.events.stageno.Count; i++)
            {
                templist.Clear();
                for (int j = 0; j < this.RAN.eventlines.Count; j++)
                {
                    if (this.RAN.eventlines[j].departure == true                         
                        && this.RAN.eventlines[j].realretime < this.RAN.events.intervals[i].relativeet)
                    {
                        foreach(Section se in this.RAN.events.sections[i])
                        {
                            if (this.RAN.eventlines[j].sectionid == se.sectionid)
                            {
                                if (this.RAN.eventlines[j].lineno == 4 && this.RAN.eventlines[j].sectionid == 74)
                                {
                                    int tony = 0;
                                }

                                H.Add(j);
                                templist.Add(j);
                                //J related arrival constraint has to be changed
                                
                                break;
                            }
                        }
                        //test whether they are in the same section
                        
                    }
                }


                foreach (int j in K)
                {
                    if (H.IndexOf(j) < 0)
                        H.Add(j);
                }

                if (H.Count <= 0)
                {
                    break;
                }

                K.Clear();

                H.Sort();
                
                

                foreach (int j in this.RAN.eventlines[H[0]].updependencyid)
                {
                    if(H.IndexOf(j)<0)
                    H.Add(j);
                }

                foreach (int j in this.RAN.eventlines[H[0]].upselfdependencyid)
                {
                    if (H.IndexOf(j) < 0)
                    H.Add(j);
                }

                H.RemoveAt(0);
                H.Sort();

                

                while (H.Count > 0)
                {
                    EventNode temp = this.RAN.eventlines[H[0]];
                    lastindex=H[0];
                    for(int k=0;k<temp.selfdependencyid.Count;k++)
                    {
                        //calculate its real relative time according to its precedence event
                        //temp.realretime = System.Math.Max(temp.realretime, temp.relativetime+temp.selfweight[k]);
                        if(temp.arrival==true)
                        {
                            int delay=0;
                            //find whether its section has the same section id
                            foreach(Section se in this.RAN.events.sections[i])
                            {
                                if (temp.sectionid == se.sectionid )
                                {
                                    if (temp.lineno == 4 && temp.sectionid == 74)
                                    {
                                        int tony = 0;
                                    }
                                    //bool succ = false;
                                    //foreach (int index in templist)
                                    //{
                                    //    if (this.RAN.eventlines[index].lineno == temp.lineno && temp.sectionid == this.RAN.eventlines[index].sectionid)
                                    //        succ = true;
                                    //}
                                    //if (succ == true)
                                    //{                                    

                                        delay = CalSgDelay(dep(temp), temp, this.RAN.events.intervals[i]); 
                                    //delay=200;                                
                                        break;
                                       //}
                                }
                            }
                            int rela=0;
                            if (delay == 0)
                            {
                                temp.delay = System.Math.Max(temp.delay, this.RAN.eventlines[temp.selfdependencyid[k]].delay - temp.selfweight[k]);
                                temp.realretime = temp.relativetime + temp.delay;
                            }
                            else
                            {
                                temp.delay = System.Math.Max(temp.delay, this.RAN.eventlines[temp.selfdependencyid[k]].delay + delay);
                                temp.realretime = temp.relativetime + temp.delay;

                                if (temp.lineno == 4 && temp.sectionid == 74)
                                {
                                    int tony = 0;
                                }
                            }

                            if (temp.delay < 0)
                            {
                                
                            }
                        }
                        else
                        {                 
                            temp.delay = System.Math.Max(this.RAN.eventlines[temp.selfdependencyid[k]].delay-temp.selfweight[k],0);
                            temp.realretime = temp.relativetime+temp.delay;

                            if (temp.delay < 0)
                            {
                                int tony = 0;
                            }
                        }
                    }
                    for (int k = 0; k < temp.dependencyid.Count; k++)
                    {
                        temp.delay = System.Math.Max(temp.delay, this.RAN.eventlines[temp.dependencyid[k]].delay - temp.dependentweight[k]);
                        temp.realretime = temp.relativetime + temp.delay;
                    }
                    if (temp.arrival==true ||( temp.departure==true && temp.realretime < this.RAN.events.intervals[i].relativeet))
                    {
                        foreach (int k in temp.updependencyid)
                        {
                            if (H.IndexOf(k) < 0)
                            H.Add(k);
                        }
                        foreach (int k in temp.upselfdependencyid)
                        {
                            
                            if (H.IndexOf(k) < 0)
                            H.Add(k);
                        }
                    }
                    else
                    {
                        if (temp.realretime > temp.relativetime)
                        {
                            K.Add(H[0]);
                        }
                    }
                    H.RemoveAt(0);
                    H.Sort();
                }
            }

            if (lastindex < this.RAN.eventlines.Count)
            {

                //method 1
                if (K.Count > 0)
                {
                    if (RAN.eventlines[K[0]].delay > 0)
                    {
                        foreach (int j in this.RAN.eventlines[K[0]].updependencyid)
                        {
                            if (K.IndexOf(j) < 0)
                                K.Add(j);
                        }

                        foreach (int j in this.RAN.eventlines[K[0]].upselfdependencyid)
                        {
                            if (K.IndexOf(j) < 0)
                                K.Add(j);
                        }
                    }
                }
                while (K.Count > 0)
                {                    

                    EventNode temp = this.RAN.eventlines[K[0]];

                    //find its dependency event
                    for (int j = 0; j < temp.selfdependencyid.Count; j++)
                    {
                        temp.delay = System.Math.Max(temp.delay, this.RAN.eventlines[temp.selfdependencyid[j]].delay - temp.selfweight[j]);
                    }
                    for (int j = 0; j < temp.dependencyid.Count; j++)
                    {
                        temp.delay = System.Math.Max(temp.delay, this.RAN.eventlines[temp.dependencyid[j]].delay - temp.dependentweight[j]);
                    }

                    temp.realretime = temp.relativetime + temp.delay;

                    if (RAN.eventlines[K[0]].delay > 0)
                    {
                        foreach (int j in this.RAN.eventlines[K[0]].updependencyid)
                        {
                            if (K.IndexOf(j) < 0)
                                K.Add(j);
                        }

                        foreach (int j in this.RAN.eventlines[K[0]].upselfdependencyid)
                        {
                            if (K.IndexOf(j) < 0)
                                K.Add(j);
                        }
                    }

                    K.RemoveAt(0);
                    K.Sort();

                    if (K.Count <= 0)
                    {
                        break;
                    }

                }
                
                //method 2
                //while (K.Count > 0)
                //{
                //    if (RAN.eventlines[K[0]].delay > 0)
                //    {
                //        foreach (int j in this.RAN.eventlines[K[0]].updependencyid)
                //        {
                //            if (K.IndexOf(j) < 0)
                //                K.Add(j);
                //        }

                //        foreach (int j in this.RAN.eventlines[K[0]].upselfdependencyid)
                //        {
                //            if (K.IndexOf(j) < 0)
                //                K.Add(j);
                //        }
                //    }

                //    K.RemoveAt(0);
                //    K.Sort();

                //    if (K.Count <= 0)
                //    {
                //        break;
                //    }

                //    EventNode temp = this.RAN.eventlines[K[0]];

                //    //find its dependency event
                //    for (int j = 0; j < temp.selfdependencyid.Count; j++)
                //    {
                //        temp.delay = System.Math.Max(temp.delay, this.RAN.eventlines[temp.selfdependencyid[j]].delay - temp.selfweight[j]);
                //    }
                //    for (int j = 0; j < temp.dependencyid.Count; j++)
                //    {
                //        temp.delay = System.Math.Max(temp.delay, this.RAN.eventlines[temp.dependencyid[j]].delay - temp.dependentweight[j]);
                //    }

                //    temp.realretime = temp.relativetime + temp.delay;


                //}
            }
            OutputDelay(2);
        }

        private EventNode dep(EventNode temp)
        {
            return this.RAN.eventlines[temp.devent];
        }

        private EventNode arr(EventNode temp)
        {
            return this.RAN.eventlines[temp.aevent];
        }

        private void ClearPreviousResults()
        {
            foreach (EventNode en in this.RAN.eventlines)
            {
                en.delay = 0;
                en.realretime = en.relativetime;
            }
        }

        private void shortestPathCalculationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearPreviousResults();
            //this algorithm contains two steps:
            //step1 calculate the directly affected events in the network (Arrival events)
            //stemp2 calculate the delay all affected events

            //step 1: calculate the directly affected events

            List<int> initlist = new List<int>();
            List<int> initlistsp = new List<int>();

            //select the event which are arrival event 
            //and its arrival realtive time is within a stage of affected time
            foreach (int i in this.RAN.events.stageno)
            {
                interval it = RAN.events.intervals[i-1];
                List<Section> ls = RAN.events.sections[i-1];

                for (int k = 0; k < this.RAN.eventlines.Count(); k++)
                {
                    EventNode tempevent = this.RAN.eventlines[k];
                    
                    if (tempevent.arrival == true)
                    {
                        continue;
                    }

                    
                    //find this departure event is happened after the starting time of the event 
                    //and it has the same section as the event section
                    int finds = 0;
                    foreach(Section se in ls)
                    {
                        if (se.sectionid == tempevent.sectionid)
                        {
                            finds = se.intensity;
                            break;
                        }
                        else
                            finds = 0;
                    }

                    if (finds == 0)
                    {
                        continue;
                    }

                    //update the departure time
                    if (initlist.Count > 0)
                    {
                        tempevent.realretime = tempevent.realretime + Updatedelay(k, initlist, initlistsp);
                    }

                    if (tempevent.realretime >= it.relativeet)
                    {
                        continue;
                    }

                    if (tempevent.lineno == 1)
                    {
                        int tony = 0;
                    }

                    //EventNode de = null;;
                    ////find its relative departure event
                    //for (int j = k - 1; j >=0; j--)
                    //{
                    //    if (this.RAN.eventlines[j].departure == true && this.RAN.eventlines[j].stationid == tempevent.stationid&&this.RAN.eventlines[j].sectionid == tempevent.sectionid)
                    //    {
                    //        de = this.RAN.eventlines[j];
                    //        break;
                    //    }
                    //}
                    //if (de == null)
                    //{
                    //    continue;
                    //}

                    
                    EventNode ar = null; ;
                    //find its relative arrival event
                    int j = 0;
                    for (j = 0; j < RAN.eventlines.Count(); j++)
                    {
                        if (this.RAN.eventlines[j].arrival == true && this.RAN.eventlines[j].lineno == tempevent.lineno && this.RAN.eventlines[j].sectionid == tempevent.sectionid)
                        {
                            ar = this.RAN.eventlines[j];
                            break;
                        }
                    }
                    if (ar == null)
                    {
                        continue;
                    } 

                    //update the arrival time under normal constraint
                    for (int index = 0; index < initlistsp.Count; index++)
                    {
                        foreach (NEdge ne in this.RAN.spgraph[initlistsp[index]].edges)
                        {
                            if (ne.id == k)
                            {
                                int tempin = this.RAN.eventlines[initlist[index]].delay-ne.weight;
                                //whether judge delayed train will still in this interval
                                //departure事件的delay
                                if (tempin > 0)
                                {
                                    tempevent.delay = System.Math.Max(tempevent.delay, tempin);
                                    tempevent.realretime = tempevent.relativetime + tempevent.delay;
                                }
                                
                                //ar.delay = System.Math.Max(ar.delay,tempin);
                                
                            }
                            if (ne.id == tempevent.aevent)
                            {
                                int tempin = this.RAN.eventlines[initlist[index]].delay - ne.weight;
                                //whether judge delayed train will still in this interval
                                //departure事件的delay
                                if (tempin > 0)
                                {
                                    ar.delay = System.Math.Max(ar.delay, tempin);
                                    ar.realretime = ar.relativetime + ar.delay;
                                }
                            }
                        }
                    }

                    if (ar.realretime <= it.relativest)
                    {
                        continue;
                    }

                    if (ar.lineno == 1 && ar.stationid == 7)
                    {
                        int tony = 0;
                    }
                    int delay = CalSgDelay(tempevent,ar,it);
                    ar.delay = System.Math.Max(ar.delay,delay);                    

                    ar.realretime = ar.relativetime + ar.delay;
                    //eventlist中的j转换到最短路中的节点的序号
                    if (ar.delay <= delay && delay!=0)
                    {
                        initlistsp.Add(tosp(j, initlist));
                        initlist.Add(j);
                    }
                    //the departure event is after the end time of the event
                    //if (ar.relativetime > it.relativeet)
                    //{
                    //    continue;
                    //}
                    //else
                    //{
                    //    //this event will be affected this event
                    //    //cal the delay time of this event
                    //    CalSgDelay(tempevent, finds);

                    //    //if(de.relativetime>it.relativest)
                    //    //{
                    //    //    if (tempevent.relativetime > it.relativeet)
                    //    //    {
                                
                    //    //    }
                    //    //    else
                    //    //    {

                    //    //    }
                    //    //}
                    //    //else
                    //    //{
                    //    //    if (de.relativetime < it.relativest)
                    //    //    {
                    //    //        if (tempevent.relativetime < it.relativeet)
                    //    //        {
                    //    //        }
                    //    //        else
                    //    //        {
                    //    //        }
                    //    //    }
                    //    //}
                    //}

                }
            }


            //after the initial events have been estabilished 
            //we then can calculate the delay for each events
            for (int i = 0; i < this.RAN.eventlines.Count; i++)
            {
                if (initlist.IndexOf(i) >= 0)
                {
                    continue;
                }
                else
                {
                    for(int k=0;k<initlist.Count;k++)
                    {
                        foreach(NEdge ne in this.RAN.spgraph[initlistsp[k]].edges)
                        {
                            if(ne.id==i)
                            {
                                if(this.RAN.eventlines[i].delay<this.RAN.eventlines[initlist[k]].delay-ne.weight)
                                {
                                    this.RAN.eventlines[i].delay = System.Math.Max(this.RAN.eventlines[initlist[k]].delay - ne.weight, 0);
                                    this.RAN.eventlines[i].realretime = this.RAN.eventlines[i].relativetime + this.RAN.eventlines[i].delay;
                                    break;
                                }
                            }

                        }                        
                    }           
                   
                }
            }

            OutputDelay(1, initlist);

            //OutputDelay(1);
            
        }

        private int CalSgDelay(EventNode departure, EventNode arrival, interval it)
        {
            int delay=0;
            departure.length = arrival.length;
            double speedlimit=30f/60f;
            double speedlimith = 100f/60f;
            int temptime = (int)((double)departure.length / speedlimit);
            int arrivaltime = departure.realretime-departure.relativetime+arrival.relativetime;
            //depature的时间在事件开始之后
            if (departure.realretime >= it.relativeet)
            {
                return 0;
            }
            if (arrival.realretime <= it.relativest)
            {
                return 0;
            }

            if (departure.realretime >= it.relativest)
            {
                //全程都收到event的影响
                if (arrivaltime <= it.relativeet)
                {
                    //the entire journey will be affected
                    
                    delay = temptime-arrivaltime + departure.realretime ;
                    if (delay < 0)
                    {
                        //MessageBox.Show("delay line 1240");
                        delay = 0;
                    }
                }
                else
                {
                    
                    //延误后全程收到evnet的影响
                    if (temptime + departure.realretime <= it.relativeet)
                    {
                        delay = temptime - arrivaltime + departure.realretime;
                        if (delay < 0)
                        {
                            //MessageBox.Show("delay line 1240");
                            delay = 0;
                        }
                    }
                    //部分收到Event的影响;后程全速前进
                    else
                    {
                        //影响下的距离
                        int templenght = (int)((double)(it.relativeet - departure.realretime) * speedlimit);
                        //追赶的时间
                        int time2 = (int)((double)(departure.length - templenght) / speedlimith);
                        //总时间
                        int total = time2 + it.relativeet - departure.realretime;
                        //延误
                        delay = total - (arrivaltime - departure.realretime);

                        if (delay < 0)
                        {
                            //MessageBox.Show("delay line 1240");
                            delay = 0;
                        }
                    }
                }
            }
            else
            {
                //事件开始的半段
                double portion = ((double)(it.relativest-departure.realretime))/((double)(arrivaltime - departure.realretime));
                //剩下的距离
                double templength = (double)departure.length * (1-portion);
                //剩下的距离全程受影响
                if (arrivaltime <= it.relativeet)
                {
                    int timeafter = (int)(templength / speedlimit);
                    int total = timeafter+it.relativest-departure.realretime;
                    //延误
                    delay = total - (arrivaltime - departure.realretime);
                    if (delay < 0)
                    {
                        //MessageBox.Show("delay line 1240");
                        delay = 0;
                    }
                }
                else
                {
                    int timeafter =(int)(templength/speedlimit);
                    if (timeafter + it.relativest <= it.relativeet)
                    {
                        //全程受影响
                        int total = timeafter + it.relativest - departure.realretime;
                        //延误
                        delay = total - (arrivaltime - departure.realretime);
                        if (delay < 0)
                        {
                            //MessageBox.Show("delay line 1240");
                            delay = 0;
                        }
                    }
                    //部分收到Event的影响;后程全速前进
                    else
                    {
                        //影响下的距离
                        double templenght2 = ((double)(it.relativeet - it.relativest) )* speedlimit;
                        //追赶的时间
                        int time2 = (int)((templength - templenght2) / speedlimith);
                        //总时间
                        int total = time2 + it.relativeet -it.relativest+(departure.realretime-it.relativest);
                        //延误
                        delay = total - (arrivaltime - departure.realretime);
                        if (delay < 0)
                        {
                            //MessageBox.Show("delay line 1240");
                            delay = 0;
                        }
                    }
                }
            }

            return delay;
        }

        private void OutputDelay(int p, List<int> initlist)
        {
            string loc = Application.StartupPath;
            string location = null;

            { location = loc + "\\ar.txt"; }
            StreamWriter sw = new StreamWriter(location);

            { location = loc + "\\de.txt"; }
            StreamWriter sw1 = new StreamWriter(location);

            { location = loc + "\\linedelay.txt"; }
            StreamWriter swline = new StreamWriter(location);

            { location = loc + "\\Alldelay.txt"; }
            StreamWriter swall = new StreamWriter(location);

            { location = loc + "\\Stations.txt"; }
            StreamWriter swstation = new StreamWriter(location);

            { location = loc + "\\accum.txt"; }
            StreamWriter swsequence = new StreamWriter(location);

            //SHOW RESULT ON INTERFACE
            //*******************************************************
            //********************************************************

            this.listView3.Items.Add("The results by Shortest-path algorithm:");

            for (int i=0; i<this.RAN.eventlines.Count;i++)
            {
                EventNode en = RAN.eventlines[i];
                if (en.delay > 0)
                {
                    if (en.arrival == true)
                    {
                        if(initlist.IndexOf(i)>=0)
                           this.listView3.Items.Add("("+en.cycleNo+")"+"    Primay Delay of Arrival Event of line " + en.lineno + " at station " + en.stationid + " is " + en.delay);
                        else
                            this.listView3.Items.Add("(" + en.cycleNo + ")" + "    Delay of Arrival Event of line " + en.lineno + " at station " + en.stationid + " is " + en.delay);

                        String cs;
                        cs = en.lineno.ToString()+";"+en.stationid.ToString()+";"+en.delay.ToString();
                        sw.WriteLine(cs);
                    }
                    else
                    {
                        this.listView3.Items.Add("(" + en.cycleNo + ")" + "    Delay of Departure Event of line " + en.lineno + " at station " + en.stationid + " is " + en.delay);
                        String cs;
                        cs = en.lineno.ToString() + ";" + en.stationid.ToString() + ";" + en.delay.ToString();
                        sw1.WriteLine(cs);
                    }
                }
            }

            sw.Close();
            sw1.Close();

            //按照线路写延误
            //*******************************************************
            //********************************************************
            int count = this.RAN.lines.Count;
            for (int i = 0; i < count; i++)
            {
                int index=0;
                String temp="";
                String temp2 = "";
                for (int j = 0; j < this.RAN.eventlines.Count; j++)
                {
                    if (this.RAN.eventlines[j].lineno == i + 1)
                    {
                        index = index + 1;
                        temp2 = temp2 + this.RAN.eventlines[j].realretime+";";
                        temp=temp+this.RAN.eventlines[j].delay+";";
                    }
                    //if (index == this.RAN.lines[i].Count*2)
                    //{
                    //    swline.WriteLine((i + 1).ToString() + ";" + this.RAN.eventlines[j].delay);                        
                    //    swall.WriteLine(temp);
                    //    swall.WriteLine(temp2);
                    //    break;
                    //}
                }
                //swline.WriteLine((i + 1).ToString() + ";" + this.RAN.eventlines[j].delay);                        
                swall.WriteLine(temp);
                swall.WriteLine(temp2);
            }
            swline.Close();
            swall.Close();

            //根据station写延误
            //*******************************************************
            //********************************************************
            count = RAN.stations.Count;
            for (int i = 0; i < count; i++)
            {
                String temp = "";
                for (int k = 0; k < RAN.lines.Count; k++)
                {
                     bool  finds=false;
                    for ( int j = 0; j < this.RAN.eventlines.Count; j++)
                    {
                        EventNode en = this.RAN.eventlines[j];
                       
                        if (en.stationid == RAN.stations[i] && en.departure==true && en.lineno==k+1)
                        {                            
                            finds=true;
                            temp = temp + en.delay+ ";" ;
                                break;
                        }                                               
                    }
                    if(finds==false)
                    {
                        temp=temp+"0"+";";
                    } 
                    for ( int j=0; j < this.RAN.eventlines.Count; j++)
                    {                      
                        EventNode en = this.RAN.eventlines[j];
                        finds=false;
                        if (en.stationid == RAN.stations[i] && en.arrival == true && en.lineno == k + 1)
                        {
                            finds = true;
                            temp = temp + en.delay + ";";                
                                break;
                        }
                    }                    
                    if(finds==false)
                    {
                        temp=temp+"0"+";";
                    }                      
                }
                  swstation.WriteLine(temp);
            }
            swstation.Close();

            //按照realtime重新排序，然后再讲延误按出发和到达写入
            //*******************************************************
            //********************************************************
            List<EventNode> templist = new List<EventNode>();
            templist.AddRange(this.RAN.eventlines);

            for (int i = 0; i < templist.Count - 1; i++)
            {

                for (int j = i + 1; j < templist.Count; j++)
                {
                    if (templist[i].realretime > templist[j].realretime)
                    {
                        EventNode temp = null;
                        temp = templist[i];
                        templist[i] = templist[j];
                        templist[j] = temp;

                        if (initlist.IndexOf(i) >= 0 && initlist.IndexOf(j) < 0)
                        {
                            initlist[initlist.IndexOf(i)] = j;
                        }
                        else
                        {
                            if (initlist.IndexOf(j) >= 0)
                            {
                                initlist[initlist.IndexOf(j)] = i;
                            }
                        }
                    }
                }
            }

            List<int> timesar = new List<int>();
            List<int> delayar = new List<int>();
            List<int> delayars= new List<int>();
            List<int> timesde = new List<int>();
            List<int> delayde = new List<int>();
            List<int> delaydes = new List<int>();
            List<int> Primary = new List<int>();
            List<int> Prirela = new List<int>();
            int sum = 0;
            for (int i = 0; i < templist.Count;i++ )
            {
                EventNode en = templist[i];
                if (en.arrival == true)
                {
                    timesar.Add(en.realretime);                    
                    sum = sum + en.delay;
                    delayar.Add(sum);
                    delayars.Add(en.delay);
                }
                else
                {
                    timesde.Add(en.realretime);
                    sum = sum + en.delay;
                    delayde.Add(sum);
                    delaydes.Add(en.delay);
                }

                if (initlist.IndexOf(i) >= 0)
                {
                    Primary.Add(sum);
                    Prirela.Add(en.realretime);
                }
            }

            String tempstr="";
            foreach (int i in timesar)
            {
                tempstr = tempstr + i + ";";
            }
            swsequence.WriteLine(tempstr);

            //累积arrival delay
            tempstr="";
            foreach (int i in delayar)
            {
                tempstr = tempstr+i+";";
            }
            swsequence.WriteLine(tempstr);

            //单个到达延误
            tempstr = "";
            foreach (int i in delayars)
            {
                tempstr = tempstr + i + ";";
            }
            swsequence.WriteLine(tempstr);

            
            tempstr="";
            foreach (int i in timesde)
            {
                tempstr = tempstr+i+";";
            }
            swsequence.WriteLine(tempstr);

            //累积departure延误
            tempstr="";
            foreach (int i in delayde)
            {
                tempstr = tempstr+i+";";
            }
            swsequence.WriteLine(tempstr);

            //单个departure延误
            tempstr = "";
            foreach (int i in delaydes)
            {
                tempstr = tempstr + i + ";";
            }
            swsequence.WriteLine(tempstr);

           tempstr = "";
            foreach (int i in Prirela)
            {
                tempstr = tempstr + i + ";";
            }
            swsequence.WriteLine(tempstr);

            tempstr = "";
            foreach (int i in Primary)
            {
                tempstr = tempstr + i + ";";
            }
            swsequence.WriteLine(tempstr);

            //按line写延误
            tempstr="";
            String tempstrt="";
            List<int> linetemp = new List<int>();
            for (int i = 0; i < this.RAN.lines.Count;i++ )
            {
                linetemp.Add(0);
            }
            foreach (EventNode en in templist)
            {
                for (int i = 0; i < this.RAN.lines.Count;i++ )
                {
                    if (en.lineno == i + 1)
                    {
                        linetemp[i] = en.delay;
                        break;
                    }
                }
                tempstr = tempstr + linetemp.Sum()+";";
                tempstrt = tempstrt + en.realretime + ";";
            }
            swsequence.WriteLine(tempstr);
            swsequence.WriteLine(tempstrt);
            
            swsequence.Close();
        }

        private void OutputDelay(int mode)
        {
            if (mode == 1)
            {
                this.listView3.Items.Add("The results by Shortest-path algorithm:");

                foreach (EventNode en in this.RAN.eventlines)
                {
                    if (en.delay > 0)
                    {
                        if (en.arrival == true)
                        {
                            this.listView3.Items.Add("(" + en.cycleNo + ")" + "    Delay of Arrival Event of line " + en.lineno + " at station " + en.stationid + " is " + en.delay);
                        }
                        else
                        {
                            this.listView3.Items.Add("(" + en.cycleNo + ")" + "    Delay of Departure Event of line " + en.lineno + " at station " + en.stationid + " is " + en.delay);
                        }
                    }
                }
            }
            else
            {
                this.listView3.Items.Add("The results by Iterative algorithm:");

                foreach (EventNode en in this.RAN.eventlines)
                {
                    if (en.delay > 0)
                    {
                        
                        if (en.arrival == true)
                        {
                            this.listView3.Items.Add("(" + en.cycleNo + ")" + "     Delay of Arrival Event of line " + en.lineno + " at station " + en.stationid + " is " + en.delay);
                        }
                        else
                        {
                            this.listView3.Items.Add("(" + en.cycleNo + ")" + "     Delay of Departure Event of line " + en.lineno + " at station " + en.stationid + " is " + en.delay);
                        }
                    }
                }
            }
        }

        private int tosp(int j, List<int> initlist)
        {
            for(int i=0;i<this.RAN.spgraph.Count;i++)
            {
                if(j==this.RAN.spgraph[i].k)
                {
                    return i;   
                }
            }
            return -1;
        }

        private int Updatedelay(int tempevent, List<int> initlist, List<int> initlistsp)
        {
            int pathlength=0;
            int d=0;
            for(int j=0;j<initlist.Count();j++)
            {
                if (initlist[j] > tempevent)
                {
                    continue;
                }
                foreach(NEdge ne in this.RAN.spgraph[initlistsp[j]].edges)
                {
                    if(ne.id==tempevent)
                    {
                        pathlength = ne.weight;
                        if (d < RAN.eventlines[initlist[j]].delay - pathlength)
                        {
                            d = RAN.eventlines[initlist[j]].delay - pathlength;
                            break;
                        }
                        break;
                    }
                }

            }
            return d;
        }

        private int CalSgDelay(EventNode tempevent, int finds)
        {
                //return 200;
            return 60;
        }

        private void showshortpath()
        {                  
            foreach (spnode sp in this.RAN.spgraph)
            {
                EventNode en = RAN.eventlines[sp.k];

                String str;
               
                if(en.arrival == true)
                {
                    if (sp.edges.Count == 0)
                    {
                        str = "Arrival Event of line " + en.lineno + " at station " + en.stationid + " is an terminal event";
                    }   
                    else
                    {
                        str = "Arrival Event of line " + en.lineno + " at station " + en.stationid + "'s shortest path to event:>>>>"+sp.k;
                    }
                }
                else
                {
                    str = "Departure Event of line " + en.lineno + " at station " + en.stationid + "'s shortest path to event:>>>>"+sp.k;
                }
                
                this.listView2.Items.Add(str);

                foreach (NEdge ne in sp.edges)
                {
                    EventNode temp = this.RAN.eventlines[ne.id];
                    if (temp.arrival == true)
                    {
                        str = "         Arrival Event of line " + temp.lineno + " at station " + temp.stationid + " and its weight is " + ne.weight + ">>>"+ne.id;
                    }
                    else
                    {
                        str = "         Departure Event of line " + temp.lineno + " at station " + temp.stationid + " and its weight is " + ne.weight + ">>>" + ne.id;
                    }
                    
                    this.listView2.Items.Add(str);
                }
            }
        }

        public double edgeWeights(TaggedEdge<int, int> edge)
        {
            return edge.Tag;
        }

        /// <summary>
        /// BUILD A SHORTEST PATH GRAPH, THE NODE IS THE EVENT, THE PATH IS 
        /// </summary>
        private void constructsp()
        {
            //build a directional graph based on eventgraph of quickgraph

            Func<TaggedEdge<int, int>, double> EdgeWeights = edgeWeights;


            for (int i = 0; i < this.RAN.eventlines.Count(); i++)
            {
                graph.AddVertex(i);
            }
            this.RAN.hashtable.Clear();
            for (int i = 0; i < this.RAN.eventlines.Count(); i++)
            {
                EventNode temp = this.RAN.eventlines[i];
                for (int k = 0; k < temp.dependencyid.Count(); k++)
                {
                    if (graph.Edges.Contains(new TaggedEdge<int, int>(temp.dependencyid[k], i, temp.dependentweight[k])) == false)
                    {
                        TaggedEdge<int, int> newedge = new TaggedEdge<int, int>(temp.dependencyid[k], i, temp.dependentweight[k]);
                        graph.AddEdge(newedge);
                        RAN.hashtable.Add(newedge.ToString(),newedge.Tag);
                    }
                }
                for (int k = 0; k < temp.selfdependencyid.Count(); k++)
                {
                    TaggedEdge<int, int> newedge = new TaggedEdge<int, int>(temp.selfdependencyid[k], i, temp.selfweight[k]);

                    graph.AddEdge(newedge);
                    RAN.hashtable.Add(newedge.ToString(), newedge.Tag);
                }
            }

            Visualizer.copy_RAN = this.RAN;
            RandColor();
            Visualizer.Visualize(graph, "dependency");
            Process photoViewer = new Process();
            Process.Start(@"c:\\temp\\dependency.jpg");

            FloydWarshallAllShortestPathAlgorithm<int, TaggedEdge<int, int>> alg = new FloydWarshallAllShortestPathAlgorithm<int, TaggedEdge<int, int>>(graph, EdgeWeights);
            alg.Compute();

            //according to alg build a new graph
            RAN.hashtable.Clear();
            foreach (int source in graph.Vertices)
            {
                spnode s = new spnode();
                s.k = source;

                foreach (int target in graph.Vertices)
                {

                    IEnumerable<TaggedEdge<int, int>> path;
                    alg.TryGetPath(source, target, out path);
                    int totalcost = 0;
                    if (path != null)
                    {
                        foreach (TaggedEdge<int, int> e in path)
                        {
                            totalcost = totalcost + (int)(edgeWeights(e));
                        }
                        if (path.Count() > 0)
                        {
                            NEdge ne = new NEdge();
                            ne.id = target;
                            ne.weight = totalcost;
                            s.edges.Add(ne);
                        }
                    }
                }

                this.RAN.spgraph.Add(s);
            }

            foreach(spnode node in RAN.spgraph)
            {
                foreach (NEdge ne in node.edges)
                {
                    if (ne.weight > 0)
                    {
                        TaggedEdge<int, int> newedge = 
                            new TaggedEdge<int, int>(node.k, ne.id, ne.weight);

                        this.sp_graph.AddVerticesAndEdge(newedge);
                        this.RAN.hashtable.Add(newedge.ToString(),ne.weight);
                    }
                }
            }

            Visualizer.Visualize(sp_graph, "global");
           // Process.Start(@"c:\\temp\\global.jpg");
        }

        private void RandColor()
        {
            Visualizer.colors.Clear();
            Random rnd = new Random();
            Byte[] b = new Byte[4];
            foreach(List<LineNode> temp in this.RAN.lines)
            {
                rnd.NextBytes(b);
                GraphvizColor graphcolor = new GraphvizColor();
                Visualizer.colors.Add(new GraphvizColor(b[0], b[1], b[2], b[3]));
            }
        }

        private void testVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string loc = Application.StartupPath;
            String location = "";
            { location = loc + "\\I.txt"; }
            StreamWriter sw = new StreamWriter(location);
            String tempstr1 = "";
            String tempstr2 = "";
            { location = loc + "\\sepI.txt"; }
            StreamWriter sw4 = new StreamWriter(location);
            
            int maxcycle = 1;
            for (int index = 0; index < RAN.eventlines.Count; index++)
            {
                if (this.RAN.eventlines[index].cycleNo == 0)
                {
                    for (int i = 0; i < this.RAN.spgraph.Count; i++)
                    {
                        if (this.RAN.spgraph[i].k == index)
                        {
                            int j = 600;
                            foreach (NEdge ne in this.RAN.spgraph[i].edges)
                            {
                                if (j - ne.weight > 0)
                                {
                                    if (this.RAN.eventlines[ne.id].cycleNo > maxcycle)
                                    {
                                        maxcycle = maxcycle + 1;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            for(int index=0;index<RAN.eventlines.Count;index++)
            {
                if (this.RAN.eventlines[index].cycleNo == 0)
                {
                    for (int i = 0; i < this.RAN.spgraph.Count; i++)
                    {
                        if (this.RAN.spgraph[i].k == index)
                        {
                            int count = 0;
                            int pre = 0;
                            for (int j = 0; j < 600; j = j + 1)
                            {
                                int sum = 0;
                                count = 0;
                                foreach (NEdge ne in this.RAN.spgraph[i].edges)
                                {
                                    sum = sum + System.Math.Max(j - ne.weight, 0);
                                    if (j - ne.weight > 0)
                                    { count = count + 1; }
                                }
                                if (count != pre)
                                {
                                    tempstr1 = tempstr1 + sum + ";";
                                    tempstr2 = tempstr2 + j + ";";
                                    pre = count;
                                }
                                sw.Write(System.Convert.ToString(sum) + ";");
                            }
                        }
                    }
                    sw.WriteLine();
                    sw4.WriteLine(tempstr1);
                    sw4.WriteLine(tempstr2);
                    tempstr1 = "";
                    tempstr2 = "";
                }
            }

            //following not modified according to the new version
            { location = loc + "\\I2.txt"; }
            StreamWriter sw2 = new StreamWriter(location);
            { location = loc + "\\sepI2.txt"; }
            StreamWriter sw3 = new StreamWriter(location);
            //write according to line
             tempstr1 = "";
             tempstr2 = "";
            for (int index = 0; index < RAN.eventlines.Count; index++)
            {
                for (int i = 0; i < this.RAN.spgraph.Count; i++)
                {
                    if (this.RAN.spgraph[i].k == index)
                    { 
                        int count = 0;
                        int pre=0;
                        for (int j = 0; j < 500; j = j + 1)
                        {
                            int sum = 0;     
                            count = 0;                      
                            foreach (NEdge ne in this.RAN.spgraph[i].edges)
                            {
                                if (this.RAN.eventlines[ne.id].squence == this.RAN.lines[this.RAN.eventlines[ne.id].lineno - 1].Count*2)
                                {
                                    sum = sum + System.Math.Max(j - ne.weight, 0);
                                    if (j - ne.weight > 0)
                                    { count = count + 1; }
                                }
                            }
                            if (count != pre)
                            {
                                tempstr1 = tempstr1+sum + ";";
                                tempstr2 = tempstr2 + j + ";";
                                pre = count;
                            }
                            sw2.Write(System.Convert.ToString(sum) + ";");
                        }
                    }
                }
                sw2.WriteLine();                
                sw3.WriteLine(tempstr1);
                sw3.WriteLine(tempstr2);
                tempstr1 = "";
                tempstr2 = "";
            }
            sw.Close();
            sw2.Close();
            sw3.Close();
            sw4.Close();
        }

        private void testVToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //test the number of cycles
            int maxcycle = 1;
            for (int index = 0; index < RAN.eventlines.Count; index++)
            {
                if (this.RAN.eventlines[index].cycleNo == 0)
                {
                    for (int i = 0; i < this.RAN.spgraph.Count; i++)
                    {
                        if (this.RAN.spgraph[i].k == index)
                        {
                            int j = 600;
                            foreach (NEdge ne in this.RAN.spgraph[i].edges)
                            {
                                if (j - ne.weight > 0)
                                {
                                    if (this.RAN.eventlines[ne.id].cycleNo > maxcycle)
                                    {
                                        maxcycle = maxcycle + 1;
                                    }
                                }
                            }
                        }
                    }
                }
            }


            string loc = Application.StartupPath;
            String location = "";
            { location = loc + "\\v.txt"; }
            StreamWriter sw = new StreamWriter(location);
            String tempstr1 = "";
            String tempstr2 = "";
            { location = loc + "\\sepv.txt"; }
            StreamWriter sw4 = new StreamWriter(location);
            
            int pre = 0;
            for (int index = 0; index < RAN.eventlines.Count; index++)
            {
                pre = 0;
                if (RAN.eventlines[index].cycleNo == maxcycle)
                {
                    for (int j = 0; j < 600; j = j + 1)
                    {
                        int sum = 0;
                        int count = 0;
                        for (int i = 0; i < this.RAN.spgraph.Count; i++)
                        {
                            if (this.RAN.spgraph[i].k != index && RAN.eventlines[this.RAN.spgraph[i].k].cycleNo <= maxcycle)
                            {
                                foreach (NEdge ne in this.RAN.spgraph[i].edges)
                                {
                                    if (ne.id == index)
                                    {
                                        sum = sum + System.Math.Max(j - ne.weight, 0);
                                        if (j - ne.weight > 0)
                                        { 
                                            count = count + 1; 
                                        }
                                    }
                                }
                            }
                        }
                        if (count != pre && count > 0)
                        {
                            tempstr1 = tempstr1 + sum + ";";
                            tempstr2 = tempstr2 + j + ";";
                            pre = count;
                        }
                        sw.Write(System.Convert.ToString(sum) + ";");
                    }
                    sw.WriteLine();
                    sw4.WriteLine(tempstr1);
                    sw4.WriteLine(tempstr2);
                    tempstr1 = "";
                    tempstr2 = "";
                }
            }

            { location = loc + "\\v2.txt"; }
            StreamWriter sw2 = new StreamWriter(location);
            { location = loc + "\\sepv2.txt"; }
            StreamWriter sw3 = new StreamWriter(location);
            //write according to line
            tempstr1 = "";
            tempstr2 = "";
            pre = 0;
            for (int index = 0; index < RAN.eventlines.Count; index++)
            {
                pre = 0;
                if (this.RAN.eventlines[index].squence == this.RAN.lines[this.RAN.eventlines[index].lineno - 1].Count*2)
                {
                    for (int j = 0; j < 500; j = j + 1)
                    {
                        int count = 0;
                        int sum = 0;

                        for (int i = 0; i < this.RAN.spgraph.Count; i++)
                        {
                            if (this.RAN.spgraph[i].k != index)
                            {
                                foreach (NEdge ne in this.RAN.spgraph[i].edges)
                                {
                                    if (ne.id == index)
                                    {
                                        sum = sum + System.Math.Max(j - ne.weight, 0);
                                        if (j - ne.weight > 0)
                                        { count = count + 1; }
                                    }
                                }
                            }
                        }
                        sw2.Write(System.Convert.ToString(sum) + ";");
                        if (count != pre)
                        {
                            tempstr1 = tempstr1 + sum + ";";
                            tempstr2 = tempstr2 + j + ";";
                            pre = count;
                        }
                    }
                }
                sw2.WriteLine();
                if (tempstr1 == "")
                {
                    sw3.WriteLine();
                }
                else
                {
                    sw3.WriteLine(tempstr1);
                    sw3.WriteLine(tempstr2);
                }
                tempstr1 = "";
                tempstr2 = "";
            }
            sw.Close();
            sw2.Close();
            sw3.Close();
            sw4.Close();
        }
    }

    public class LineNode
    {
        public int lineid;
        public int fromno;
        public int tono;
        public DateTime dt = new DateTime();
        public DateTime at = new DateTime();
        public int speedlimit;
        public int travetime;
        public int sectionno;
        public int length;
    }

    public class interval
    {
        public DateTime st = new DateTime();
        public DateTime et = new DateTime();
        public int relativest;
        public int relativeet;
        public int last;
    }

    public class Section
    {
        public int sectionid;
        public int intensity;
    }

    public class Event
    {
        public List<List<Section>> sections = new List<List<Section>>();
        public List<interval> intervals = new List<interval>();
        public List<int> stageno = new List<int>();
    }

    public class EventNode
    {
        public bool arrival = false;
        public bool departure = false;
        public int sectionid = 0;
        public int stationid=0;
        public int lineno;
        public int squence;
        public DateTime st = new DateTime();  //schduled time

        //happens before this event
        public List<int> selfdependencyid = new List<int>();
        public List<int> dependencyid = new List<int>();
        public List<int> selfweight = new List<int>();
        public List<int> dependentweight = new List<int>();

        //happens after this event
        public List<int> upselfdependencyid = new List<int>();
        public List<int> updependencyid = new List<int>();
        public List<int> upselfweight = new List<int>();
        public List<int> updependentweight = new List<int>();
        public int relativetime = 0;
        //only arrival event has following two para defined
        public int speedlimit;
        public int length;
        public int delay;
        public int realretime;

        public int cycleNo = 0;
        public int devent;
        public int aevent;
        private EventNode eventNode;

        public EventNode(EventNode eventNode)
        {
            // TODO: Complete member initialization
            arrival = eventNode.arrival;
            departure = eventNode.departure;
            sectionid = eventNode.sectionid;
            stationid=eventNode.stationid;
            lineno=eventNode.lineno;
            squence=eventNode.squence;
            speedlimit=eventNode.speedlimit;
            length=eventNode.length;
            delay=eventNode.delay;
            cycleNo = 0;
        }

        public EventNode()
        {
            // TODO: Complete member initialization
            
        }
    }

    public class RAnetwork
    {
        public List<int> stations = new List<int>();
        public Event events = new Event();
        public List<List<LineNode>> lines = new List<List<LineNode>>();
        public List<string> linename = new List<string>();
        public List<EventNode> eventlines = new List<EventNode>();  
        public List<spnode> spgraph = new List<spnode>();
        //weight of link in the dependence graph
        public List<int> Weights = new List<int>();
        public Hashtable hashtable = new Hashtable();
    }

    public class spnode
    {
        //this k is identification in eventlist
        public int k;
        public List<NEdge> edges = new List<NEdge>();
    }

    public class NEdge
    {
        public int id;
        public int weight;
    }
}
