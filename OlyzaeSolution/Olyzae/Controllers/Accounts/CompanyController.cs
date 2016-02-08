using NihonUnisys.Olyzae.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using NihonUnisys.Olyzae.Models;

namespace NihonUnisys.Olyzae.Controllers.Accounts
{
    [Authorize(Roles = "AccountUser")]
    public class CompanyController : Controller
    {
        private Entities db = new Entities();
        private ExecutionContext ctx = ExecutionContext.Create();
        // GET: Company
        public ActionResult Index()
        {
            // 企業ユーザーの場合は会社情報も取得されているので、それを使って絞り込む
            var currentUser = ctx.CurrentUser as AccountUser;
            var accountUsers = db.Users.OfType<AccountUser>().Where (x => x.Company.ID == currentUser.Company.ID).ToList();
            return View(accountUsers);
        }

        // GET: Company/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AccountUser accountUser = db.Users.Find(id) as AccountUser;
            if (accountUser == null)
            {
                return HttpNotFound();
            }
            return View(accountUser);
        }

        // GET: Company/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Company/Create
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,UserName,RawPassword,DisplayName,Organization")] AccountUser accountUser)
        {
            if (string.IsNullOrEmpty(accountUser.RawPassword))
            {
                ModelState.AddModelError("RawPassword", "パスワードを入力してください。");
                return View(accountUser);
            }

            if (ModelState.IsValid)
            {
                // 企業ユーザーの場合は会社情報も取得されているので、それを使って絞り込む
                var currentUser = ctx.CurrentUser as AccountUser;

                db.Companies.Attach(currentUser.Company);
                accountUser.UserType = 1; // TODO: 不要であれば削除
                accountUser.Company = currentUser.Company;
                accountUser.RawPassword = Models.User.GenerateHashedPassword(accountUser.RawPassword);
                db.Users.Add(accountUser);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(accountUser);
        }

        // GET: Company/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var currentUser = ctx.CurrentUser as AccountUser;
            AccountUser accountUser = db.Users.Find(id) as AccountUser;
            if ((accountUser == null)||(accountUser.ID != currentUser.ID))
            {
                return HttpNotFound();
            }
            return View(accountUser);
        }

        // POST: Company/Edit/5
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,UserName,RawPassword,DisplayName,Organization")] AccountUser accountUser)
        {
            if (!ModelState.IsValid)
            {
                return View(accountUser);
            }

            // パスワード変更の対応のため、
            // 受け取ったaccountUserをそのまま使うのではなく
            // DBから取得しなおす。

            var currentUser = (AccountUser)ctx.CurrentUser;
            var target = db.Users
                .OfType<AccountUser>()
                .Where(u => u.ID == accountUser.ID && u.Company.ID == currentUser.Company.ID)
                .FirstOrDefault();
            if (target == null)
            {
                return RedirectToAction("Index");
            }

            target.UserName = accountUser.UserName;
            target.DisplayName = accountUser.DisplayName;
            target.Organization = accountUser.Organization;
            if (!string.IsNullOrEmpty(accountUser.RawPassword))
            {
                target.Password = Models.User.GenerateHashedPassword(accountUser.RawPassword);
            }
            db.Entry(target).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET: Company/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AccountUser accountUser = db.Users.Find(id) as AccountUser;
            if (accountUser == null)
            {
                return HttpNotFound();
            }
            return View(accountUser);
        }

        // POST: Company/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            AccountUser accountUser = db.Users.Find(id) as AccountUser;
            db.Users.Remove(accountUser);
            db.SaveChanges();
            return RedirectToAction("Index");
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
