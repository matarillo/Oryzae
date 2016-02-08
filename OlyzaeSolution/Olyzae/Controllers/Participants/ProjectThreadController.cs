using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using NihonUnisys.Olyzae.Models;
using System.IO;
using System.Net.Mime;

namespace NihonUnisys.Olyzae.Controllers.Participants
{
    /// <summary>
    /// 参加者のプロジェクトメニューから「お知らせ」を選択して遷移する画面のコントローラー。
    /// </summary>
    [Authorize(Roles = "ParticipantUser")]
    public class ProjectThreadController : AbstractParticipantProjectController
    {
        // GET: /ProjectThread/project/1
        public ActionResult Index(int? projectId)
        {
            // ユーザーとプロジェクトの取得は親クラスで実行ずみ。
            // 現在のプロジェクトの、自分宛てのお知らせスレッドをすべて取得する。
            //TODO 表示順序をどうするか(今はとりあえずID順)
            var threads = from t in this.DbContext.Threads.OfType<ProjectThread>()
                          from relation in this.DbContext.ParticipantUserThreads
                          where t.Project.ID == this.Project.ID
                          where relation.ParticipantUser.ID == this.CurrentUser.ID
                          where relation.Thread.ID == t.ID
                          orderby t.ID
                          select t;

            Dictionary<int, DateTime> lastPostedTimes = new Dictionary<int, DateTime>();
            Dictionary<int, string> sentUserNames = new Dictionary<int, string>();

            //TODO データアクセスの効率が良くない
            foreach (var thread in threads)
            {
                var lastMessage = from m in this.DbContext.Messages
                                  from u in this.DbContext.Users
                                  let inner = from m2 in this.DbContext.Messages
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
            ViewBag.SentUserNames = sentUserNames;

            ViewBag.ProjectId = this.Project.ID;
            ViewBag.ProjectName = this.Project.Name;
            return View(threads.ToList());
        }

        public ActionResult Download(int? threadId, int? messageId)
        {
            if (threadId == null || messageId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 指定したスレッドが自分宛てかどうかを確認する。
            var thread = this.DbContext.Threads.OfType<ProjectThread>()
                .Include(t => t.ReceivedUsers)
                .FirstOrDefault(t => t.ID == threadId);

            if (thread == null)
            {
                return HttpNotFound();
            }

            if (thread.ReceivedUsers.Where(rupt => rupt.ParticipantUser.ID == this.CurrentUser.ID) == null)
            {
                return HttpNotFound();
            }

            // IDに一致するメッセージを取得する。
            var message = this.DbContext.Messages
                .Include(m => m.Thread)
                .FirstOrDefault(m => m.ID == messageId && m.Thread.ID == thread.ID);

            if (message == null)
            {
                return HttpNotFound();
            }

            var document = this.DbContext.Documents.FirstOrDefault(d => d.ID == message.AttachedDocumentID);

            if (document == null)
            {
                // not found
                return RedirectToAction("Messages", new { threadId = threadId, fromReply = false });
            }

            return File(document.BinaryData, MediaTypeNames.Application.Octet, message.AttachedFileName);
        }

        // GET: /ProjectThread/project/1/Messages/5
        public ActionResult Messages(int? threadId, bool? fromReply, string body)
        {
            // ユーザーとプロジェクトの取得は親クラスで実行ずみ。
            if (threadId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 指定したスレッドが自分宛てかどうかを確認し、
            // そのスレッドのメッセージも含めて取得する。
            var thread = this.DbContext.Threads.OfType<ProjectThread>()
                .Include(t => t.ReceivedUsers)
                .Include(t => t.Message)
                .FirstOrDefault(t => t.ID == threadId);

            if (thread == null)
            {
                return HttpNotFound();
            }

            if (thread.ReceivedUsers.Where(rupt => rupt.ParticipantUser.ID == this.CurrentUser.ID) == null)
            {
                return HttpNotFound();
            }

            if (fromReply != null && fromReply.Value)
            {
                if (string.IsNullOrEmpty(body))
                {
                    ModelState.AddModelError("Body", "メッセージを入力してください。");
                }
            }

            // 最初の1件目のメッセージがお知らせの内容。
            Message information = thread.Message.OrderBy(m => m.ID).FirstOrDefault();
            ViewBag.Information = information;

            // 投稿日時の昇順にソートする。
            var messages = this.DbContext.Messages
                .Include(m => m.SentUser)
                .OrderBy(m => m.Sent)
                .Where(m => m.Thread.ID == threadId);

            Dictionary<int, string> userNames = new Dictionary<int, string>();

            foreach (var message in messages)
            {
                userNames.Add(message.ID, message.SentUser.DisplayName);
            }

            ViewBag.UserNames = userNames;
            ViewBag.ThreadId = threadId;
            ViewBag.ThreadName = thread.ThreadName;
            ViewBag.ProjectId = this.Project.ID;
            return View(messages);
        }

        public ActionResult Reply(
            int? threadId,
            [Bind(Include = "Body")]Message message,
            HttpPostedFileBase uploadedFile)
        {
            if (threadId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            message.Sent = this.ExecutionContext.Now;

            if (!ModelState.IsValid)
            {
                return RedirectToAction("Messages", new { threadId = threadId, fromReply = true, body = message.Body });
            }

            // スレッドIDのチェック
            if (this.DbContext.ParticipantUserThreads
                .Where(rupt => rupt.Thread.ID == threadId && rupt.ParticipantUser.ID == this.CurrentUser.ID).ToList().Count() == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            AttachFile(message, uploadedFile);

            message.SentUser = this.DbContext.Users.Find(this.CurrentUser.ID);
            message.Thread = this.DbContext.Threads.Find(threadId);

            this.DbContext.Users.Attach(message.SentUser);
            this.DbContext.Threads.Attach(message.Thread);
            this.DbContext.Messages.Add(message);

            this.DbContext.SaveChanges();

            return RedirectToAction("Messages", new { threadId = threadId });
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
                    User = this.CurrentUser,
                    FileExtension = extension,
                    BinaryData = binaryData,
                };
                this.DbContext.Documents.Add(document);
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
