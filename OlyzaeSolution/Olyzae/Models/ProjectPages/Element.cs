using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NihonUnisys.Olyzae.Models.ProjectPages
{
    public class Element
    {
        public int type { get; set; }
        public string text { get; set; }
        public int image { get; set; }
        public int imageAlignment { get; set; }
        public string header { get; set; }
        public int headerSize { get; set; }
    }
}