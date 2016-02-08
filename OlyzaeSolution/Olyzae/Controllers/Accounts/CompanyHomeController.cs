using NihonUnisys.Olyzae.Framework;
using NihonUnisys.Olyzae.Models;
using System;
using System.Linq;
using System.Web.Helpers;
using System.Web.Mvc;

namespace NihonUnisys.Olyzae.Controllers.Accounts
{
    [Authorize(Roles = "AccountUser")]
    public class CompanyHomeController : Controller
    {
        private Entities db = new Entities();
        private ExecutionContext ctx = ExecutionContext.Create();

        public ActionResult Index()
        {
            // 企業ユーザーの場合は会社情報も取得されているので、それを使って絞り込む
            var au = ExecutionContext.Create().CurrentUser as AccountUser;
            return View(db.Projects.Where(x => x.Company.ID == au.Company.ID).ToList());
        }

        // GET: /CompanyHome/Create
        public ActionResult Create()
        {
            DateTime now = ctx.Now;

            // ProjectDate は30分単位の時間で初期化しておく
            Project project = new Project()
            {
                ProjectDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute / 30 * 30, 0),
                Category = ProjectCategory.Career
            };

            return View(project);
        }

        // POST: /CompanyHome/Create
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Name,Description,ProjectDate,Category,ProjectQuota,ProjectApply")] Project project)
        {
            if (ModelState.IsValid)
            {
                var user = ctx.CurrentUser as AccountUser;
                // 別コンテキストで取得した親オブジェクトは、最初にAttachしておく
                db.Companies.Attach(user.Company);

                project.Icon = GetProjectIconFromRequest();
                project.Duration = new Duration(); // TODO: Durationの見直し。
                project.Company = user.Company;
                db.Projects.Add(project);
                
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
                Uploaded = ctx.Now,
                User = ctx.CurrentUser
            };
            db.Documents.Add(document);
            return document.ID;
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
