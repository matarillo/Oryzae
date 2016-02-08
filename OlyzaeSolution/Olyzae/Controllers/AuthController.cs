using NihonUnisys.Olyzae.Models;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;

namespace NihonUnisys.Olyzae.Controllers
{
    [Authorize]
    public class AuthController : Controller
    {
        private Entities db = new Entities();

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel account, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.FirstOrDefault(x => x.UserName == account.UserName);
                if (user != null && user.VerifyPassword(account.Password) && !string.IsNullOrEmpty(user.Role))
                {
                    FormsAuthentication.SetAuthCookie(account.UserName, false);
                    return RedirectToLocalOrHome(returnUrl, user);
                }
            }

            ModelState.AddModelError("", "指定されたユーザー名またはパスワードが正しくありません。");
            return View(account);
        }

        private ActionResult RedirectToLocalOrHome(string returnUrl, User user)
        {
            if ((!string.IsNullOrEmpty(returnUrl))
                && (!string.Equals(returnUrl, "/", System.StringComparison.OrdinalIgnoreCase))
                && (Url.IsLocalUrl(returnUrl)))
            {
                return Redirect(returnUrl);
            }

            return RedirectToHome(user);
        }

        private ActionResult RedirectToHome(User user)
        {
            switch (user.Role)
            {
                case "AccountUser":
                    return RedirectToAction("Index", "CompanyHome");
                case "ParticipantUser":
                    return RedirectToAction("Index", "Home");
                case "Administrator":
                    return RedirectToAction("Index", "AdministratorHome");
                default:
                    throw new SystemException();
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Logout(string returnUrl)
        {
            FormsAuthentication.SignOut();
            // プロトではセッションを使用していないが、後で使うようになるかもしれないので消しておく
            this.Session.Abandon();
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public ActionResult SignUpAccountUser()
        {
            SetCompanies(null);
            return View();
        }

        // POST: /Auth/SignUpAccountUser
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult SignUpAccountUser([Bind(Include = "UserName,RawPassword,DisplayName,Organization")] AccountUser user, int companyId)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(user.RawPassword))
                {
                    ModelState.AddModelError("RawPassword", "パスワードを入力してください。");
                    SetCompanies(companyId);
                    return View(user);
                }

                var registered = db.Users.Any(x => x.UserName == user.UserName);
                if (registered)
                {
                    ModelState.AddModelError("UserName", "指定されたユーザー名は使われています。");
                    SetCompanies(companyId);
                    return View(user);
                }

                var company = db.Companies.FirstOrDefault(x => x.ID == companyId);
                if (company == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                user.UserType = 1; // TODO: 不要であれば削除
                user.Company = company;
                user.Password = Models.User.GenerateHashedPassword(user.RawPassword);
                db.Users.Add(user);
                db.SaveChanges();
                return RedirectToAction("Login");
            }

            return View(user);
        }

        internal void SetCompanies(int? selectedId)
        {
            // TODO: 正しい会社を選択させるサインアッププロセスの検討。
            // 企業の管理者アカウントによる承認が必要。

            var companies = db.Companies.AsEnumerable();
            ViewBag.Companies = companies.Select(x => new SelectListItem
            {
                Text = x.CompanyName,
                Value = x.ID.ToString(),
                Selected = (selectedId == x.ID)
            });
        }

        [AllowAnonymous]
        public ActionResult SignUpParticipantUser()
        {
            return View();
        }

        // POST: /Auth/SignUpParticipantUser
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult SignUpParticipantUser([Bind(Include = "UserName,RawPassword,DisplayName,OnlineName,Kana,Gender,BirthDay,AcademicRecord,Departments,AcademicYear,EMailAddress,PhoneNumber,Zip,State,City,StreetAddress1,StreetAddress2,ReturningHomeZip,ReturningHomeState,ReturningHomeCity,ReturningHomeStreetAddress1,ReturningHomeStreetAddress2,Academic,Happpiness,CareerAnchors,Mentor,Answer")] ParticipantUser user, string[] answer)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(user.RawPassword))
                {
                    ModelState.AddModelError("RawPassword", "パスワードを入力してください。");
                    return View(user);
                }

                var registered = db.Users.Any(x => x.UserName == user.UserName);
                if (registered)
                {
                    ModelState.AddModelError("UserName", "指定されたユーザー名は使われています。");
                    return View(user);
                }

                // 質問回答を配列からカンマ区切りに入れ直し
                StringBuilder sb = new StringBuilder(string.Empty);
                int i = 0;
                for (; i < answer.Length - 1; i++)
                {
                    // @Html.checkBoxで自動生成されるhidden項目のvalueの既定値が"false"なので、弾く
                    if (answer[i] == "false")
                        continue;

                    sb.Append(answer[i]);
                    sb.Append(",");
                }
                sb.Append(answer[i]);
                user.Answer = sb.ToString();
                user.UserType = 3; // TODO: 不要であれば削除
                user.Password = Models.User.GenerateHashedPassword(user.RawPassword);

                db.Users.Add(user);
                db.SaveChanges();
                return RedirectToAction("Login");
            }

            return View(user);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
