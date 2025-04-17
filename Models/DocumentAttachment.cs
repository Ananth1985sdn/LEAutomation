using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEAutomation.Models
{
    public class DocumentAttachment
    {
        public string Id { get; set; }
        public List<DocumentPage> Pages { get; set; }
        public List<string> originalUrls { get; set; }

    }
}
