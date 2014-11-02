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
using System.Runtime.Serialization.Formatters.Binary;

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

        public int check = 0;
        public bool nocycle = false;

        BidirectionalGraph<int, TaggedEdge<int, int>> graph = new BidirectionalGraph<int, TaggedEdge<int, int>>(true);
        BidirectionalGraph<int, TaggedEdge<int, int>> sp_graph = new BidirectionalGraph<int, TaggedEdge<int, int>>(true);
        private List<Train> Trains = new List<Train>();

        List<List<EventNode>> templist_event = new List<List<EventNode>>();
        public Form1()
        {
            InitializeComponent();

            Form3 f = new Form3();

            if (f.ShowDialog() == DialogResult.OK)
            {
                check = f.getradio();
                nocycle = f.getcheck();
                Intial(check,nocycle);
            }
            else
            {
                return;
            }
        }

        private void Intial(int check, bool nocycle)
        {
            ClearVariables();

            ConnectDB(check,nocycle);
            RefTime = DateTime.Today;
            InitialContraints(check);
            ReadIntialMap();
            this.Mapname = "Map1";
            LoadMap("Map1");            
        }

        /// <summary>
        /// clear all the variables
        /// </summary>
        private void ClearVariables()
        {
            this.RAN = new RAnetwork();
            graph = new BidirectionalGraph<int, TaggedEdge<int, int>>(true);
            sp_graph = new BidirectionalGraph<int, TaggedEdge<int, int>>(true);
            Trains.Clear();

            this.listView1.Items.Clear();
            this.listView2.Items.Clear();
            this.listView3.Items.Clear();
            this.listView4.Items.Clear();
            this.listView5.Items.Clear();
        }

        private void InitialContraints(int check)
        {
            if (check == 1)
            {
                this.AAheadway = 5;
                this.DDheadway = 5;
                this.Dwell = 2;
                this.ADheadway = 2;
            }

            else
            {
                this.AAheadway = 5;
                this.DDheadway = 5;
                this.Dwell = 5;
                this.ADheadway = 5;
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

        private void ConnectDB(int check,bool nocycle)
        {
            conn = new 
                System.Data.OleDb.OleDbConnection();
            // TODO: Modify the connection string and include any
            // additional required properties for your database.
            string location = Application.StartupPath;
            if (check == 1)
            {
                if (nocycle == true)
                {
                    location = location + "\\RailwayNetwork.mdb";
                }
                else
                {
                    location = location + "\\RailwayNetwork_Nocycle.mdb";
                }
            }
            else
            {
                if (nocycle == true)
                {
                    location = location + "\\RailwayNetwork-1.mdb";
                }
                else
                {
                    location = location + "\\RailwayNetwork-1_Nocycle.mdb";
                }
            }

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
            templist_event.Clear();
            for (int i = 1; i < 100; i++)
            {
                //read routes
                cmd.CommandText = "SELECT * FROM "+ linename +" where LineNo="+System.Convert.ToString(i)+" order by Sq ASC";
                reader = cmd.ExecuteReader();
                List<LineNode> templist = new List<LineNode>();
                int j = 0;
                List<EventNode> tempevents = new List<EventNode>();
                while (reader.Read())
                {                    

                    LineNode node = new LineNode();
                    node.lineid = i;
                    node.fromno = reader.GetInt32(2);
                    node.tono = reader.GetInt32(3);
                    node.speedlimit = reader.GetInt32(5);
                    node.sectionno = reader.GetInt32(4);
                    //set node section no
                    node.sectionno = node.fromno * 100 + node.tono;
                    node.travetime = reader.GetInt32(8);
                    node.at = reader.GetDateTime(6);
                    node.dt = reader.GetDateTime(7);
                    TimeSpan ts = new TimeSpan();
                    ts = node.at - node.dt;
                    int travetime = (int)
                        (ts.TotalMinutes < 0 ? ts.TotalMinutes + 1440 : ts.TotalMinutes);
                    if (node.travetime != travetime)
                        node.travetime = travetime;
                    node.length = reader.GetInt32(10);
                    node.period = reader.GetInt32(11);
                    node.repetition = reader.GetInt32(12);                    
                    templist.Add(node);
                    
                    //departure event
                    EventNode enode = new EventNode();
                    enode.departure = true;
                    enode.stationid = node.fromno;
                    enode.sectionid = node.sectionno;
                    enode.squence = j*2 + 1;
                    enode.lineno = node.lineid;
                    enode.st = node.dt;
                    enode.periodicity = node.period;
                    enode.repetition = node.repetition;
                    this.RAN.eventlines.Add(enode);
                    enode.aevent = this.RAN.eventlines.Count;
                    tempevents.Add(enode);

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
                    enodd.periodicity = node.period;
                    enodd.repetition = node.repetition;
                    this.RAN.eventlines.Add(enodd);
                    tempevents.Add(enodd);
                    
                    j = j + 1;
                }

                reader.Close();

                if (j == 0)
                    break;

                this.RAN.lines.Add(templist);
                templist_event.Add(tempevents);
                
            }

            //first method set the reference time as 12:00 am
            RefTime = new DateTime(templist_event[0][0].st.Year,templist_event[0][0].st.Month,templist_event[0][0].st.Day);
            //or set the reference as the earliest departure event of all the routes
            RefTime = DateTime.MaxValue;
            foreach(List<LineNode> templist in RAN.lines)
            {
                if (templist[0].dt < RefTime)
                {
                    RefTime = templist[0].dt;
                }
            }           
            

            //read stations (the station number is from 1)
            cmd.CommandText= "SELECT * FROM " + Stationname;
                
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {;
                this.RAN.stations.Add(reader.GetInt32(1));
                Station s = new Station();
                s.id = reader.GetInt32(1);
                s.name = reader.GetString(2);
                this.RAN.Stations.Add(s);
            }

            //from stations, build links (link id: fromno*10+tonumber)
            int index = 0;
            for(int i=0;i<this.RAN.stations.Count;i++)
            {
                for (int j = i+1; j < this.RAN.stations.Count; j++)
                {
                    Link link = new Link();
                    link.fromstation = i+1;
                    link.tostation = j+1;
                    link.id = (i + 1) * 100 + (j + 1);
                    this.RAN.Links.Add(link);
                    if (this.RAN.hashtable_link.ContainsKey(link.id) == false)
                    {
                        this.RAN.hashtable_link.Add(link.id, index);
                    }
                    for(int k=0;k<RAN.lines.Count;k++)
                    {
                        foreach(LineNode LN in RAN.lines[k])
                        {
                            if (LN.fromno == i+1 && LN.tono == j+1)
                            {
                                LN.linkid = (i+1)*100+(j+1);
                                //add into the hashtable
                                
                            }
                        }
                    }
                    index++;

                    //reverse direction
                    link = new Link();
                    link.fromstation = j + 1;
                    link.tostation = i + 1;
                    link.id = (j + 1) * 100 + (i + 1);
                    this.RAN.Links.Add(link);
                    //add into the hash-table
                    if (this.RAN.hashtable_link.ContainsKey(link.id) == false)
                    {
                        this.RAN.hashtable_link.Add(link.id, index);
                    }
                    for (int k = 0; k < RAN.lines.Count; k++)
                    {
                        foreach (LineNode LN in RAN.lines[k])
                        {
                            if (LN.fromno == j + 1 && LN.tono == i + 1)
                            {
                                LN.linkid = (j + 1) * 100 + (i + 1);
                                
                            }
                        }
                    }
                    index++;
                }
            }

            reader.Close();

            //read route names
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

                        AddHazard2Link(news,node.st,node.et);
                    }

                    Section newitem = new Section();
                    newitem.sectionid = System.Convert.ToInt32(tempstring);
                    newitem.intensity = System.Convert.ToInt32(temp2);
                    single.Add(newitem);
                    AddHazard2Link(newitem, node.st, node.et);

                    //for each event add the affected link for the reverse direction
                    int sectioncout = single.Count;
                    for (int sectionindex = 0; sectionindex < sectioncout; sectionindex++)
                    {
                        Section s = single[sectionindex];
                        Section news = new Section();
                        news.intensity = s.intensity;
                        news.sectionid = (s.sectionid % 100) * 100 + s.sectionid / 100;
                        single.Add(news);
                        AddHazard2Link(news, node.st, node.et);
                    }

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

            //add trains for cycle 0
            int route_index = 1;
            int additional =1;
            foreach (List<LineNode> templist in RAN.lines)
            {
                //simulate or compute 2 days
                if (templist[0].period == 60 * 24)
                {
                    for (int i = 0; i <= additional; i++)
                    {
                        //input: i is the cycle
                        AddTrain(templist, i, route_index, templist_event[route_index - 1]);
                    }
                }
                //else do the repetition for less than an cycle 
                else
                {
                    for (int i = 0; i <= templist[0].repetition; i++)
                    {
                        //input: i is the cycle
                        AddTrain(templist, i, route_index, templist_event[route_index - 1]);
                    }
                    //for each of those repetition add the same route for the second day
                    for (int i = 0; i <= templist[0].repetition; i++)
                    {
                        copytrain42ndday(this.Trains[this.Trains.Count - 1 - i * 2], templist[0].repetition+i+1);
                    }
                }
                route_index++;
            }

            //according to the train add event
            this.RAN.eventlines.Clear();
            foreach(Train train in this.Trains)
            {
                this.RAN.eventlines.AddRange(train.schedule);
            }

            //Write the routes
            writeroutes();

            writehazards();

            buildeventgraph();

            Showdependent();

            //ShowGraph();
        }

        //copy the train route and schedule plus adding one day for each event
        private void copytrain42ndday(Train copy_train, int cycle)
        {
            // add a full train
            Train train = new Train();
            train.cycle = copy_train.cycle;
            train.route = this.DeepCopy(copy_train.route);
            //default initialization
            train.destination = copy_train.route[copy_train.route.Count - 1].tono;
            train.fromstation = copy_train.route[0].fromno;
            train.location = 0;
            train.onstation = copy_train.route[0].fromno;
            train.speed = (double)copy_train.route[0].length / (double)((double)copy_train.route[0].travetime / 60);
            train.tostation = copy_train.route[0].tono;
            train.routeid = copy_train.routeid;
            train.schedule = this.DeepCopy(copy_train.schedule);
            train.act_times = this.DeepCopy(copy_train.schedule);
            train.period = copy_train.route[0].period;

            //change the train to the second-day 
            for (int i = 0; i < train.route.Count; i++)
            {
                train.route[i].at = train.route[i].at.AddHours(24);
                train.route[i].dt = train.route[i].dt.AddHours(24);
            }
            //add cycles for schedules
            for (int i = 0; i < train.schedule.Count; i++)
            {
                train.schedule[i].st = train.schedule[i].st.AddHours(24);
                train.schedule[i].cycleNo = cycle;
            }
            train.cycle = cycle;
            this.Trains.Add(train);
        }

        /// <summary>
        /// write hazardous events
        /// </summary>
        private void writehazards()
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\Dali\Desktop\Hazards.txt");
            for(int i=0;i<RAN.events.sections.Count;i++)
            { 
                List<Section> sections = RAN.events.sections[i];
                interval inter = RAN.events.intervals[i];
                string line = "Event "+i.ToString()+" from " + inter.st.ToString() + " to " + inter.et.ToString();
                file.WriteLine(line);
                for(int j=0;j<sections.Count;j++)
                {
                    Section s = sections[j];
                    int lid = (int)this.RAN.hashtable_link[s.sectionid];
                    string from = this.RAN.Stations[this.RAN.Links[lid].fromstation - 1].name;
                    string to = this.RAN.Stations[this.RAN.Links[lid].tostation - 1].name;
                    line = "       Link: " + s.sectionid.ToString() + " from " + from + " to " + to 
                        + "and Intensity: "
                        + s.intensity.ToString() + " Reduced speed " + this.ReduceSpeed(s.intensity).ToString();
                    file.WriteLine(line);
                }
            }
            file.Close();
        }

        /// <summary>
        /// write routes of trains
        /// </summary>
        private void writeroutes()
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\Dali\Desktop\Routes.txt");
            foreach(Train train in this.Trains)
            {
                 string line  = "Route "+train.routeid.ToString()+"_Cycle_"+train.cycle.ToString();
                 file.WriteLine(line);
                foreach(EventNode en in train.schedule)
                {
                    if (en.arrival == true)
                    {
                        line = "      Arrival at "+this.RAN.Stations[en.stationid-1].name+" at time "+en.st.ToString();
                    }
                    else
                    {
                        line = "      Departure at " + this.RAN.Stations[en.stationid - 1].name + " at time " + en.st.ToString();
                    }
                    file.WriteLine(line);
                }
            }
            file.Close();
        }

        //add hazards to link
        private void AddHazard2Link(Section news,DateTime st, DateTime et)
        {
            foreach(Link lk in this.RAN.Links)
            {
                if (lk.id == news.sectionid)
                {
                    Hazard hazard = new Hazard();
                    hazard.startime = st;
                    hazard.endtime = et;
                    hazard.intensity = news.intensity;
                    lk.Link_hazards.Add(hazard);
                    
                    //update list view
                    TimeSpan sp = new TimeSpan();
                    sp = st-this.RefTime;
                    int refstart = (int) sp.TotalMinutes;

                    sp = et - this.RefTime;
                    int refend = (int) sp.TotalMinutes;

                    string item = "Link_" + lk.id.ToString() + "_intensity_" + hazard.intensity.ToString() +
                        "_from_" + refstart.ToString() + "_end_" + refend.ToString();
                    this.listView5.Items.Add(item);

                    break;
                }
            }
        }

        private void AddTrain(List<LineNode> route,int cycle,int routeid,List<EventNode> schedule)
        {
            //when add a train, there may be a partial train and full train
            
            // add a full train
            Train train = new Train();
            train.cycle = cycle;
            train.route = this.DeepCopy(route);
            //default initialization
            train.destination = route[route.Count - 1].tono;
            train.fromstation = route[0].fromno;
            train.location = 0;
            train.onstation = route[0].fromno;
            train.speed = (double)route[0].length / (double)((double)route[0].travetime / 60);
            train.tostation = route[0].tono;
            train.routeid = routeid;
            train.schedule = this.DeepCopy(schedule);
            train.act_times = this.DeepCopy(schedule);
            train.period = route[0].period;

            //change the arrival and departure events that are in the second day
            for (int i = 1; i <train.route.Count; i++)
            {
                if ((train.route[i].dt - train.route[0].dt).TotalMinutes <= 0)
                {
                    train.route[i].dt = train.route[i].dt.AddDays(1);
                }
                if ((train.route[i].at - train.route[0].dt).TotalMinutes <= 0)
                {
                    train.route[i].at = train.route[i].at.AddDays(1);
                }
            }
            //now for schedules
            for (int i = 1; i <train.schedule.Count; i++)
            {
                if ((train.schedule[i].st - train.schedule[0].st).TotalMinutes <= 0)
                {
                    train.schedule[i].st = train.schedule[i].st.AddDays(1);
                }
            }
            //add cycles for route
            for (int i = 0; i < train.route.Count; i++)
            {
                train.route[i].at = train.route[i].at.AddMinutes(cycle*train.period);
                train.route[i].dt = train.route[i].dt.AddMinutes(cycle * train.period);
            }
            //add cycles for schedules
            for (int i = 0; i < train.schedule.Count; i++)
            {
                train.schedule[i].st = train.schedule[i].st.AddMinutes(cycle * train.period);
               train.schedule[i].cycleNo = cycle;
            }
            this.Trains.Add(train);
            if (cycle > 0)
                return;
            //now add the partial train if necessary
            int index=0;
            int min_interval=100000000;
            //find the event that are closest to the ref time
            bool arrival = false;
            for (int i = 0; i < route.Count;i++ )
            {
                if ((route[i].dt - this.RefTime).TotalMinutes < min_interval && (route[i].dt - this.RefTime).TotalMinutes>=0)
                {
                    min_interval = (int)(route[i].dt - this.RefTime).TotalMinutes;
                    index = i;
                    arrival = false;
                }
                if ((route[i].at - this.RefTime).TotalMinutes < min_interval && (route[i].at - this.RefTime).TotalMinutes > 0)
                {
                    min_interval = (int)(route[i].at - this.RefTime).TotalMinutes;
                    index = i;
                    arrival = true;
                }
            }
            if (index == 0 && arrival == false)
                //no partial route
                return;
            else
            {
                //partial route is only for those train that is not traveling in periodic mood
                //specifically, the periodic mode only works for those routes that depart and arrive in the same day
                Train train1 = new Train();
                train1.cycle = -1; //partial train's cycle index is -1
                train1.route = this.DeepCopy(route);
                //default initialization
                train1.destination = route[route.Count - 1].tono;
                train1.fromstation = route[0].fromno;
                train1.onstation = route[0].fromno;
                train1.tostation = route[0].tono;
                train1.routeid = routeid;
                train1.schedule = this.DeepCopy(schedule);

                //add a train that is already traveling on the link or wait in the station
                if (arrival == true)
                {
                    //the train in currently in travel
                    train1.onlink = route[index].sectionno;
                    train1.onstation = -1;
                    train1.cycle = -1;
                    train1.tostation = route[index].tono;
                    train1.fromstation = route[index].fromno;
                    train1.speed = (double)route[index].length / (double)((double)route[index].travetime / 60);
                    train1.station_passed = index ;
                    train1.location = train1.speed 
                        * (double)(route[index].travetime - min_interval) / 60;
                    
                    //set the arrival or departure before index minus one day
                    for (int i = 0; i <= index; i++)
                    {
                        if (i>0&&(train1.route[i - 1].at.AddDays(1) - train1.route[i].dt).TotalMinutes > 0)
                        {
                            break;
                        }
                        if ((train1.route[i].at - train1.route[i].dt).TotalMinutes > 0)
                        {                            
                            train1.route[i].at = train1.route[i].at.AddDays(-1);
                            train1.schedule[i*2+1].st = train1.schedule[i*2+1].st.AddDays(-1);
                            train1.route[i].dt = train1.route[i].dt.AddDays(-1);                            
                            train1.schedule[i*2].st = train1.schedule[i*2].st.AddDays(-1);
                        }
                        else
                        {
                            train1.route[i].dt = train1.route[i].dt.AddDays(-1);
                            train1.schedule[i * 2].st = train1.schedule[i * 2].st.AddDays(-1);
                            break;
                        }
                    }
                }
                else
                {
                    //the train is currently wait in the station
                    train1.speed = 0;
                    train1.onstation = route[index].fromno;
                    train1.onlink = -1;
                    train1.station_passed = index;
                    train1.cycle = -1;

                    //set the arrival or departure before index minus one day
                    for (int i = 0; i <= index; i++)
                    {
                        if (i > 0 && (train1.route[i - 1].at.AddDays(1) - train1.route[i].dt).TotalMinutes > 0)
                        {
                            break;
                        }
                        if ((train1.route[i].at - train1.route[i].dt).TotalMinutes > 0)
                        {
                            train1.route[i].at = train1.route[i].at.AddDays(-1);
                            train1.schedule[i * 2 + 1].st = train1.schedule[i * 2 + 1].st.AddDays(-1);
                            train1.route[i].dt = train1.route[i].dt.AddDays(-1);
                            train1.schedule[i * 2].st = train1.schedule[i * 2].st.AddDays(-1);
                        }
                        else
                        {
                            train1.route[i].dt = train1.route[i].dt.AddDays(-1);
                            train1.schedule[i * 2].st = train1.schedule[i * 2].st.AddDays(-1);
                            break;
                        }
                    }
                }

                foreach (EventNode en in train1.schedule)
                {
                    en.cycleNo = -1;
                }
                train1.act_times = this.DeepCopy(train1.schedule);

                this.Trains.Add(train1);    
            }
               
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
            DateTime dt = this.RefTime;
            //in the event-line, we ignore the event that are in cycle -1 and earlier than the ref time
            int count = this.RAN.eventlines.Count;
            List<int> remove_index = new List<int>();
            for (int i = 0; i < count; i++)
            {
                EventNode en = RAN.eventlines[i];
                TimeSpan ts = en.st - dt;
                en.relativetime = (int)(ts.TotalMinutes);
                if (en.relativetime < 0)
                {
                    remove_index.Add(i);
                }
            }

            //remove those events that happen earlier than the reference time 
            for (int i = 0; i < remove_index.Count; i++)
            {
                RAN.eventlines.RemoveAt(remove_index[i] - i);
            }
            
            //re order the list by the relative time
            for (int i = 0; i < this.RAN.eventlines.Count - 1; i++)
            {
                this.RAN.eventlines[i].realretime = this.RAN.eventlines[i].relativetime + this.RAN.eventlines[i].delay;
                for (int j = i + 1; j < this.RAN.eventlines.Count; j++)
                {
                    if (this.RAN.eventlines[i].relativetime > this.RAN.eventlines[j].relativetime)
                    {
                        EventNode temp = null;
                        temp = this.RAN.eventlines[i];
                        this.RAN.eventlines[i] = this.RAN.eventlines[j];
                        this.RAN.eventlines[j] = temp;
                    }
                }
            }

            writeevents();
            
            //set the schedule 进站顺序 和link的到达顺序for simulation
            //for each station, set the list of arrival route (the sequence)
            foreach(Station s  in this.RAN.Stations)
            {
                s.routes = new List<int>();
                s.cycles = new List<int>();
                foreach(EventNode en in this.RAN.eventlines)
                {
                    if (en.arrival == true && en.stationid == s.id)
                    {
                        s.routes.Add(en.lineno);
                        s.cycles.Add(en.cycleNo);
                        s.nextroute = 0;
                        s.cycle = 0;
                    }
                }
            }
            //write
            writestationsequence();

            //for line add departure sequence
            int temp_indexer = -1;
            foreach(Link k in this.RAN.Links)
            {
                temp_indexer++;
                k.routes = new List<int>();
                k.cycles = new List<int>();
                foreach (EventNode en in this.RAN.eventlines)
                {
                    if (en.arrival == false && k.id == en.sectionid)
                    {
                        k.routes.Add(en.lineno);
                        k.cycles.Add(en.cycleNo);
                        k.nextroute = 0;
                        k.cycle = 0;
                    }
                }
            }
            writelinksequence();

            //*************************************
            //reset the ref arrival dept index
            for (int i = 0; i < this.RAN.eventlines.Count; i++)
            {
                if (this.RAN.eventlines[i].departure == true)
                {
                    for (int j = i; j < this.RAN.eventlines.Count; j++)
                    {
                        if (this.RAN.eventlines[i].lineno == this.RAN.eventlines[j].lineno 
                            && this.RAN.eventlines[j].sectionid == this.RAN.eventlines[i].sectionid 
                            && this.RAN.eventlines[j].arrival == true
                            && this.RAN.eventlines[i].cycleNo == this.RAN.eventlines[j].cycleNo)
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
                        if (temp.lineno == 2 && temp.stationid == 2)
                            temp = temp;
                        //running time
                        if (compareitem.departure == true 
                            && compareitem.lineno == temp.lineno 
                            && compareitem.sectionid == temp.sectionid
                            && compareitem.cycleNo == temp.cycleNo)
                        {
                            temp.selfdependencyid.Add(j);
                            //buffer time (scheduled time-minimum time)
                            st = temp.relativetime - compareitem.relativetime;
                            buff = st-System.Convert.ToInt32((float)temp.length *1000/ ((float)temp.speedlimit/3.6)/60);
                            temp.selfweight.Add(buff);
                        }
                        //arrival/departure headway
                        //different line or same line but different cycle
                        if (compareitem.departure == true && compareitem.stationid == temp.stationid 
                            && (compareitem.lineno != temp.lineno||(compareitem.lineno==temp.lineno && compareitem.cycleNo!=temp.cycleNo)))
                        {
                            if(temp.dependencyid.Count==0)
                            {
                                temp.dependencyid.Add(j);
                                st = temp.relativetime - compareitem.relativetime;
                                buff = st - ADheadway;
                                temp.dependentweight.Add(buff);
                            }
                            else
                            {
                                bool found_dept = false;
                                for(int k=0; k<temp.dependencyid.Count();k++)
                                {
                                    if (this.RAN.eventlines[temp.dependencyid[k]].departure == true)
                                    {
                                        temp.dependencyid[k]=j;
                                        //buffer time
                                        st = temp.relativetime - compareitem.relativetime;
                                        buff = st - ADheadway;
                                        temp.dependentweight[k]=buff;
                                        found_dept = true;
                                        break;
                                    }
                                }
                                if (found_dept == false)
                                {
                                    temp.dependencyid.Add(j);
                                    st = temp.relativetime - compareitem.relativetime;
                                    buff = st - ADheadway;
                                    temp.dependentweight.Add(buff);
                                }                                    
                            }
                        }
                        //arrival headway
                        //or different cycle
                         if (compareitem.arrival == true && compareitem.sectionid == temp.sectionid
                             && (compareitem.lineno != temp.lineno || (compareitem.lineno == temp.lineno && compareitem.cycleNo != temp.cycleNo)))
                        {
                            if(temp.dependencyid.Count==0)
                            {
                                temp.dependencyid.Add(j);
                                st = temp.relativetime - compareitem.relativetime;
                                buff = st - AAheadway;
                                temp.dependentweight.Add(buff);
                            }
                            else
                            {
                                bool found_arrival = false;
                                for (int k=0; k<temp.dependencyid.Count();k++)
                                {
                                    if (this.RAN.eventlines[temp.dependencyid[k]].arrival == true)
                                    {
                                        temp.dependencyid[k]=j;
                                        st = temp.relativetime - compareitem.relativetime;
                                        buff = st - AAheadway;                                        
                                        temp.dependentweight[k]=buff;
                                        found_arrival = true;
                                        break;
                                    }
                                }
                                if (found_arrival == false)
                                {
                                    temp.dependencyid.Add(j);
                                    st = temp.relativetime - compareitem.relativetime;
                                    buff = st - AAheadway;
                                    temp.dependentweight.Add(buff);
                                }
                            }
                        }
                    }
                    //if this is a departure event
                    if (temp.departure == true)
                    {
                        //dwell constraint
                        if (compareitem.arrival == true 
                            && compareitem.lineno == temp.lineno
                            && compareitem.stationid == temp.stationid
                            && compareitem.cycleNo == temp.cycleNo)
                        {                            
                            temp.selfdependencyid.Add(j);
                            st = temp.relativetime - compareitem.relativetime;
                            buff = st - Dwell;
                            temp.selfweight.Add(buff);
                        }
                        //departure headway                        
                        if (compareitem.departure == true && compareitem.sectionid == temp.sectionid &&
                             (compareitem.lineno != temp.lineno || (compareitem.lineno == temp.lineno && compareitem.cycleNo != temp.cycleNo)))
                        {
                            if (temp.dependencyid.Count == 0)
                            {
                                temp.dependencyid.Add(j);
                                st = temp.relativetime - compareitem.relativetime;
                                buff = st - DDheadway;
                                temp.dependentweight.Add(buff);
                            }
                            else
                            {
                                bool found_dept2 = false;
                                for (int k = 0; k < temp.dependencyid.Count(); k++)
                                {
                                    if (this.RAN.eventlines[temp.dependencyid[k]].departure == true)
                                    {
                                        temp.dependencyid[k] = j;
                                        st = temp.relativetime - compareitem.relativetime;
                                        buff = st - DDheadway;
                                        temp.dependentweight[k]=buff;
                                        found_dept2 = true;
                                        break;
                                    }
                                }
                                if (found_dept2 == false)
                                {
                                    temp.dependencyid.Add(j);
                                    st = temp.relativetime - compareitem.relativetime;
                                    buff = st - DDheadway;
                                    temp.dependentweight.Add(buff);
                                }
                            }
                        }
                    }
                    if (buff < 0)
                        buff = buff;
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

        private void writelinksequence()
        { 
            System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\Dali\Desktop\LinkService.txt");
            foreach(Link k in this.RAN.Links)
            {
                string line;
                line = "Link "+k.id.ToString()+"_incoming trains";
                file.WriteLine(line);
                int index = 0;
                foreach(int routeid in k.routes)
                {
                    line = "        Service " + routeid.ToString()+ "_Cycle_"+k.cycles[index];
                    file.WriteLine(line);
                    index++;
                }
            }
            file.Close();
        }

        private void writestationsequence()
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\Dali\Desktop\StationService.txt");
            foreach (Station s in this.RAN.Stations)
            {
                string line;
                line = "Station " + s.name + "_incoming trains";
                file.WriteLine(line);
                int index = 0;
                foreach (int routeid in s.routes)
                {
                    line = "        Service " + this.RAN.linename[routeid-1] + "_Cycle_" + s.cycles[index];
                    file.WriteLine(line);
                    index++;
                }
            }
            file.Close();
        }

        private void writeevents()
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\Dali\Desktop\Events.txt");
            foreach(EventNode en in this.RAN.eventlines)
            {
                string line;
                    if (en.arrival == true)
                    {
                        line ="Route_"+en.lineno+ "_cycle_"+en.cycleNo.ToString()+" Arrival at "+this.RAN.Stations[en.stationid-1].name+" at time "+en.st.ToString();
                    }
                    else
                    {
                        line = "Route_" + en.lineno + "_cycle_" + en.cycleNo.ToString() + " Departure at " + this.RAN.Stations[en.stationid - 1].name + " at time " + en.st.ToString();
                    }
                    file.WriteLine(line);
            }
            file.Close();
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

        /// <summary>
        /// interactive algorithm
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void calToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Interative();            
        }

        private void Interative()
        {
            ClearPreviousResults();

            //using the iterative method to calculate the delay

            List<int> H = new List<int>();
            List<int> K = new List<int>();

            List<int> templist = new List<int>();
            List<int> constraint = new List<int>();

            int lastindex = 0;
            for (int i = 0; i < this.RAN.events.stageno.Count; i++)
            {
                templist.Clear();
                for (int j = 0; j < this.RAN.eventlines.Count; j++)
                {
                    //if the event happens before the stage i begins
                    if (this.RAN.eventlines[j].departure == true
                        && this.RAN.eventlines[j].realretime < this.RAN.events.intervals[i].relativeet)
                    {
                        //if this event is within the range of the hazard, put it into H and temp list
                        foreach (Section se in this.RAN.events.sections[i])
                        {
                            if (this.RAN.eventlines[j].sectionid == se.sectionid)
                            {
                                H.Add(j);
                                templist.Add(j);

                                break;
                            }
                        }
                    }
                }

                //combine K and H
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

                //find the events that H relies on
                foreach (int j in this.RAN.eventlines[H[0]].updependencyid)
                {
                    if (H.IndexOf(j) < 0)
                        H.Add(j);
                }
                //find the events that H relies on
                foreach (int j in this.RAN.eventlines[H[0]].upselfdependencyid)
                {
                    if (H.IndexOf(j) < 0)
                        H.Add(j);
                }

                H.RemoveAt(0); // ?
                H.Sort();

                while (H.Count > 0)
                {
                    EventNode temp = this.RAN.eventlines[H[0]];

                    if (temp.lineno == 2 && temp.stationid == 2 && temp.cycleNo == 0)
                        temp.lineno = 2;
                    lastindex = H[0];
                    for (int k = 0; k < temp.selfdependencyid.Count; k++)
                    {
                        //calculate its real relative time according to its precedence event
                        //temp.realretime = System.Math.Max(temp.realretime, temp.relativetime+temp.selfweight[k]);
                        if (temp.arrival == true)
                        {
                            int delay = 0;
                            //find whether its section has the same section id
                            foreach (Section se in this.RAN.events.sections[i])
                            {
                                if (temp.sectionid == se.sectionid)
                                {
                                    delay = CalSgDelay(dep(temp), temp, this.RAN.events.intervals[i],
                                        this.RAN.events.sections[i][0].intensity);
                                    break;
                                }
                            }
                            int rela = 0;
                            if (delay == 0)
                            {
                                //for safety
                                temp.delay =
                                    System.Math.Max(temp.delay, this.RAN.eventlines[temp.selfdependencyid[k]].delay - temp.selfweight[k]);
                                temp.realretime = temp.relativetime + temp.delay;
                            }
                            else
                            {
                                //for safety
                                temp.delay = System.Math.Max(delay, 0);//?
                                temp.realretime = temp.relativetime + temp.delay;
                                if (temp.lineno == 4 && temp.sectionid == 74)
                                {
                                    int tony = 0;
                                }
                            }
                        }
                        else
                        {
                            temp.delay =
                                System.Math.Max
                                (this.RAN.eventlines[temp.selfdependencyid[k]].delay - temp.selfweight[k], 0);
                            temp.realretime = temp.relativetime + temp.delay;
                        }
                    }
                    for (int k = 0; k < temp.dependencyid.Count; k++)
                    {
                        if (temp.lineno == 15 && temp.stationid == 4)
                            temp.lineno = 15;
                        temp.delay = System.Math.Max
                            (temp.delay,
                            this.RAN.eventlines[temp.dependencyid[k]].delay - temp.dependentweight[k]);
                        temp.realretime = temp.relativetime + temp.delay;
                    }
                    if (temp.arrival == true || (temp.departure == true && temp.realretime < this.RAN.events.intervals[i].relativeet))
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
                        temp.delay = System.Math.Max
                            (temp.delay, this.RAN.eventlines[temp.selfdependencyid[j]].delay - temp.selfweight[j]);
                    }
                    for (int j = 0; j < temp.dependencyid.Count; j++)
                    {
                        temp.delay = System.Math.Max
                            (temp.delay, this.RAN.eventlines[temp.dependencyid[j]].delay - temp.dependentweight[j]);
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
            //and its arrival relative time is within a stage of affected time
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

                    if (ar.lineno == 14 && ar.stationid == 4)
                        ar.lineno = ar.lineno;
                    if (ar.lineno == 15 && ar.stationid == 4)
                        ar.lineno = ar.lineno;

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
                    if (ar.lineno == 15 && ar.stationid == 4)
                        ar.lineno = ar.lineno;
                    int delay = CalSgDelay(tempevent,ar,it, RAN.events.sections[i-1][0].intensity);
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
                //if (initlist.IndexOf(i) >= 0)
                //{
                //    continue;
                //}
                //else
                //{
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
                //}
            }

            OutputDelay(1, initlist);

            //OutputDelay(1);
            
        }

        private int CalSgDelay(EventNode departure, EventNode arrival, interval it,int intensity)
        {
            if (departure.lineno == 2 && departure.sectionid == 25)
                departure.lineno = departure.lineno;
            int delay=0;
            departure.length = arrival.length;
            double traveltime = (double)(arrival.relativetime-departure.relativetime);
            //speed limit is the reduce speed
            double speedlimit=this.ReduceSpeed(intensity);
            //the maximum speed in normal conditions
            double speedlimith = 100f;
            //the normal speed
            double speednormal = (double)arrival.length / (traveltime/60f);
            //temp-time is the journey time if the traveling period is covered by the hazard
            int time_under_hazard = (int)((double)departure.length / speedlimit*60);
            //d_j is the delay of the departure 
            double d_j = departure.realretime-departure.relativetime;
            //the time of departure is after the hazardous event
            double d_i = (double)System.Math.Max(
                (int)(d_j - ((double)departure.length / (double)speednormal * 60f - (double)departure.length / (double)speedlimith * 60f)),
                0);
            //arrival time considering the delay of the departure
            int arrivaltime = arrival.relativetime + System.Convert.ToInt32(d_i);
            //the expected travel time without considering the hazard
            double eta_i = 0;
            if (d_i== 0)
            {
                eta_i = (double)(departure.length) / (double)speednormal*60 - d_j;
            }
            else
            {
                eta_i = (double)departure.length / (double)speedlimith * 60 ;
            }
            //departure happens after the hazard ends
            if (departure.realretime >= it.relativeet)
            {
                return 0;
            }
            //arrive  before the hazard ends
            if (arrivaltime <= it.relativest)
            {
                return 0;
            }
            //scenario 1
            //the departure happens after the event begins
            if (departure.realretime >= it.relativest)
            {
                //minute
                //to see whether or not the hazard ends before the train arrives
                //scenario 1.1
               // the arrival happens first and then the hazard ends
                if (departure.realretime + time_under_hazard <= it.relativeet)
                {  
                    //the entire journey will be affected
                    //the first part is the realized departure time; the second is the scheduled time
                    //this value will surely be larger than zero
                    delay = Math.Max((time_under_hazard + departure.realretime) - arrival.relativetime, 0);
                    if (delay == 0)
                    {
                        MessageBox.Show
                            ("something is wrong. The delay under the influence of the hazard for the whole journey will surely be larger than zero.");
                    }
                }
                //scenario 1.2
                //the hazard ends first and then the train needs to run in full speed for the remaining segment
                else
                {  
                    //半程收到影响
                    //影响下的距离
                    double templenght =((double)(it.relativeet - departure.realretime) /60f* speedlimit);
                    //追赶的时间
                    double time2 = ((double)(departure.length - templenght) / speedlimith*60f);
                    //总时间
                    double total = time2 + it.relativeet;
                    //延误
                    //delay = System.Convert.ToInt32(total - (arrivaltime - departure.realretime)); //?
                    delay = System.Math.Max(0,(int)total - arrival.relativetime);
                }
            }
            //scenario 2
            //the departure happens before the hazard begins
            else
            {
                double epsilon=0;
                //the time between the departure and the beginning of the hazard
                double delta_time = it.relativest - departure.realretime;
                //Departure happens before the hazards begins
                //the train will travel maximum speed due to the delay of event j for part of or the entire journey
                //the length traveled before the event starts is denoted by epsilon
                if(d_i>0)
                {
                    epsilon = speedlimith * delta_time / 60f ;              
                }
                else
                {                    
                    double delta_speed = speedlimith - speednormal;
                    epsilon = speedlimith * Math.Min(delta_time / 60f, speednormal * d_j / 60f / delta_speed) +
                        speednormal * Math.Max(0, delta_time / 60f - speednormal * d_j / 60f / delta_speed);    
                }
                //事件开始的半段
                double portion = ((double)(it.relativest-departure.realretime))/((double)(arrivaltime - departure.realretime));
                //剩下的距离
                double templength = (double)departure.length * (1-portion);
                templength = departure.length-epsilon;
                //剩下的距离全程受影响
                //scenario 2.1
                if (epsilon+speedlimit*it.last >= departure.length) //the length totally covered is larger than the link length
                {
                    int timeafter = (int)(templength / speedlimit*60f);
                    int total = timeafter+it.relativest;
                    //延误
                    delay = total - (arrivaltime - departure.realretime);//?
                    delay = total-arrival.relativetime;
                    if (delay < 0)
                    {
                        //MessageBox.Show("delay line 1240");
                        delay = 0;
                    }
                }
                //scenario 2.2
                else
                {
                    //剩下的路程也仅仅是半程收影响              
                    //影响下的距离
                    double templenght2 = ((double)(it.relativeet - it.relativest) )* speedlimit;
                    //追赶的时间
                    int time2 = (int)((templength - templenght2) / speedlimith);
                    //总时间
                    int total = time2 + it.relativeet -it.relativest+departure.realretime;
                    //延误
                    delay = total - (arrivaltime - departure.realretime);
                    delay = total - arrival.relativetime;
                    if (delay < 0)
                    {
                        //MessageBox.Show("delay line 1240");
                        delay = 0;
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
                            this.listView3.Items.Add("(" + en.cycleNo + ")" 
                                + "     Delay of Arrival Event of line " + this.RAN.linename[en.lineno-1]
                                + " at station " + this.RAN.Stations[en.stationid-1].name + " is " + en.delay);
                        }
                        else
                        {
                            this.listView3.Items.Add("(" + en.cycleNo + ")"
                                + "     Delay of Departure Event of line " + this.RAN.linename[en.lineno - 1]
                                + " at station " + this.RAN.Stations[en.stationid - 1].name + " is " + en.delay);
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

            //Visualizer.copy_RAN = this.RAN;
            //RandColor();
            //Visualizer.Visualize(graph, "dependency");
            //Process photoViewer = new Process();
            //Process.Start(@"c:\\temp\\dependency.jpg");

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

            //Visualizer.Visualize(sp_graph, "global");
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

        private void simulationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Simulate();
        }

        /// <summary>
        /// simulate the movement of the vehicle
        /// </summary>
        private void Simulate()
        {
            Intial(check,nocycle);

            bool finished = false;
            double resolution = 1;// (seconds)
            DateTime simutime = this.RefTime;

            while(finished == false)
            {
                //update position
                foreach (Train train in this.Trains)
                {
                    updateposition(train,resolution,simutime);
                }
                //update speed

                simutime = simutime.AddSeconds(1);
                finished = testFinish(this.Trains);
            }

            WriteSimuResult();
        }
        /// <summary>
        /// write the simulation results
        /// </summary>
        private void WriteSimuResult()
        {
            SimuResult form = new SimuResult();

            foreach (Train train in this.Trains)
            {
                int index = 0;
                foreach (EventNode en in train.schedule)
                {
                    form.AddEvent(en, train.act_times[index], this.RAN);
                    index++;
                }
            }

            //write results by eventlines
            WriteResultEvents();

            form.ShowDialog();
        }

        private void WriteResultEvents()
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\Dali\Desktop\ResultByEvent.txt");
            foreach(EventNode en in this.RAN.eventlines)
            {
                //find the corresponding event in train
                EventNode en2 = FindTrainEvent(en);
                string line1;
                string line2;
                if(en2.arrival == true)
                {
                    line1=RAN.linename[en.lineno-1]+" Arrival at "+this.RAN.Stations[en2.stationid-1].name + "_Cycle_"+en.cycleNo;
                    line2 = "           Schedule time: "+en.st.ToString()+"  ||  "+ "Actual time: "+en2.st.ToString()+"  ||  "+
                        "Delay: "+en2.delay.ToString();
                    if (en2.primary == true) 
                        line2 = line2 + " PRIMARY";
                }
                else
                {
                    line1 = RAN.linename[en.lineno - 1] + " Departure at " + this.RAN.Stations[en2.stationid - 1].name + "_Cycle_" + en.cycleNo;
                    line2 = "           Schedule time: " + en.st.ToString() + "  ||  " + "Actual time: " + en2.st.ToString() + "  ||  " +
                        "Delay: " + en2.delay.ToString();
                }
                file.WriteLine(line1);
                file.WriteLine(line2);
            }
            file.Close();
        }

        private EventNode FindTrainEvent(EventNode en)
        {
            foreach (Train train in this.Trains)
            {
                if (train.routeid == en.lineno)
                {
                    if (en.cycleNo == train.cycle)
                    {
                        foreach(EventNode en2 in train.act_times)
                        {
                            if(en2.stationid == en.stationid && en2.arrival ==en.arrival)
                                return en2;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// update position with events
        /// </summary>
        /// <param name="train"></param>
        /// <param name="resolution"></param>
        /// <param name="simutime"></param>
        private void updateposition(Train train, double resolution,DateTime simutime)
        {
            //on the link
            if (train.arrived == true)
                return;
            if(train.onlink > 0)
            {
                //resolution is in second
                
                train.location = train.speed * resolution/3600+train.location;
                //arrive at the "to" station
                if(train.location>=train.route[train.station_passed].length)
                {
                    if (train.routeid == 15 && train.tostation == 4)
                    {
                        train.routeid = train.routeid;
                    }
                    //check whether or not the platform is available
                    //available
                    //arrive at the station has to follow the specific order
                    if(this.RAN.Stations[train.tostation-1].instation_train == null
                        && train.routeid == this.RAN.Stations[train.tostation-1].routes[this.RAN.Stations[train.tostation-1].nextroute] //make sure the sequence of arrivals correct
                        && train.cycle == this.RAN.Stations[train.tostation-1].cycles[this.RAN.Stations[train.tostation-1].cycle]) // make sure the cycle also true
                    {
                        if (simutime >= train.route[train.station_passed].at && //only arrive after schedule time
                            simutime >= this.RAN.Stations[train.tostation - 1].earliest_arrive) //only arrive after the platform is clear
                        {
                            //update the actual times of arrivals
                            TimeSpan tempsp = new TimeSpan();
                            tempsp = simutime - this.RefTime;
                            int index = train.station_passed*2 + 1;
                            train.act_times[index].relativetime = (int)tempsp.TotalMinutes;                            
                            train.act_times[index].delay = train.act_times[index].relativetime-train.schedule[index].relativetime;
                            train.act_times[index].st = simutime;

                            train.onstation = train.tostation;
                            train.onlink = -1;
                            train.station_passed = train.station_passed + 1;
                            train.location = 0;
                            this.RAN.Stations[train.tostation-1].instation_train = train;
                            //the earliest release time of the train
                            if (train.onstation != train.destination) //no need to add earliest release
                            {
                                this.RAN.Stations[train.tostation - 1].earliest_relase = simutime.AddMinutes(Dwell);
                            }
                            train.speed = 0;
                            //set the indicators for the next arrival
                            this.RAN.Stations[train.tostation - 1].nextroute++;
                            this.RAN.Stations[train.tostation - 1].cycle++;

                            //if the train is on station, then set the earliest arrival time of the next train
                            if (train.onstation == train.destination)
                            {
                                //arrival constraint
                                this.RAN.Stations[train.tostation - 1].earliest_arrive = simutime.AddMinutes(AAheadway);
                                this.RAN.Stations[train.tostation - 1].instation_train = null;
                                train.arrived = true;
                            }
                        }
                        else
                        {
                            //arrived earlier than schedule time
                            //then wait
                            train.waiting = true;
                            train.speed = 0;
                        }
                    }
                    //unavailable
                    else
                    {                        
                        train.waiting = true;
                        train.speed = 0;
                    }
                }
                //train is still going
                else
                {
                    if (train.tostation == 9 && train.routeid == 17)
                        train.tostation = train.tostation;
                    double reduced_speed=0;
                    int linkindex=System.Convert.ToInt32(this.RAN.hashtable_link[train.onlink]);
                    if (train.routeid == 2 && train.tostation == 5)
                    {
                        train.routeid = train.routeid;
                    }
                    foreach (Hazard hazard in this.RAN.Links[linkindex].Link_hazards)
                    {
                        if(hazard.startime <= simutime && hazard.endtime>=simutime)
                        {
                            reduced_speed = ReduceSpeed(hazard.intensity);
                            goto
                                affected;                            
                        }
                    }
                    goto
                        unaffected;
                affected:
                    {
                        train.speed = reduced_speed; //update the speed
                        train.act_times[train.station_passed * 2 + 1].primary = true;
                        return;
                    }
                unaffected:
                    {
                        //check whether the train has already compensated the delay
                        
                        double normal_speed = (double)train.route[train.station_passed].length
                                / ((double)train.route[train.station_passed].travetime / 60);
                        if (checkdelay(train,normal_speed,train.location,simutime) == true)//still delayed
                        {
                            train.speed = train.route[train.station_passed].speedlimit;
                        }
                        else
                        {
                            train.speed = normal_speed;
                        }
                    }
                }
            }
            //train is in station
            else
            {
                //link id in the link list
                int outbound_link_id = (int)this.RAN.hashtable_link[train.route[train.station_passed].linkid];
                
                //whether the time has passed the departure time
                if ((simutime >= this.RAN.Stations[train.onstation-1].earliest_relase) // (dwell time) larger than the release time
                    && simutime >= train.route[train.station_passed].dt //larger than the schedule time
                    && simutime >= this.RAN.Links[outbound_link_id].earliest_departure   // departure/departure headway
                    && train.routeid == this.RAN.Links[outbound_link_id].routes[this.RAN.Links[outbound_link_id].nextroute]
                    && train.cycle == this.RAN.Links[outbound_link_id].cycles[this.RAN.Links[outbound_link_id].cycle]) // (DD time) 
                {
                   
                    //update the actual times of departures
                    TimeSpan tempsp = new TimeSpan();
                    tempsp = simutime - this.RefTime;
                    int index = train.station_passed * 2 ;
                    train.act_times[index].relativetime = (int)tempsp.TotalMinutes;                
                    train.act_times[index].delay = train.act_times[index].relativetime - train.schedule[index].relativetime;
                    train.act_times[index].st = simutime;
                                        
                    train.onlink = train.route[train.station_passed].linkid;
                    train.tostation = train.route[train.station_passed].tono;
                    train.fromstation = train.onstation;
                    if (train.routeid == 17 && train.tostation==9)
                        train.routeid = train.routeid;
                    //speed is in km per hour
                    //length is in km
                    //travel time is in minutes (this is the original speed)
                    train.speed = (double)train.route[train.station_passed].length
                        / ((double)train.route[train.station_passed].travetime/60);
                    //delay happened has to increase the speed) 
                    if (train.act_times[index].delay>0)
                    {
                        train.speed =train.route[train.station_passed].speedlimit;
                    }
                    // update the wait index 
                    this.RAN.Stations[train.onstation - 1].wait_index
                        = Math.Max(0,this.RAN.Stations[train.onstation - 1].wait_index - 1);
                    // set the earliest arrive time using the arrival departure headway
                    this.RAN.Stations[train.onstation - 1].earliest_arrive
                        = simutime.AddMinutes(this.ADheadway);
                    // set the earliest train using the same outbound link                  
                    this.RAN.Links[outbound_link_id].earliest_departure
                        = simutime.AddMinutes(this.DDheadway);
                    this.RAN.Stations[train.onstation - 1].instation_train = null;
                                        
                    this.RAN.Links[outbound_link_id].nextroute++;
                    this.RAN.Links[outbound_link_id].cycle++;

                    train.onstation = -1;
                }
                else //unable to depart
                {
                    //do nothing
                }
            }
        }

        //check whether the speed of the train
        //return true, full speed
        // return false, normal speed
        private bool checkdelay(Train train,double normalspeed,double length,DateTime dt)
        {
            //check the scheduled time (in minutes)
            double schedule = train.schedule[train.station_passed * 2].relativetime+length/normalspeed*60;
            //CURRENT TIme
            TimeSpan actual = new TimeSpan();
            actual = dt - this.RefTime;

            if (schedule - actual.TotalMinutes < 0)
            {
                return true;
            }
            return false;
        }



        //according to the intensity of the hazard, determine the reduced traveling speed
        //unit: km/h
        private double ReduceSpeed(int p)
        {
            //lowest level
            return 60 - p * 10; 
        }

        private bool testFinish(List<Train> list)
        {
            List<int> index = new List<int>() ;
            int indexer = 0;
            foreach (Train train in list)
            {
                if (train.destination == train.onstation)
                {
                    index.Add(indexer);
                }
                indexer++;
            }
            if (index.Count == list.Count)
            {
                return true;
            }
            else
            {
                foreach (int i in index)
                {
                    list[i].arrived = true;
                }
                return false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SimulateNoEvent();
        }

        private void SimulateNoEvent()
        {
            Intial(check,nocycle);
            //clear events
            foreach(Link lk in this.RAN.Links)
            {
                lk.Link_hazards.Clear();
            }

            bool finished = false;
            double resolution = 1;// (seconds)
            DateTime simutime = this.RefTime;

            while (finished == false)
            {
                //update position
                foreach (Train train in this.Trains)
                {
                    this.updateposition(train, resolution, simutime);
                }
                //update speed

                simutime = simutime.AddSeconds(1);
                finished = testFinish(this.Trains);
            }

            WriteSimuResult();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Simulate();
        }
                
        public List<EventNode> DeepCopy(List<EventNode> input)
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, input);
            ms.Position = 0;
            return (List<EventNode>)bf.Deserialize(ms);
        }

        public List<LineNode> DeepCopy(List<LineNode> input)
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, input);
            ms.Position = 0;
            return (List<LineNode>)bf.Deserialize(ms);
        }
    }

    [Serializable]
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
        public int period;
        public DateTime endtime;
        public int repetition;

        public int linkid { get; set; }
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

    [Serializable]
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

        public int periodicity = 24;
        public bool primary = false;
        public DateTime endtime;
        public int repetition;

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
        public Hashtable hashtable_link = new Hashtable();
        public List<Link> Links = new List<Link>();
        public List<Station> Stations = new List<Station>();
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
