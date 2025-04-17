using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEAutomation.Models
{
    public class PageImage
    {
        public int Height { get; set; }
        public int Width { get; set; }
        public int DpiX { get; set; }
        public int DpiY { get; set; }
        public string ZipKey { get; set; }
        public string ImageKey { get; set; }
    }
}
