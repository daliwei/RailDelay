using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RADFE
{
    public class Station
    {
        public Train instation_train=null;
        public DateTime earliest_relase=DateTime.MinValue;
        public int wait_index;
        public string name;
        public int id;
        public DateTime earliest_arrive = DateTime.MinValue;
        public int nextroute;
        public int cycle;
        public List<int> routes;
        public List<int> cycles;

        
    }
}
