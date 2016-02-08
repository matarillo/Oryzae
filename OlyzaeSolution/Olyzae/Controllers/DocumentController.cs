using NihonUnisys.Olyzae.Models;
using NihonUnisys.Olyzae.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace NihonUnisys.Olyzae.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private Entities db = new Entities();

        [AllowAnonymous]
        public ActionResult Index()
        {
            // デバッグ用
            // TODO: 後で消すこと

            var docs = db.Documents.ToList();
            return View(docs);
        }

        //
        // GET: /Document/Get/45C26F59-7A67-434F-AD78-DABEEBB803B7.png
        [AllowAnonymous]
        public ActionResult Get(string id)
        {
            // デバッグ用
            // この実装ではGUIDを知っていれば誰でもダウンロード可能
            // TODO: 後で消すこと

            Guid guid;
            if (!Guid.TryParse(id, out guid))
            {
                return HttpNotFound();
            }

            var document = db.Documents.FirstOrDefault(x => x.ID == guid);
            if (document == null)
            {
                return HttpNotFound();
            }

            return this.File(document);
        }

        [AllowAnonymous]
        public ActionResult ProjectIcon(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Project project = db.Projects.Find(id);
            if (project == null || project.Icon == null)
            {
                return HttpNotFound();
            }

            var document = db.Documents.FirstOrDefault(x => x.ID == project.Icon.Value);
            if (document == null)
            {
                return HttpNotFound();
            }

            return this.File(document);
        }

        [AllowAnonymous]
        public ActionResult ProfileImage(int? id, int? width, int? height)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ParticipantUser user = db.Users.OfType<ParticipantUser>().FirstOrDefault(x => x.ID == id);
            if (user == null || user.ProfileImageDocumentID == null)
            {
                return HttpNotFound();
            }

            var document = db.Documents.FirstOrDefault(x => x.ID == user.ProfileImageDocumentID);
            if (document == null)
            {
                return HttpNotFound();
            }

            var data = (width.HasValue && height.HasValue)
                ? (new WebImage(document.BinaryData)).Resize(width.Value, height.Value).GetBytes()
                : document.BinaryData;

            return this.File(data, document.FileExtension, document.Uploaded, document.ID);
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