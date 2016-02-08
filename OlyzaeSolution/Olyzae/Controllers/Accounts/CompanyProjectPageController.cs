using NihonUnisys.Olyzae.Framework;
using NihonUnisys.Olyzae.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace NihonUnisys.Olyzae.Controllers.Accounts
{
    public class CompanyProjectPageController : AbstractCompanyProjectController
    {
        private Entities db = new Entities();
        private ExecutionContext ctx = ExecutionContext.Create();

        //
        // GET: /CompanyProjectPage/project/1
        public ActionResult Index()
        {
            // 現在のプロジェクトと一緒に、実行ユーザーと所属企業もAttachされる。
            db.Configuration.ProxyCreationEnabled = false;
            db.Projects.Attach(this.Project);

            db.Entry(this.Project)
                .Reference(x => x.ProjectPage)
                .Query()
                .Include(x => x.PageContents)
                .Load();

            var projectPage = this.Project.ProjectPage ?? new ProjectPage();
            var pageBody = projectPage.PageBody ?? new Models.ProjectPages.PageBody();

            var images = (this.Project.ProjectPage == null)
                ? new List<SelectListItem>()
                : this.Project.ProjectPage.PageContents
                    .Select(x => x.ID.ToString())
                    .Select(x => new SelectListItem { Text = x, Value = x })
                    .ToList();
            images.Insert(0, new SelectListItem { Text = "なし", Value = "0" });
            var bgValue = pageBody.backgroundImage.ToString();
            var selected = images.FirstOrDefault(x => x.Value == bgValue) ?? images[0];
            selected.Selected = true;

            ViewBag.ProjectPage = projectPage;
            ViewBag.PageBody = pageBody;
            ViewBag.Images = images;

            return View(this.Project);
        }

        public ActionResult ShowImage(int? id, bool? thumbnail)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 現在のプロジェクトと一緒に、実行ユーザーと所属企業もAttachされる。
            db.Configuration.ProxyCreationEnabled = false;
            db.Projects.Attach(this.Project);

            var content = db.Entry(this.Project)
                .Reference(x => x.ProjectPage)
                .Query()
                .SelectMany(x => x.PageContents)
                .FirstOrDefault(x => x.ID == id.Value);
            if (content == null)
            {
                return HttpNotFound();
            }

            var document = db.Documents.FirstOrDefault(x => x.ID == content.DocumentID);
            if (document == null)
            {
                return HttpNotFound();
            }

            var data = (thumbnail == true)
                ? (new WebImage(document.BinaryData)).Resize(100, 100).GetBytes()
                : document.BinaryData;
            return this.File(data, document.FileExtension, document.Uploaded, document.ID);
        }

        public ActionResult AddImage()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddImage(HttpPostedFileBase uploadedFile)
        {
            // uploadedFileパラメータは、Get用のアクションメソッドと区別するためのダミーとする。
            // 画像ファイルの扱いはWebImageクラスを使用する。
            var image = WebImage.GetImageFromRequest();
            if (image == null)
            {
                // 画像ファイルとして不適切
                return RedirectToAction("Index");
            }

            // TODO: トランザクション

            // 現在のプロジェクトと一緒に、実行ユーザーと所属企業もAttachされる。
            db.Configuration.ProxyCreationEnabled = false;
            db.Projects.Attach(this.Project);

            db.Entry(this.Project).Reference(x => x.ProjectPage).Query().Include(x => x.PageContents).Load();
            var projectPage = this.Project.ProjectPage;
            if (projectPage == null)
            {
                projectPage = new ProjectPage
                {
                    Created = ctx.Now
                };
                this.Project.ProjectPage = projectPage;
            }

            var document = new Document()
            {
                ID = Guid.NewGuid(),
                BinaryData = image.GetBytes(),
                FileExtension = "." + image.ImageFormat,
                Uploaded = ctx.Now,
                User = ctx.CurrentUser
            };
            db.Documents.Add(document);

            var content = new PageContent
            {
                ProjectPage = projectPage,
                DocumentID = document.ID
            };
            projectPage.PageContents.Add(content);

            db.SaveChanges();

            return RedirectToAction("Index");
        }

        // TODO: POSTのみ対応にするかどうか
        public ActionResult DeleteImage(int? contentId)
        {
            if (contentId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 現在のプロジェクトと一緒に、実行ユーザーと所属企業もAttachされる。
            db.Configuration.ProxyCreationEnabled = false;
            db.Projects.Attach(this.Project);

            db.Entry(this.Project)
                .Reference(x => x.ProjectPage)
                .Query()
                .Include(x => x.PageContents)
                .Load();
            if (this.Project.ProjectPage != null)
            {
                var content = this.Project.ProjectPage.PageContents.FirstOrDefault(x => x.ID == contentId.Value);
                if (content != null)
                {
                    this.Project.ProjectPage.PageContents.Remove(content);
                    db.PageContents.Remove(content);
                    db.SaveChanges();
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditLayout(int? backgroundImage)
        {
            if (backgroundImage == null)
            {
                return RedirectToAction("Index");
            }

            // TODO: トランザクション

            // 現在のプロジェクトと一緒に、実行ユーザーと所属企業もAttachされる。
            db.Configuration.ProxyCreationEnabled = false;
            db.Projects.Attach(this.Project);

            db.Entry(this.Project).Reference(x => x.ProjectPage).Query().Include(x => x.PageContents).Load();
            var projectPage = this.Project.ProjectPage;
            if (projectPage == null || !projectPage.PageContents.Any(x => x.ID == backgroundImage))
            {
                return RedirectToAction("Index");
            }

            var pageBody = projectPage.PageBody ?? new Models.ProjectPages.PageBody();
            pageBody.backgroundImage = backgroundImage.Value;
            projectPage.UpdatePageBody(pageBody);

            db.SaveChanges();

            return RedirectToAction("Index");
        }

        public ActionResult AddSection()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddSection([Bind(Include = "title")] Models.ProjectPages.Section section)
        {
            if (section == null)
            {
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(section.title))
            {
                return RedirectToAction("Index");
            }

            // TODO: トランザクション

            // 現在のプロジェクトと一緒に、実行ユーザーと所属企業もAttachされる。
            db.Configuration.ProxyCreationEnabled = false;
            db.Projects.Attach(this.Project);

            // PageContentsは不要のため、ProjectPageだけ読み取る。
            db.Entry(this.Project).Reference(x => x.ProjectPage).Load();
            var projectPage = this.Project.ProjectPage;
            if (projectPage == null)
            {
                projectPage = new ProjectPage
                {
                    Created = ctx.Now
                };
                this.Project.ProjectPage = projectPage;
            }

            var pageBody = projectPage.PageBody ?? new Models.ProjectPages.PageBody();
            if (pageBody.sections == null)
            {
                pageBody.sections = new List<Models.ProjectPages.Section>();
            }
            pageBody.sections.Add(section);
            projectPage.UpdatePageBody(pageBody);

            db.SaveChanges();

            return RedirectToAction("Index");
        }

        public ActionResult EditSection(int? sectionId)
        {
            if (sectionId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 現在のプロジェクトと一緒に、実行ユーザーと所属企業もAttachされる。
            db.Configuration.ProxyCreationEnabled = false;
            db.Projects.Attach(this.Project);

            db.Entry(this.Project)
                .Reference(x => x.ProjectPage)
                .Query()
                .Include(x => x.PageContents)
                .Load();

            var projectPage = this.Project.ProjectPage;
            if (projectPage == null)
            {
                return RedirectToAction("Index");
            }
            var pageBody = projectPage.PageBody;
            if (pageBody == null)
            {
                return RedirectToAction("Index");
            }
            var sections = pageBody.sections;
            if (sections == null)
            {
                return RedirectToAction("Index");
            }
            if (sectionId.Value < 0 || sections.Count <= sectionId.Value)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var section = sections[sectionId.Value];
            ViewBag.JSON = section.ToString();
            ViewBag.ProjectPage = projectPage;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditSection(int? sectionId, string json)
        {
            if (sectionId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (string.IsNullOrEmpty(json))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var section = default(Models.ProjectPages.Section);
            try
            {
                section = Models.ProjectPages.Section.FromJsonString(json);
            }
            catch
            {
                // TODO: 不正な入力をデバッグログなどに出力
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 現在のプロジェクトと一緒に、実行ユーザーと所属企業もAttachされる。
            db.Configuration.ProxyCreationEnabled = false;
            db.Projects.Attach(this.Project);

            db.Entry(this.Project)
                .Reference(x => x.ProjectPage)
                .Load();

            var projectPage = this.Project.ProjectPage;
            if (projectPage == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var pageBody = projectPage.PageBody;
            if (pageBody == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var sections = pageBody.sections;
            if (sections == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (sectionId.Value < 0 || sections.Count <= sectionId.Value)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            sections[sectionId.Value] = section;

            projectPage.UpdatePageBody(pageBody);

            db.SaveChanges();

            return RedirectToAction("Index");
        }

        public ActionResult DeleteSection(int? sectionId)
        {
            if (sectionId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 現在のプロジェクトと一緒に、実行ユーザーと所属企業もAttachされる。
            db.Configuration.ProxyCreationEnabled = false;
            db.Projects.Attach(this.Project);

            db.Entry(this.Project)
                .Reference(x => x.ProjectPage)
                .Load();

            var projectPage = this.Project.ProjectPage;
            if (projectPage == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var pageBody = projectPage.PageBody;
            if (pageBody == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var sections = pageBody.sections;
            if (sections == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (sectionId.Value < 0 || sections.Count <= sectionId.Value)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            sections.RemoveAt(sectionId.Value);

            projectPage.UpdatePageBody(pageBody);

            db.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}