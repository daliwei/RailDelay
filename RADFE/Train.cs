using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RADFE
{
    public class Train
    {
        public double location = 0;
        public double speed = 0;
        public int tostation = 0;
        public int fromstation = 0;
        public int onlink = -1;
        public int onstation = 0;
        public int destination = 0;

        public int cycle = 0;

        public List<LineNode> route = new List<LineNode>();
        public List<EventNode> schedule = new List<EventNode>();
        public List<EventNode> act_times = new List<EventNode>();

        public int routeid;

        public int station_passed = 0;
        public bool waiting = false;
        public int wait_index;

        public bool arrived = false;
        public int period;

        
    }
}
