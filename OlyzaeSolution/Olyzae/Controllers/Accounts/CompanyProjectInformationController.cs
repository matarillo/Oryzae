using NihonUnisys.Olyzae.Framework;
using NihonUnisys.Olyzae.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Web;
using System.Web.Mvc;

namespace NihonUnisys.Olyzae.Controllers.Accounts
{
    public class CompanyProjectInformationController : AbstractCompanyProjectController
    {
        private Entities db = new Entities();
        private ExecutionContext ctx = ExecutionContext.Create();

        // GET: /CompanyProjectInformation/
        public ActionResult Index()
        {
            IQueryable<ProjectThread> projectThreads = db.Threads.OfType<ProjectThread>().Where(pt => pt.Project.ID == this.Project.ID);
            ViewBag.Project = this.Project;

            Dictionary<int, DateTime> lastPostedTimes = new Dictionary<int, DateTime>();
            Dictionary<int, string> sentUserNames = new Dictionary<int, string>();

            //TODO データ取得の効率が悪い
            foreach (var thread in projectThreads)
            {
                var lastMessage = from m in db.Messages
                                  from u in db.Users
                                  let inner = from m2 in db.Messages
                                              where m2.Thread.ID == thread.ID
                                              select m2.Sent
                                  where m.SentUser.ID == u.ID
                                  where m.Sent == inner.Max()
                                  select new { m.Sent, u.DisplayName };

                if (lastMessage.Count() > 0)
                {
                    lastPostedTimes.Add(thread.ID, lastMessage.First().Sent.Value);
                    sentUserNames.Add(thread.ID, lastMessage.First().DisplayName);
                }
            }

            ViewBag.LastPostedTimes = lastPostedTimes;
            ViewBag.LastPostedUsers = sentUserNames;

            return View(projectThreads.ToList());
        }

        // GET: /CompanyProjectInformation/Details/5
        public ActionResult Details(int? projectThreadId, bool? isError, string body)
        {
            if (projectThreadId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (isError.HasValue && isError.Value)
            {
                if (string.IsNullOrEmpty(body))
                {
                    ModelState.AddModelError("Body", "本文を入力してください。");
                }
            }

            ProjectThread projectthread = db.Threads.OfType<ProjectThread>().First(pt => pt.ID == projectThreadId);

            if (projectthread == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            IQueryable<Message> messages = db.Messages.Where(m => m.Thread.ID == projectThreadId).OrderBy(m => m.ID);

            if (messages == null || messages.Count() == 0)
            {
                //TODO 配信リスト作成時に必ずメッセージは1件作成されるので0件はシステムエラー
                return HttpNotFound();
            }

            Dictionary<int, string> receivedUserNames = new Dictionary<int, string>();

            IQueryable<ParticipantUserThread> receivedUsers = db.ParticipantUserThreads.Where(rupt => rupt.Thread.ID == projectThreadId);

            if (receivedUsers == null || receivedUsers.Count() == 0)
            {
                //TODO 配信リスト作成時にユーザーを必ず指定するので0件はシステムエラー
                return HttpNotFound();
            }

            foreach (ParticipantUserThread receivedUser in receivedUsers)
            {
                receivedUserNames.Add(receivedUser.ParticipantUser.ID, receivedUser.ParticipantUser.DisplayName);
            }

            ViewBag.ReceivedUserNames = receivedUserNames;

            Dictionary<int, string> sentUserNames = new Dictionary<int, string>();

            foreach (Message message in messages)
            {
                sentUserNames.Add(message.ID, message.SentUser.DisplayName);
            }

            ViewBag.SentUserNames = sentUserNames;

            Message information = messages.First();
            ViewBag.Information = information;
            ViewBag.ResponseMessages = messages.Where(m => m.ID != information.ID).ToList();

            ViewBag.Project = this.Project;
            return View(projectthread);
        }

        public ActionResult PostMessage(int? projectThreadId, [Bind(Include = "Body")]Message message, HttpPostedFileBase uploadedFile)
        {
            if (projectThreadId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Body = message.Body;
                return RedirectToAction("Details", new { projectThreadId = projectThreadId, isError = true, body = message.Body });
            }

            AttachFile(message, uploadedFile);

            message.SentUser = db.Users.Find(ctx.CurrentUser.ID);
            message.Thread = db.Threads.Find(projectThreadId);
            message.Sent = ctx.Now;
            db.Messages.Add(message);
            db.SaveChanges();
            return RedirectToAction("Details", new { projectThreadId = projectThreadId });
        }

        // GET: /CompanyProjectInformation/Create
        public ActionResult Create()
        {
            ViewBag.Project = this.Project;

            IQueryable<ProjectGroup> groups = db.Groups.OfType<ProjectGroup>().Where(pg => pg.Project.ID == this.Project.ID);
            ViewBag.ProjectGroups = groups;

            ViewBag.ProjectUsers = db.ParticipantUserProjects.Where(pu => pu.Project.ID == this.Project.ID);

            return View();
        }

        // POST: /CompanyProjectInformation/Create
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            [Bind(Include = "ID,ThreadName")] ProjectThread projectthread,
            [Bind(Include = "From, To")]Duration duration,
            [Bind(Include = "Body")]Message message,
            int? selectAddingUserType,
            string selectedUserIds,
            HttpPostedFileBase uploadedFile)
        {
            if (selectAddingUserType == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            bool userSelected = SetReceivedUser(projectthread, selectAddingUserType, selectedUserIds);

            if (!userSelected)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (!ModelState.IsValid)
            {
                return View(projectthread);
            }

            // dbフィールドは親クラスのデータベース接続と異なるインスタンスなので、
            // 関係するオブジェクトはAttachしておく必要がある。
            // TODO: あとでdbフィールドを整理
            var currentUser = ctx.CurrentUser as AccountUser;
            db.Users.Attach(currentUser);
            db.Companies.Attach(currentUser.Company);
            db.Projects.Attach(this.Project);

            AttachFile(message, uploadedFile);

            message.Sent = ctx.Now;

            ViewBag.Project = this.Project;

            IQueryable<ProjectGroup> groups = db.Groups.OfType<ProjectGroup>().Where(pg => pg.Project.ID == this.Project.ID);
            ViewBag.ProjectGroups = groups;

            ViewBag.ProjectUsers = db.ParticipantUserProjects.Where(pu => pu.Project.ID == this.Project.ID);

            projectthread.Duration = duration;
            message.SentUser = db.Users.Find(ctx.CurrentUser.ID);
            projectthread.Message.Add(message);
            projectthread.Project = this.Project;

            db.Threads.Add(projectthread);
            db.SaveChanges();


            // 宛先の参加者のタイムラインを更新する
            // TODO: 非同期処理の検討
            var summary = message.BodySummary;
            var participantsTimeLines = projectthread.ReceivedUsers
                .Select(put => put.ParticipantUser)
                .Select(pu => new Timeline
                {
                    OwnerID = pu.ID,
                    Timestamp = ctx.Now,
                    Type = TimelineType.ProjectThread,
                    SourceName = "「" + projectthread.Project.Name + "」（" + projectthread.Project.Company.CompanyName + "）",
                    Summary = summary,
                    ActionName = "Messages",
                    ControllerName = "ProjectThread",
                    RouteValuesJSON = Timeline.ToJSON(new { projectId = this.Project.ID, threadId = projectthread.ID }),
                });
            using (var publisher = new Business.TimelinePublisher())
            {
                publisher.Publish(participantsTimeLines);
            }

            return RedirectToAction("Index");
        }

        public ActionResult Download(int? threadId, int? messageId)
        {
            if (threadId == null || messageId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 指定されたメッセージが現在のプロジェクトの配信リストであればダウンロード可能。
            var messageQuery =
                from projectInfo in db.Threads.OfType<ProjectThread>()
                where projectInfo.ID == threadId
                from project in db.Projects
                where project.ID == this.Project.ID
                from msg in db.Messages
                where msg.ID == messageId
                select msg;

            var message = messageQuery.FirstOrDefault();

            if (message == null)
            {
                // not found
                return HttpNotFound();
            }

            var document = db.Documents.FirstOrDefault(d => d.ID == message.AttachedDocumentID);

            if (document == null)
            {
                // not found
                return HttpNotFound();
            }

            return File(document.BinaryData, MediaTypeNames.Application.Octet, message.AttachedFileName);
        }

        private bool SetReceivedUser(ProjectThread projectthread, int? selectAddingUserType, string selectedUserIds)
        {
            switch (selectAddingUserType.Value)
            {
                case 1:

                    db.Projects.Attach(this.Project);
                    var users = db.Entry(this.Project).Collection(p => p.ParticipantUsers).Query();
                    users.Load();

                    foreach (var user in users)
                    {
                        projectthread.ReceivedUsers.Add(new ParticipantUserThread { ParticipantUser = user.ParticipantUser });
                    }

                    return true;

                case 2:

                    if (selectedUserIds == null)
                    {
                        ModelState.AddModelError("ProjectUsers", "配信先のユーザーが選択されていません。");
                    }

                    List<string> userIdList = selectedUserIds.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    if (userIdList.Count() == 0)
                    {
                        ModelState.AddModelError("ProjectUsers", "配信先のユーザーが選択されていません。");
                    }

                    foreach (var user in userIdList)
                    {
                        int userId;

                        if (!int.TryParse(user, out userId))
                        {
                            return false;
                        }

                        ParticipantUser receivedUser = db.Users.OfType<ParticipantUser>().FirstOrDefault(pu => pu.ID == userId);

                        if (receivedUser == null)
                        {
                            return false;
                        }

                        projectthread.ReceivedUsers.Add(new ParticipantUserThread { ParticipantUser = receivedUser });
                    }

                    return true;

                default:
                    return false;
            }
        }

        [HttpGet]
        public ActionResult GetGroupUsers(int? groupId)
        {
            List<int> userIds = new List<int>();

            if (groupId == null)
            {
                return Json(userIds, JsonRequestBehavior.AllowGet);
            }

            Group group = db.Groups.Find(groupId);

            if (group == null)
            {
                return Json(userIds, JsonRequestBehavior.AllowGet);
            }

            var users = db.ParticipantUserGroups.Where(pug => pug.Group.ID == groupId);

            if (users == null || users.Count() == 0)
            {
                return Json(userIds, JsonRequestBehavior.AllowGet);
            }

            foreach (var user in users)
            {
                userIds.Add(user.ParticipantUser.ID);
            }

            return Json(userIds, JsonRequestBehavior.AllowGet);
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

    }
}
