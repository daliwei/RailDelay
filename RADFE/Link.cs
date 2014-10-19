using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RADFE
{
    //a directional link 
    public class Link
    {
        public int id;
        public int fromstation;
        public int tostation;


        public List<Hazard> Link_hazards = new List<Hazard>();
        public double max_speed;
        public DateTime earliest_departure;
        public int nextroute;
        public int cycle;
        public List<int> routes;
        public List<int> cycles;
    }
}
