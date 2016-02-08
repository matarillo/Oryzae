using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace NihonUnisys.Olyzae.Models
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "ID")]
        public string UserName { get; set; }

        /// <summary>
        /// ハッシュ化されていない生のパスワード。
        /// </summary>
        [Required]
        [Display(Name = "パスワード")]
        public string Password { get; set; }
    }
}