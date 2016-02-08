using NihonUnisys.Olyzae.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace NihonUnisys.Olyzae.Controllers.Accounts
{
    public class CompanyProjectHomeController : AbstractCompanyProjectController
    {
        //
        // GET: /CompanyProjectHome/project/1/Index

        public ActionResult Index()
        {
            return View(this.Project);
        }

        // GET: /CompanyProjectHome/project/5/Edit
        public ActionResult Edit()
        {
            var project = this.Project;
            return View(project);
        }

        // POST: /CompanyProjectHome/project/5/Edit
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,Name,Description,ProjectDate,Category,Status,ProjectQuota,ProjectApply")] Project project)
        {
            if (ModelState.IsValid && this.Project.ID == project.ID)
            {
                // this.Projectがすでにattach済みのため、
                // projectのプロパティをthis.Projectのプロパティにコピーし直す。
                var db = this.DbContext;
                this.Project.Name = project.Name;
                this.Project.Description = project.Description;
                this.Project.ProjectDate = project.ProjectDate;
                this.Project.Category = project.Category;
                this.Project.Status = project.Status;
                this.Project.ProjectQuota = project.ProjectQuota;
                this.Project.ProjectApply = project.ProjectApply;
                this.Project.Icon = GetProjectIconFromRequest() ?? this.Project.Icon;
                // アイコンが送信された場合はDBを更新するが、
                // ASP.NET MVCの既定の設定ではブラウザキャッシュを許容しているので、
                // F5でリロードするまでは古いアイコンが表示されることに注意。
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(project);
        }

        /// <summary>
        /// ブラウザーを使用してアップロードされた画像があれば、
        /// Documentsテーブルに追加してIDを返します。
        /// </summary>
        /// <returns>
        /// 新しいDocumentオブジェクトのID。
        /// アップロードされた画像がなければnull。
        /// </returns>
        internal Guid? GetProjectIconFromRequest()
        {
            // 画像ファイルの扱いはWebImageクラスを使用する。
            // フォームには "uploadedFile" という名前で格納されているが、
            // アクションメソッドのパラメータにはバインドしない。
            var image = WebImage.GetImageFromRequest();
            if (image == null)
            {
                // 画像ファイルとして不適切
                return null;
            }
            var document = new Document()
            {
                ID = Guid.NewGuid(),
                BinaryData = image.GetBytes(),
                FileExtension = "." + image.ImageFormat,
                Uploaded = this.ExecutionContext.Now,
                User = this.ExecutionContext.CurrentUser
            };
            this.DbContext.Documents.Add(document);
            return document.ID;
        }
    }
}