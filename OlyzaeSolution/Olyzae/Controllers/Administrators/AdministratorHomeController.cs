using NihonUnisys.Olyzae.Framework;
using NihonUnisys.Olyzae.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace NihonUnisys.Olyzae.Controllers.Administrators
{
    public class AdministratorHomeController : Controller
    {
        private Entities db = new Entities();
        private ExecutionContext ctx = ExecutionContext.Create();

        // GET: AdministratorHome
        public ActionResult Index()
        {
            if (!IsAdministrator())
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            }

            // 企業一覧を取得する
            return View(db.Companies.ToList());
        }

        // GET: AdministratorHome/Details/5
        public ActionResult Details(int? companyId)
        {
            // TODO:未実装
            return RedirectToAction("Index");
            //return View();
        }

        // GET: AdministratorHome/CreateAccountUser
        public ActionResult CreateAccountUser()
        {
            return View();
        }

        // POST: AdministratorHome/CreateAccountUser
        [HttpPost]
        public ActionResult CreateAccountUser([Bind(Include = "UserName,DisplayName,CompanyName,Organization,RawPassword")] CreateAccountUserViewModel viewModel)
        {
            // パスワードは自動生成する
            // TODO:以下のメソッドは記号以外の文字種を全て織り交ぜない場合があるのでイマイチ。
            // パスワードポリシーによっては自作すべきかもしれない。
            viewModel.RawPassword = Membership.GeneratePassword(8, 1);

            AccountUser accountUser = new AccountUser() { UserName = viewModel.UserName, DisplayName = viewModel.DisplayName, Organization = viewModel.Organization };

            accountUser.Password = Models.User.GenerateHashedPassword(viewModel.RawPassword);
            accountUser.UserType = 1; // TODO: 不要であれば削除

            // 企業エンティティを作成し、関連付ける
            Company company = new Company() { CompanyName = viewModel.CompanyName };
            accountUser.Company = company;

            // TODO:現在、UserNameやCompanyNameの重複チェックは行っていない。
            try
            {
                db.Companies.Add(company);
                db.Users.Add(accountUser);
                db.SaveChanges();

                ViewBag.CompanyId = company.ID;
                return View("CreateCompleted", viewModel);
            }
            catch
            {
                return View();
            }
        }

        // GET: AdministratorHome/Edit/5
        public ActionResult Edit(int? companyId)
        {
            return RedirectToAction("Index");
            //return View();
        }

        // POST: AdministratorHome/Edit/5
        [HttpPost]
        public ActionResult Edit(int? companyId, FormCollection collection)
        {
            try
            {
                // TODO:未実装
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: AdministratorHome/Delete/5
        public ActionResult Delete(int? companyId)
        {
            // TODO:未実装
            return RedirectToAction("Index");
            //return View();
        }

        // POST: AdministratorHome/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO:未実装
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // TODO: 業務共通ロジックに移動したい
        private bool IsAdministrator()
        {
            return ctx != null && ctx.CurrentUser.Role == "Administrator";
        }

    }
}
