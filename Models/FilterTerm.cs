using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEAutomation.Models
{
    public class FilterTerm
    {
        public string canonicalName { get; set; }
        public object value { get; set; }
        public string matchType { get; set; }
        public bool include { get; set; } = true;
        public string precision { get; set; } 
    }
}
