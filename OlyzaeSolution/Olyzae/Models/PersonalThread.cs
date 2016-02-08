using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace NihonUnisys.Olyzae.Models
{

    public partial class PersonalThread : Thread
    {
        /// <summary>
        /// 参加者と友人のコミュニケーションに使用するスレッド。
        /// </summary>
        /// <remarks>
        /// Durationプロパティは使用しない。
        /// </remarks>
        public PersonalThread()
        {
        }

        /// <summary>
        /// スレッドの所有者。
        /// スレッドのメッセージを読み書きできるのは、このスレッドの所有者とその友人。
        /// </summary>
        public virtual ParticipantUser OwnerUser { get; set; }
    }
}