using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NihonUnisys.Olyzae.Models
{
    public partial class ProjectThread : Thread
    {
        /// <summary>
        /// プロジェクトを主催している企業と参加者とのコミュニケーションに使用するスレッド。
        /// 企業から参加者へのお知らせとして使用します。
        /// </summary>
        public ProjectThread()
        {
        }

        /// <summary>
        /// スレッドのメッセージを読み書きできる企業と参加者を結びつけているプロジェクト。
        /// </summary>
        public virtual Project Project { get; set; }
    }
}