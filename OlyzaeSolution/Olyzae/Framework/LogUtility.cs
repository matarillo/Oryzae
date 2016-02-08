using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace NihonUnisys.Olyzae.Framework
{
    public static class LogUtility
    {
        [Conditional("DEBUG")]
        public static void DebugWriteQuery(IQueryable query)
        {
#if DEBUG
            // プロトタイプではデバッガに出力
            var oQuery = query as ObjectQuery;
            if (oQuery != null)
            {
                System.Diagnostics.Trace.WriteLine(oQuery.ToTraceString());
            }
            else
            {
                System.Diagnostics.Trace.WriteLine(query.ToString());
            }
#endif
        }
    }
}