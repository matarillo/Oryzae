using NihonUnisys.Olyzae.Framework;
using NihonUnisys.Olyzae.Models;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Web;
using System.Web.Mvc;

namespace NihonUnisys.Olyzae.Controllers.Accounts
{
    public class CompanyProjectThreadController : AbstractCompanyProjectController
    {
        private Entities db = new Entities();
        private ExecutionContext ctx = ExecutionContext.Create();

        //
        // GET: /CompanyProjectThread/project/1/Index

        public ActionResult Index()
        {
            db.Configuration.ProxyCreationEnabled = false;
            db.Configuration.LazyLoadingEnabled = false;

            db.Projects.Attach(this.Project);

            // TODO:
            // 効率のいい取得方法
            var threads =
                from p in db.Projects.Where(x => x.ID == this.Project.ID)
                from pg in p.ProjectGroups
                from gt in pg.GroupThreads
                select gt;
            var threadQuery = threads
                .Include(t => t.Group)
                .Include(t => t.Message)
                .Include(t => t.Message.Select(m => m.SentUser));

            bool hasProjectGroup = db.Groups.OfType<ProjectGroup>().Where(pg => pg.Project.ID == this.Project.ID).Count() > 0;
            ViewBag.HasProjectGroup = hasProjectGroup;

#if DEBUG
            // いずれ、デバッグログに出すように変更するかもしれない
            System.Diagnostics.Trace.WriteLine(threadQuery.ToString());
#endif

            threadQuery.Load();
            // これで、this.Project -> ProjectGroups -> Threads -> Message -> SentUser までたどることが可能になった（はず）

            return View(this.Project);
        }

        // GET: /CompanyProjectThread/project/1/Create
        public ActionResult Create()
        {
            return CreateView(null, null);
        }

        internal ActionResult CreateView(GroupThread thread, Message message)
        {
            db.Configuration.ProxyCreationEnabled = false;
            db.Configuration.LazyLoadingEnabled = false;

            // 現在のプロジェクトに属し、かつ、公開されているグループを取得する。
            var groups = db.Groups.OfType<ProjectGroup>().Where(x => x.Project.ID == this.Project.ID && x.Accessibility == ProjectGroupAccessibility.Public).ToList();

            var selectList = groups.Select(x => new SelectListItem { Text = x.GroupName, Value = x.ID.ToString() });
            ViewBag.GroupId = selectList;
            ViewBag.Message = message ?? new Message();
            ViewBag.GroupThread = thread ?? new Thread();
            ViewBag.Project = this.Project;
            return View(thread);
        }

        // POST: /CompanyProjectThread/project/1/Create
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            int? groupId,
            string threadName,
            string body,
            HttpPostedFileBase uploadedFile)
        {
            db.Configuration.ProxyCreationEnabled = false;
            db.Configuration.LazyLoadingEnabled = false;

            // TODO: トランザクション

            // 現在のプロジェクトと一緒に、実行ユーザーと所属企業もAttachされる。
            db.Projects.Attach(this.Project);

            if (groupId == null)
            {
                ModelState.AddModelError("GroupId", "グループを選択してください。");
            }

            // 正しいグループかどうかチェック
            var targetGroup = db.Entry(this.Project)
                .Collection(p => p.ProjectGroups)
                .Query()
                .Include(g => g.ParticipantUsers)
                .Include(g => g.ParticipantUsers.Select(u => u.ParticipantUser))
                .FirstOrDefault(g => g.ID == groupId);

            if (targetGroup == null)
            {
                ModelState.AddModelError("GroupId", "選択したグループは存在しません。");
            }
            else
            {
                if (targetGroup.Accessibility != ProjectGroupAccessibility.Public)
                {
                    ModelState.AddModelError("GroupId", "選択したグループは非公開です。");
                }

                if (targetGroup.ParticipantUsers.Count == 0)
                {
                    ModelState.AddModelError("GroupId", "選択したグループにはメンバーがいません。");
                }
            }

            if (string.IsNullOrEmpty(threadName))
            {
                ModelState.AddModelError("ThreadName", "スレッド名を入力してください。");
            }

            if (string.IsNullOrEmpty(body))
            {
                ModelState.AddModelError("Body", "本文を入力してください。");
            }

            GroupThread groupThread = new GroupThread { ThreadName = threadName };
            Message message = new Message { Body = body };

            if (!ModelState.IsValid)
            {
                return CreateView(groupThread, message);
            }

            AttachFile(message, uploadedFile);

            // Threadの追加
            // TODO: Durationの意味。返信可能な期限？
            db.Configuration.ProxyCreationEnabled = false;
            db.Configuration.LazyLoadingEnabled = false;

            groupThread.Group = targetGroup;
            groupThread.Duration = new Duration();
            db.Threads.Add(groupThread);

            // Messageの追加
            message.SentUser = ctx.CurrentUser;
            message.Sent = ctx.Now;
            groupThread.Message.Add(message);
            foreach (var groupuser in targetGroup.ParticipantUsers)
            {
                var relationship = new UserMessage { Message = message, User = groupuser.ParticipantUser };
                db.UserMessages.Add(relationship);
            }

            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Details(int? threadId)
        {
            if (threadId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var thread = db.Threads.OfType<GroupThread>().Where(gt => gt.ID == threadId)
                .Include(t => t.Group)
                .Include(t => t.Message).FirstOrDefault();

            if (thread == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ProjectGroup projectGroup = thread.Group as ProjectGroup;

            if (projectGroup != null && projectGroup.Accessibility != ProjectGroupAccessibility.Public)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (thread.Message == null || thread.Message.Count() == 0)
            {
                return View(this.Project);
            }

            ViewBag.Project = this.Project;
            ViewBag.Messages = thread.Message;
            return View(thread);
        }

        [HttpPost]
        public ActionResult PostMessage(
            int? threadId,
            [Bind(Include = "Subject,Body")]Message message,
            HttpPostedFileBase uploadedFile)
        {
            if (threadId == null || message == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Message = message;
                return View(this.Project);
            }

            GroupThread thread = db.Threads.OfType<GroupThread>()
                .Include(gt => gt.Group)
                .FirstOrDefault(gt => gt.ID == threadId);

            if (thread == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ProjectGroup projectGroup = thread.Group as ProjectGroup;

            if (projectGroup != null && projectGroup.Accessibility != ProjectGroupAccessibility.Public)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            message.SentUser = db.Users.Find(ctx.CurrentUser.ID);
            message.Sent = ctx.Now;
            message.Thread = thread;

            AttachFile(message, uploadedFile);

            db.Messages.Add(message);

            db.Threads.Attach(message.Thread);
            db.Users.Attach(message.SentUser);

            db.SaveChanges();

            return RedirectToAction("Details", new { threadId = threadId });
        }

        public ActionResult Download(int? groupId, int? threadId, int? messageId)
        {
            if (groupId == null || threadId == null || messageId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 指定されたメッセージが、自分が参加しているグループのスレッドであれば
            // ダウンロード可能。
            var messageQuery =
                from gt in db.Threads.OfType<GroupThread>()
                where gt.ID == threadId && gt.Group.ID == groupId
                from p in db.Projects
                where p.ID == this.Project.ID
                from m in gt.Message
                where m.ID == messageId
                select m;
            LogUtility.DebugWriteQuery(messageQuery);
            var message = messageQuery.FirstOrDefault();

            if (message == null)
            {
                // not found
                return RedirectToAction("Details", new { groupId = groupId, threadId = threadId });
            }

            var document = db.Documents.FirstOrDefault(d => d.ID == message.AttachedDocumentID);

            if (document == null)
            {
                // not found
                return RedirectToAction("Details", new { groupId = groupId, threadId = threadId });
            }

            return File(document.BinaryData, MediaTypeNames.Application.Octet, message.AttachedFileName);
        }

        internal void AttachFile(Message message, HttpPostedFileBase uploadedFile)
        {
            if (message == null || uploadedFile == null)
            {
                return;
            }

            // TODO: 見直し。プロトでは特に検証せずDBに保存する。
            var fileName = Path.GetFileName(uploadedFile.FileName);
            var extension = Path.GetExtension(fileName);
            var binaryData = GetBytes(uploadedFile.InputStream);

            if (binaryData.Length > 0)
            {
                var document = new Document
                {
                    ID = Guid.NewGuid(),
                    Uploaded = this.ExecutionContext.Now,
                    User = ctx.CurrentUser,
                    FileExtension = extension,
                    BinaryData = binaryData,
                };
                db.Documents.Add(document);
                message.AttachedFileName = fileName;
                message.AttachedDocumentID = document.ID;
            }
        }

        private static byte[] GetBytes(Stream stream)
        {
            if (stream == null)
            {
                return new byte[0];
            }
            var ms = stream as MemoryStream;
            if (ms == null)
            {
                ms = new MemoryStream();
                stream.CopyTo(ms);
            }
            return ms.ToArray();
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