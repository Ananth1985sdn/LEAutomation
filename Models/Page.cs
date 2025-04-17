using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEAutomation.Models
{
    public class Page
    {
        public PageImage PageImage { get; set; }
        public PageImage ThumbnailImage { get; set; }
        public long FileSize { get; set; }
        public int Rotation { get; set; }
    }
}
