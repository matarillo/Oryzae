using NihonUnisys.Olyzae.Framework;
using NihonUnisys.Olyzae.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Web;
using System.Web.Mvc;

namespace NihonUnisys.Olyzae.Controllers.Participants
{
    [Authorize(Roles = "ParticipantUser")]
    public class GroupThreadController : AbstractParticipantGroupController
    {
        // GET: /GroupThread/?groupId=1
        public ActionResult Index(int? groupId)
        {
            var group = this.Group;
            Debug.Assert(group != null, "this.Group is null");

            if (!IsAccessibleProject(group))
            {
                // 参加者がアクセスできない公開範囲である場合はエラー
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ViewBag.GroupName = group.GroupName;
            ViewBag.GroupId = groupId;

            Dictionary<int, DateTime> lastPostedTimes = new Dictionary<int, DateTime>();
            Dictionary<int, string> sentUserNames = new Dictionary<int, string>();

            var db = this.DbContext;
            var threads = db.Groups.Where(g => g.ID == groupId).SelectMany(g => g.GroupThreads).OrderBy(t => t.ID);

            //TODO データ取得の効率が悪い
            foreach (var thread in threads)
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
            ViewBag.SentUserNames = sentUserNames;

            //TODO スレッドの表示順序(現状はとりあえずIDの昇順)
            return View(threads.ToList());
        }

        // GET: /GroupThread/Messages?groupId=1&threadId=2
        public ActionResult Messages(int? groupId, int? threadId, bool? fromReply, string subject, string body)
        {
            if (groupId == null || threadId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (!IsAccessibleProject(this.Group))
            {
                // 参加者がアクセスできない公開範囲である場合はエラー
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (fromReply == true)
            {
                // Replyで入力エラーが発生したときにここに到達する。
                if (string.IsNullOrEmpty(subject))
                {
                    ModelState.AddModelError("Subject", "メッセージの件名を入力してください。");
                }

                if (string.IsNullOrEmpty(body))
                {
                    ModelState.AddModelError("Body", "メッセージの本文を入力してください。");
                }
            }

            if (threadId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var db = this.DbContext;
            GroupThread thread = db.Threads
                .OfType<GroupThread>()
                .Where(gt => gt.ID == threadId)
                .Include(gt => gt.Group)
                .FirstOrDefault();

            if (thread == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ViewBag.ThreadName = thread.ThreadName;
            ViewBag.GroupId = thread.Group.ID;
            ViewBag.ThreadId = threadId.Value;

            List<Message> messages = db.Messages
                .Where(message => message.Thread.ID == threadId.Value)
                .Include(message => message.SentUser)
                .OrderBy(message => message.Sent)
                .ToList();

            Dictionary<int, string> userNames = new Dictionary<int, string>();

            foreach (Message m in messages)
            {
                userNames.Add(m.ID, m.SentUser.DisplayName);
            }

            ViewBag.UserNames = userNames;

            return View(messages);
        }

        // GET: /GroupThread/Create
        public ActionResult Create(int? groupId)
        {
            // 基底クラスの共通処理により、Groupプロパティには必ず値が入っている。
            var group = this.Group;
            Debug.Assert(group != null, "this.Group is null");

            if (!IsAccessibleProject(group))
            {
                // 参加者がアクセスできない公開範囲である場合はエラー
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ViewBag.GroupId = group.ID;
            ViewBag.GroupName = group.GroupName;

            return View();
        }

        // POST: /GroupThread/Create
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int? groupId, string threadName, string body, HttpPostedFileBase uploadedFile)
        {
            // 基底クラスの共通処理により、Groupプロパティには必ず値が入っている。
            var group = this.Group;
            Debug.Assert(group != null, "this.Group is null");

            if (!IsAccessibleProject(group))
            {
                // 参加者がアクセスできない公開範囲である場合はエラー
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrEmpty(threadName))
            {
                ModelState.AddModelError("ThreadName", "スレッドタイトルを入力してください。");
            }

            if (string.IsNullOrEmpty(body))
            {
                ModelState.AddModelError("Body", "スレッドの説明を入力してください。");
            }

            GroupThread thread = new GroupThread { ThreadName = threadName };
            ViewBag.Body = body;

            if (!ModelState.IsValid)
            {
                ViewBag.GroupName = group.GroupName;
                ViewBag.GroupId = groupId;
                return View(thread);
            }

            var db = this.DbContext;
            Message message = new Message
            {
                Body = body,
                Sent = this.ExecutionContext.Now,
            };
            AttachFile(message, uploadedFile);
            message.SentUser = this.CurrentUser;
            db.Users.Attach(message.SentUser);

            thread.Message.Add(message);
            //TODO スレッドのDurationの扱いについて要検討
            thread.Duration = new Duration();
            thread.Group = db.Groups.Find(groupId.Value);

            db.Groups.Attach(thread.Group);
            db.Threads.Add(thread);

            db.SaveChanges();
            return RedirectToAction("Index", new { groupId = groupId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reply(int? groupId, int? threadId, string body, HttpPostedFileBase uploadedFile)
        {
            if (groupId == null || threadId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (!IsAccessibleProject(this.Group))
            {
                // 参加者がアクセスできない公開範囲である場合はエラー
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            var db = this.DbContext;
            Thread thread = db.Threads.Find(threadId);

            if (thread == null)
            {
                // IDに一致するスレッドが存在しない場合はエラー
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Message message = new Message
            {
                Body = body,
                Sent = this.ExecutionContext.Now,
            };

            if (string.IsNullOrEmpty(body))
            {
                ViewBag.Body = body;
                ViewBag.ThreadId = threadId;
                return RedirectToAction("Messages", new { groupId = groupId, threadId = threadId, fromReply = true, body = body });
            }

            // メッセージ投稿処理
#if DEBUG
            var currentUserState = this.DbContext.Entry(this.CurrentUser).State;
            Debug.Assert(currentUserState == System.Data.EntityState.Unchanged, "CurrentUser is " + currentUserState);
#endif

            AttachFile(message, uploadedFile);

            message.SentUser = this.CurrentUser;
            message.Thread = thread;

            db.Users.Attach(message.SentUser);
            db.Threads.Attach(message.Thread);
            db.Messages.Add(message);

            db.SaveChanges();

            return RedirectToAction("Messages", new { groupId = groupId, threadId = threadId });
        }

        public ActionResult Download(int? groupId, int? threadId, int? messageId)
        {
            if (groupId == null || threadId == null || messageId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (!IsAccessibleProject(this.Group))
            {
                // 参加者がアクセスできない公開範囲である場合はエラー
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            // 指定されたメッセージが、自分が参加しているグループのスレッドであれば
            // ダウンロード可能。
            var db = this.DbContext;
            var messageQuery =
                from gt in db.Threads.OfType<GroupThread>()
                where gt.ID == threadId && gt.Group.ID == groupId
                from pug in db.ParticipantUserGroups
                where pug.Group.ID == groupId && pug.ParticipantUser.ID == this.CurrentUser.ID
                from m in gt.Message
                where m.ID == messageId
                select m;
            LogUtility.DebugWriteQuery(messageQuery);
            var message = messageQuery.FirstOrDefault();
            if (message == null)
            {
                // not found
                return RedirectToAction("Messages", new { groupId = groupId, threadId = threadId });
            }

            var document = db.Documents.FirstOrDefault(d => d.ID == message.AttachedDocumentID);
            if (document == null)
            {
                // not found
                return RedirectToAction("Messages", new { groupId = groupId, threadId = threadId });
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

        private bool IsAccessibleProject(Group group)
        {
            ProjectGroup projectGroup = group as ProjectGroup;

            if (projectGroup == null)
            {
                return true;
            }

            if (projectGroup.Accessibility == ProjectGroupAccessibility.Public)
            {
                return true;
            }

            return false;
        }
    }
}
