using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEAutomation.Models
{
    public class Attachment
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public bool IsActive { get; set; }
        public EntityInfo AssignedTo { get; set; }
        public long FileSize { get; set; }
        public bool IsRemoved { get; set; }
        public EntityInfo CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<Page> Pages { get; set; }
    }
}
