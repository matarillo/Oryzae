//------------------------------------------------------------------------------
// <auto-generated>
//    このコードはテンプレートから生成されました。
//
//    このファイルを手動で変更すると、アプリケーションで予期しない動作が発生する可能性があります。
//    このファイルに対する手動の変更は、コードが再生成されると上書きされます。
// </auto-generated>
//------------------------------------------------------------------------------

namespace NihonUnisys.Olyzae.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    public partial class Group
    {
        public Group()
        {
            this.ParticipantUsers = new HashSet<ParticipantUserGroup>();
            this.GroupThreads = new HashSet<GroupThread>();
        }

        public int ID { get; set; }
        [Display(Name = "グループ名")]
        [Required]
        public string GroupName { get; set; }

        public virtual ICollection<ParticipantUserGroup> ParticipantUsers { get; set; }
        public virtual ICollection<GroupThread> GroupThreads { get; set; }

        /// <summary>
        /// 指定したユーザーが所属しているかどうかを返します。
        /// </summary>
        /// <param name="userId">ユーザーID。</param>
        /// <returns>指定したユーザーが所属している場合はtrue。</returns>
        /// <remarks>
        /// このメソッドを呼び出す前に、関連オブジェクト(ParticipantUserGroup)をDBから取得しておく必要があります。
        /// </remarks>
        public bool ContainsUser(int userId)
        {
            return this.ParticipantUsers.Any(pug => pug.ParticipantUser.ID == userId);
        }
    }
}
