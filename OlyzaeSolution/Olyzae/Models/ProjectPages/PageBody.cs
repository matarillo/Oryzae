using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace NihonUnisys.Olyzae.Models.ProjectPages
{
    public class PageBody
    {
        public int backgroundImage { get; set; }
        public IList<Section> sections { get; set; }

        public static PageBody FromJsonString(string json)
        {
            return JsonConvert.DeserializeObject<PageBody>(json);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}