using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NihonUnisys.Olyzae.Framework
{
    public class ExecutionContext
    {
        public static Func<ExecutionContext> Create;

        private HttpContextBase _context;

        public ExecutionContext(HttpContextBase context)
        {
            _context = context;
            if (!_context.Items.Contains("Now"))
            {
                _context.Items["Now"] = DateTime.Now;
            }
        }

        public void SetCurrentUserFromIdentity()
        {
            // ユーザー情報
            // 企業ユーザーの場合は会社情報も取得
            var p = _context.User.Identity;
            if (p.IsAuthenticated)
            {
                using (var db = new Models.Entities())
                {
                    db.Configuration.ProxyCreationEnabled = false;
                    // TODO: UserNameの一意性保証
                    this.CurrentUser = db.Users.FirstOrDefault(x => x.UserName == p.Name);
                    var au = this.CurrentUser as Models.AccountUser;
                    if (au != null)
                    {
                        db.Entry(au).Reference(x => x.Company).Load();
                    }
                }
            }
            if (this.CurrentUser == null)
            {
                // 未認証とする
                this.CurrentUser = Models.User.Anonymous;
                if (p.IsAuthenticated)
                {
                    // 認証クッキーが有効だが、DBにユーザーが存在しない場合の対処
                    var identity = new System.Security.Principal.GenericIdentity("");
                    var principal = new System.Security.Principal.GenericPrincipal(identity, null);
                    _context.User = principal;
                }
            }
        }

        public void PreRequestHandlerExecute()
        {
        }

        public void PostRequestHandlerExecute()
        {
        }

        public Models.User CurrentUser
        {
            get { return (Models.User)_context.Items["CurrentUser"]; }
            set { _context.Items["CurrentUser"] = value; }
        }

        public DateTime Now
        {
            get { return (DateTime)_context.Items["Now"]; }
            set { _context.Items["Now"] = value; }
        }
    }
}