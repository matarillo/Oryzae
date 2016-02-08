using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NihonUnisys.Olyzae.Models
{
    public enum TimelineType
    {
        /// <summary>マイページのスレッド</summary>
        PersonalThread = 0,

        /// <summary>友人のマイページのスレッド</summary>
        FriendThread = 1,

        /// <summary>プロジェクトからのお知らせ</summary>
        ProjectThread = 2,
    }
}