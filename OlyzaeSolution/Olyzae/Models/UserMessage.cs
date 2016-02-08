using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace NihonUnisys.Olyzae.Models
{
    /// <summary>
    /// メッセージと受信者を関連付けるオブジェクト。
    /// 受信者ごとの既読・未読管理に使用できます。
    /// </summary>
    public partial class UserMessage
    {
        public int ID { get; set; }

        /// <summary>
        /// メッセージの受信者が、そのメッセージを読んだかどうかを表すフラグ。
        /// </summary>
        [Required]
        public bool HasRead { get; set; }

        /// <summary>
        /// メッセージ。
        /// </summary>
        [Required]
        public virtual Message Message { get; set; }

        /// <summary>
        /// 受信者。
        /// </summary>
        [Required]
        public virtual User User { get; set; }
    }
}