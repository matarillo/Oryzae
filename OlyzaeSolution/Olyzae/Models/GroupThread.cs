using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace NihonUnisys.Olyzae.Models
{
    /// <summary>
    /// グループのコミュニケーションに使用されるスレッド。
    /// </summary>
    public partial class GroupThread : Thread
    {
        public GroupThread()
        {
        }

        /// <summary>
        /// スレッドのメッセージを読み書きできるグループ。
        /// </summary>
        [Required]
        public virtual Group Group { get; set; }
    }
}