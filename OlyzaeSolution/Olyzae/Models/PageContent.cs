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
    
    public partial class PageContent
    {
        public int ID { get; set; }
        public System.Guid DocumentID { get; set; }

        [Required]
        public virtual ProjectPage ProjectPage { get; set; }
    }
}
