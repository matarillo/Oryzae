using NihonUnisys.Olyzae.Framework;
using NihonUnisys.Olyzae.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;

namespace NihonUnisys.Olyzae.Business
{
    /// <summary>
    /// スレッドとメッセージを処理するクラス。
    /// </summary>
    public class Thread
    {
        private readonly Entities db;
        private readonly ExecutionContext ctx;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="db">データベースコンテキスト。</param>
        /// <param name="ctx">実行コンテキスト。</param>
        public Thread(Entities db, ExecutionContext ctx)
        {
            this.db = db;
            this.ctx = ctx;
        }

        public Models.GroupThread GetGroupThread(AccountUser currentUser, int groupId, int threadId, bool includesMessages)
        {
            return GetGroupThread(groupId, threadId, includesMessages,
                q => q
                    .OfType<ProjectGroup>()
                    .Where(pg => pg.Project.Company.ID == currentUser.Company.ID)
                    .OfType<Group>());
        }

        public Models.GroupThread GetGroupThread(ParticipantUser currentUser, int groupId, int threadId, bool includesMessages)
        {
            return GetGroupThread(groupId, threadId, includesMessages,
                q => q.Where(g => g.ParticipantUsers.Any(pug => pug.ParticipantUser.ID == currentUser.ID)));
        }

        internal Models.GroupThread GetGroupThread(int groupId, int threadId, bool includesMessages,
            Func<IQueryable<Group>, IQueryable<Group>> groupFilter)
        {
            if (groupId <= 0) throw new ArgumentOutOfRangeException("groupId");
            if (threadId <= 0) throw new ArgumentOutOfRangeException("threadId");

            var groupQuery = db.Groups
                .Where(pg => pg.ID == groupId);
            groupQuery = groupFilter(groupQuery);
            var threadQuery = groupQuery
                .SelectMany(g => g.GroupThreads)
                .Where(t => t.ID == threadId);
            threadQuery = IncludeMessages(threadQuery, includesMessages);

            LogUtility.DebugWriteQuery(threadQuery);
            var thread = threadQuery.FirstOrDefault();
            return thread;
        }

        public Models.ProjectThread GetProjectThread(AccountUser currentUser, int projectId, int threadId, bool includesMessages)
        {
            return GetProjectThread(projectId, threadId, includesMessages,
                q => q.Where(p => p.Company.ID == currentUser.Company.ID),
                q => q);
        }

        public Models.ProjectThread GetProjectThread(ParticipantUser currentUser, int projectId, int threadId, bool includesMessages)
        {
            return GetProjectThread(projectId, threadId, includesMessages,
                q => q.Where(p => p.ParticipantUsers.Any(pup => pup.ParticipantUser.ID == currentUser.ID)),
                q => q.Where(t => t.ReceivedUsers.Any(put => put.ParticipantUser.ID == currentUser.ID)));
        }

        internal Models.ProjectThread GetProjectThread(int projectId, int threadId, bool includesMessages,
            Func<IQueryable<Project>, IQueryable<Project>> projectFilter,
            Func<IQueryable<ProjectThread>, IQueryable<ProjectThread>> threadFilter)
        {
            if (projectId <= 0) throw new ArgumentOutOfRangeException("projectId");
            if (threadId <= 0) throw new ArgumentOutOfRangeException("threadId");

            var projectQuery = db.Projects
                .Where(p => p.ID == projectId);
            projectQuery = projectFilter(projectQuery);
            var threadQuery = projectQuery
                .SelectMany(p => p.ProjectThreads)
                .Where(t => t.ID == threadId);
            threadQuery = threadFilter(threadQuery);
            threadQuery = IncludeMessages(threadQuery, includesMessages);

            LogUtility.DebugWriteQuery(threadQuery);
            var thread = threadQuery.FirstOrDefault();
            return thread;
        }

        public Models.PersonalThread GetPersonalThread(AccountUser currentUser, int userId, int threadId, bool includesMessages)
        {
            return GetPersonalThread(userId, threadId, includesMessages,
                q => q.Where(u => u.Projects.Any(pup => pup.Project.Company.ID == currentUser.Company.ID)));
        }

        public Models.PersonalThread GetPersonalThread(ParticipantUser currentUser, int userId, int threadId, bool includesMessages)
        {
            if (userId == currentUser.ID)
            {
                return GetPersonalThread(userId, threadId, includesMessages, q => q);
            }
            else
            {
                var fg = new FriendGraph(db);
                return GetPersonalThread(userId, threadId, includesMessages, q => fg.Filter(q, currentUser.ID));
            }
        }

        internal Models.PersonalThread GetPersonalThread(int userId, int threadId, bool includesMessages,
            Func<IQueryable<ParticipantUser>, IQueryable<ParticipantUser>> userFilter)
        {
            if (userId <= 0) throw new ArgumentOutOfRangeException("userId");
            if (threadId <= 0) throw new ArgumentOutOfRangeException("threadId");

            var userQuery = db.Users
                .OfType<ParticipantUser>()
                .Where(u => u.ID == userId);
            userQuery = userFilter(userQuery);
            var threadQuery = userQuery
                .SelectMany(u => u.PersonalThreads)
                .Where(t => t.ID == threadId);
            threadQuery = IncludeMessages(threadQuery, includesMessages);

            LogUtility.DebugWriteQuery(threadQuery);
            var thread = threadQuery.FirstOrDefault();
            return thread;
        }

        public IList<Models.PersonalThread> GetPersonalThreads(ParticipantUser currentUser, int userId, int pageNumber, int pageSize, bool includesMessages)
        {
            if (userId == currentUser.ID)
            {
                return GetPersonalThreads(userId, pageNumber, pageSize, includesMessages, q => q);
            }
            else
            {
                var fg = new FriendGraph(db);
                return GetPersonalThreads(userId, pageNumber, pageSize, includesMessages, q => fg.Filter(q, currentUser.ID));
            }
        }

        internal IList<Models.PersonalThread> GetPersonalThreads(int userId, int pageNumber, int pageSize, bool includesMessages,
            Func<IQueryable<ParticipantUser>, IQueryable<ParticipantUser>> userFilter)
        {
            if (userId <= 0) throw new ArgumentOutOfRangeException("userId");

            var userQuery = db.Users
                .OfType<ParticipantUser>()
                .Where(u => u.ID == userId);
            userQuery = userFilter(userQuery);
            IQueryable<PersonalThread> threadQuery = userQuery
                .SelectMany(u => u.PersonalThreads)
                .OrderByDescending(t => t.Message.Min(m => m.Sent));
            threadQuery = IncludeMessages(threadQuery, includesMessages)
                .Skip(pageSize * pageNumber)
                .Take(pageSize);

            LogUtility.DebugWriteQuery(threadQuery);
            var threads = threadQuery.ToList();

            return threads;
        }

        internal protected IQueryable<T> IncludeMessages<T>(IQueryable<T> threadQuery, bool includesMessages) where T : Models.Thread
        {
            if (!includesMessages)
            {
                return threadQuery;
            }
            return threadQuery
                .Include(t => t.Message)
                .Include(t => t.Message.Select(m => m.SentUser));
        }

        /// <summary>
        /// データベースコンテキストに新しいメッセージを追加して返します。
        /// 保存処理は行いません。
        /// </summary>
        /// <param name="thread"></param>
        /// <param name="body"></param>
        /// <param name="uploadedFile"></param>
        /// <returns></returns>
        public Message AddMessage(Models.Thread thread, string body, HttpPostedFileBase uploadedFile)
        {
            Debug.Assert(thread != null, "thread is null.");
            Debug.Assert(!string.IsNullOrEmpty(body), "body is null or empty.");

            Message message = new Message
            {
                Body = body,
                Sent = this.ctx.Now,
            };

            // メッセージ投稿処理
#if DEBUG
            var currentUserState = this.db.Entry(this.ctx.CurrentUser).State;
            Debug.Assert(currentUserState == System.Data.EntityState.Unchanged, "CurrentUser is " + currentUserState);
#endif

            AttachFile(message, uploadedFile);

            message.SentUser = this.ctx.CurrentUser;
            message.Thread = thread;
            db.Messages.Add(message);

            return message;
        }

        /// <summary>
        /// スレッドに属するメッセージの添付ファイルを取得します。
        /// </summary>
        /// <remarks>
        /// スレッドの読み書きが可能かどうかはチェック済みであることを前提とします。
        /// </remarks>
        /// <param name="thread"></param>
        /// <param name="messageId"></param>
        /// <returns>
        /// メッセージとドキュメントのタプル。
        /// タプル自体はnullになりませんが、メッセージやドキュメントはnullである可能性があります。
        /// </returns>
        public Tuple<Models.Message, Models.Document> GetAttachment(Models.Thread thread, int messageId)
        {
            if (thread == null) throw new ArgumentNullException("thread");
            if (messageId <= 0) throw new ArgumentOutOfRangeException("messageId");
            
            var messageQuery =
                from gt in db.Threads where gt.ID == thread.ID
                from m in gt.Message where m.ID == messageId
                select m;
            LogUtility.DebugWriteQuery(messageQuery);

            var message = messageQuery.FirstOrDefault();
            var document = (message != null)
                ? db.Documents.FirstOrDefault(d => d.ID == message.AttachedDocumentID)
                : null;
            return Tuple.Create(message, document);
        }

        /// <summary>
        /// メッセージに添付ファイルを登録します。
        /// 保存処理は行いません。
        /// </summary>
        /// <param name="message">添付ファイルを登録するメッセージ。nullではないことを前提とします。</param>
        /// <param name="uploadedFile">添付ファイル。nullの可能性があります。</param>
        public void AttachFile(Message message, HttpPostedFileBase uploadedFile)
        {
            if (message == null) throw new ArgumentNullException("message");

            if (uploadedFile == null)
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
                    Uploaded = this.ctx.Now,
                    User = this.ctx.CurrentUser,
                    FileExtension = extension,
                    BinaryData = binaryData,
                };
                this.db.Documents.Add(document);
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