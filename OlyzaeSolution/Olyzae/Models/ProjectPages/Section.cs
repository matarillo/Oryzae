using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NihonUnisys.Olyzae.Models.ProjectPages
{
    public class Section
    {
        public string title { get; set; }
        public IList<IDictionary<string, string>> items { get; set; }

        public static Section FromJsonString(string json)
        {
            return JsonConvert.DeserializeObject<Section>(json);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}