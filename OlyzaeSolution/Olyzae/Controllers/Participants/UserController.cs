using NihonUnisys.Olyzae.Framework;
using NihonUnisys.Olyzae.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace NihonUnisys.Olyzae.Controllers.Participants
{
    [Authorize(Roles = "ParticipantUser")]
    public class UserController : Controller
    {
        private Entities db;
        private ExecutionContext ctx;
        private Business.Thread threadBusiness;

        public UserController()
        {
            this.db = new Entities();
            db.Configuration.ProxyCreationEnabled = false;
            this.ctx = ExecutionContext.Create();
            this.threadBusiness = new Business.Thread(db, ctx);
        }

        // GET: /User/Index/5
        public ActionResult Index(int? id, int? threadId)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // id がログインユーザのものであれば /Home に移動
            if (id == ctx.CurrentUser.ID)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = db.Users.OfType<ParticipantUser>().FirstOrDefault(x => x.ID == id);
            if (user == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // プロトではマイスレッドの先頭から50件を取得する。
            const int pageSize = 50;
            var currentUser = ctx.CurrentUser as ParticipantUser;
            var threads = threadBusiness.GetPersonalThreads(currentUser, id.Value, 0, pageSize, true);
            foreach (var thread in threads)
            {
                thread.Message = thread.Message.OrderBy(m => m.Sent).ToList();
            }
            ViewBag.Threads = threads;
            ViewBag.ThreadId = threadId;

            return View(user);
        }

        // GET: /User/ProfileImage/5?thumbnail=True
        public ActionResult ProfileImage(int? id, bool? thumbnail)
        {
            if(id==null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var documentId = db.Users.OfType<ParticipantUser>().FirstOrDefault(x => x.ID == id).ProfileImageDocumentID;
            if (documentId == null)
            {
                return HttpNotFound();
            }

            var document = db.Documents.FirstOrDefault(x => x.ID == documentId);
            if (document == null)
            {
                return HttpNotFound();
            }

            // TODO:メディアクエリを考慮したリサイズ？
            var data = (thumbnail == true)
                ? (new WebImage(document.BinaryData)).Resize(126, 126).GetBytes()
                : document.BinaryData;

            return this.File(data, document.FileExtension, document.Uploaded, document.ID);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reply(int? userId, int? threadId, string body, HttpPostedFileBase uploadedFile)
        {
            if (userId == null || threadId == null || string.IsNullOrEmpty(body))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var currentUser = ctx.CurrentUser as ParticipantUser;
            db.Users.Attach(currentUser);
            var thread = threadBusiness.GetPersonalThread(currentUser, userId.Value, threadId.Value, false);
            if (thread != null)
            {
                threadBusiness.AddMessage(thread, body, uploadedFile);
                db.SaveChanges();
            }

            return RedirectToIndex(userId.Value);
        }

        public ActionResult Download(int? userId, int? threadId, int? messageId)
        {
            if (userId == null || threadId == null || messageId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var currentUser = ctx.CurrentUser as ParticipantUser;
            var thread = threadBusiness.GetPersonalThread(currentUser, userId.Value, threadId.Value, false);
            if (thread == null)
            {
                return RedirectToIndex(userId.Value);
            }
            var tuple = threadBusiness.GetAttachment(thread, messageId.Value);
            var message = tuple.Item1;
            var document = tuple.Item2;

            if (message == null || document == null)
            {
                // not found
                return RedirectToIndex(userId.Value);
            }

            return File(document.BinaryData, MediaTypeNames.Application.Octet, message.AttachedFileName);
        }

        internal ActionResult RedirectToIndex(int userId)
        {
            if (userId == ctx.CurrentUser.ID)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return RedirectToAction("Index", new { id = userId });
            }
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
