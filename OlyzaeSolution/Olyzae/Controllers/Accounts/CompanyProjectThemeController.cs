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
    public class CompanyProjectThemeController : AbstractCompanyProjectController
    {
        private Entities db = new Entities();

        // GET: /CompanyProjectTheme/project/5
        public ActionResult Index()
        {
            ViewBag.Project = this.Project;
            return View(db.Themes.Where(theme => theme.Project.ID == this.Project.ID).ToList());
        }

        // GET: /CompanyProjectTheme/Details/5
        public ActionResult Details(int? themeId)
        {
            ViewBag.Project = this.Project;
            
            if (themeId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Theme theme = db.Themes.Find(themeId);
            if (theme == null)
            {
                return HttpNotFound();
            }
            return View(theme);
        }

        // GET: /CompanyProjectTheme/Create
        public ActionResult Create()
        {
            ViewBag.Project = this.Project;
            return View();
        }

        // POST: /CompanyProjectTheme/Create
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,Name,Description,EvaluationJSON")] Theme theme)
        {
            ViewBag.Project = this.Project;
            
            if (ModelState.IsValid)
            {
                // このコントローラのdb以外で取得したprojectを
                // コード中で使うと、新しいプロジェクトとして扱われてしまうので
                // それを防ぐためにattachしておく
                db.Projects.Attach(Project);

                // projectはattach済みなので挿入されない予定
                theme.Project = Project;

                // durationは新規作成したので挿入される予定
                theme.Duration = new Models.Duration();

                // themeも（ブラウザからの送信データをもとに）新規作成したので挿入される予定
                db.Themes.Add(theme);

                // EntryStateは想定した状態になっているはずなので、このままsaveする
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(theme);
        }

        // GET: /CompanyProjectTheme/Edit/5
        public ActionResult Edit(int? themeId)
        {
            if (themeId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            ViewBag.Project = this.Project;
            
            Theme theme = db.Themes.Find(themeId);
            if (theme == null)
            {
                return HttpNotFound();
            }
            return View(theme);
        }

        // POST: /CompanyProjectTheme/Edit/5
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,Name,Description,EvaluationJSON")] Theme theme)
        {
            ViewBag.Project = this.Project;
            if (ModelState.IsValid)
            {
                db.Entry(theme).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(theme);
        }

        // GET: /CompanyProjectTheme/Delete/5
        public ActionResult Delete(int? themeId)
        {
            ViewBag.Project = this.Project;
            
            if (themeId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Theme theme = db.Themes.Find(themeId);
            if (theme == null)
            {
                return HttpNotFound();
            }
            return View(theme);
        }

        // POST: /CompanyProjectTheme/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int themeId)
        {
            ViewBag.Project = this.Project;
            
            Theme theme = db.Themes.Find(themeId);
            db.Themes.Remove(theme);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        /// <summary>
        /// プロジェクトと課題テーマのIDを指定して、課題テーマを取得します。
        /// </summary>
        /// <param name="db">DBコンテキスト。</param>
        /// <param name="project">現在のプロジェクト。</param>
        /// <param name="id">課題テーマのID。</param>
        /// <returns>
        /// 現在のプロジェクトに関連づいた課題テーマのうち、指定したIDを持つものが見つかれば、そのインスタンス。
        /// 見つからない場合はnull。
        /// </returns>
        internal static Theme GetThemeById(Entities db, Project project, int? id)
        {
            System.Diagnostics.Debug.Assert(db != null, "db is null");
            System.Diagnostics.Debug.Assert(project != null, "project is null");
            if (id == null)
            {
                return null;
            }
            var theme = db.Themes.FirstOrDefault(x => x.ID == id && x.Project.ID == project.ID);
            return theme;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private void ValidateModelInput(Theme theme)
        {
            if (string.IsNullOrEmpty(theme.Name))
            {
                ModelState.AddModelError("Name", "Nameを入力してください。");
            }

            if (string.IsNullOrEmpty(theme.Description))
            {
                ModelState.AddModelError("Description", "Descriptionを入力してください。");
            }
        }
    }
}
