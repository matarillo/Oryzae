using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;


namespace NihonUnisys.Olyzae.Models
{
    /// <summary>
    /// 管理者機能の新規ユーザー追加で使用するビューモデルです。
    /// </summary>
    public class CreateAccountUserViewModel
    {
        [Required]
        [Display(Name = "ユーザID")]
        public string UserName { get; set; }

        [Required]
        [Display(Name = "ユーザ名")]
        public string DisplayName { get; set; }

        [Required]
        [Display(Name = "企業名")]
        public string CompanyName { get; set; }

        [Display(Name = "所属する組織名")]
        public string Organization { get; set; }

        [Display(Name = "初期パスワード")]
        public string RawPassword { get; set; }
    }
}