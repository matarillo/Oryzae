using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace NihonUnisys.Olyzae.Models
{
    /// <summary>
    /// スレッドの読み書きが可能な参加者を限定するための関連オブジェクト。
    /// </summary>
    public partial class ParticipantUserThread
    {
        public ParticipantUserThread()
        {
        }

        public int ID { get; set; }

        [Required]
        public virtual ParticipantUser ParticipantUser { get; set; }
        [Required]
        public virtual Thread Thread { get; set; }
    }
}