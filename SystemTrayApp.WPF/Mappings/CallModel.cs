using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShinyCall.Mappings
{
    public class CallModel
    {
        public int id { get; set; }
        public String caller { get; set; }
        public String status { get; set; }
        public String time { get; set; }
    }
}
