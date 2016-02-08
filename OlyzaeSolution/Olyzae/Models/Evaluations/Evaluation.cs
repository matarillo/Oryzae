using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NihonUnisys.Olyzae.Models.Evaluations
{
    public class Evaluation
    {
        public IList<Item> items { get; set; }

        public static Evaluation FromJsonString(string json)
        {
            return JsonConvert.DeserializeObject<Evaluation>(json);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}