using NihonUnisys.Olyzae.Framework;
using NihonUnisys.Olyzae.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace NihonUnisys.Olyzae.Controllers.Participants
{
    [Authorize(Roles = "ParticipantUser")]
    public class HomeController : Controller
    {
        private Entities db;
        private ExecutionContext ctx;
        private Business.Thread threadBusiness;

        public HomeController()
        {
            this.db = new Entities();
            db.Configuration.ProxyCreationEnabled = false;
            db.Configuration.LazyLoadingEnabled = false;
            this.ctx = ExecutionContext.Create();
            this.threadBusiness = new Business.Thread(db, ctx);
        }

        // GET: /Home/Index
        public ActionResult Index()
        {
            // プロトではタイムラインの先頭から50件を取得する。
            const int pageSize = 50;
            IList<Timeline> timelines = null;
            using (var publisher = new Business.TimelinePublisher())
            {
                timelines = publisher.Subscribe(ctx.CurrentUser.ID, 0, pageSize);
            }

            // マイスレッドは別途取得する。
            var timelines_threadIds = timelines
                .Select(tl => new
                {
                    Timeline = tl,
                    ThreadId = GetThreadIdOrNull(tl)
                });
            var threadIds = timelines_threadIds
                .Where(x => x.ThreadId.HasValue)
                .Select(x => x.ThreadId.Value)
                .ToArray();
            var threadsQuery = db.Threads
                .OfType<PersonalThread>()
                .Where(t => t.OwnerUser.ID == ctx.CurrentUser.ID && threadIds.Contains(t.ID))
                .Include(t => t.Message)
                .Include(t => t.Message.Select(m => m.SentUser));
            LogUtility.DebugWriteQuery(threadsQuery);
            var threads = threadsQuery.ToDictionary(t => t.ID);

            var timelines_threads = timelines_threadIds
                .Select(x => Tuple.Create(x.Timeline, GetThreadOrNull(x.ThreadId, threads)))
                .ToList();

            return View(timelines_threads);
        }

        internal static int? GetThreadIdOrNull(Timeline tl)
        {
            if (tl == null) return null;
            if (tl.Type != TimelineType.PersonalThread) return null;
            var value = tl.RouteValues["threadId"] as IConvertible;
            if (value == null) return null;
            return Convert.ToInt32(value);
        }

        internal static Thread GetThreadOrNull(int? threadId, IDictionary<int, PersonalThread> threads)
        {
            PersonalThread result;
            if (threadId != null && threads.TryGetValue(threadId.Value, out result))
            {
                return result;
            }
            return null;
        }

        public ActionResult Recommend()
        {
            // TODO:
            // プロトでは、「おすすめ」の実装は面倒なので、
            // いま応募可能なプロジェクトを全件取得するだけとする。
            // （定員は無視する。ページングも省略する。）

            // DBクエリではProjectStatusだけで絞り込み、その後はメモリ上で絞り込む。
            db.Users.Attach(ctx.CurrentUser);
            var projectsAccepting = db.Projects
                .Where(p => p.Status == ProjectStatus.Accepting)
                .Include(p => p.Company)
                .Include(p => p.Duration)
                .ToList();
            var relations = db.ParticipantUserProjects
                .Where(pup => pup.ParticipantUser.ID == ctx.CurrentUser.ID)
                .ToList();

            var targetProjects = projectsAccepting
                .Where(p => p.IsAcceptingApplication() == true && p.IsCurrentUserAccepted() == false)
                .OrderBy(p => p.ID)
                .ToList();

            return View(targetProjects);
        }

        [ActionName("Profile")]
        public ActionResult ShowProfile()
        {
            return View(ctx.CurrentUser as ParticipantUser);
        }

        // GET: /Home/ProfileImage?thumbnail=True
        public ActionResult ProfileImage(bool? thumbnail)
        {
            if (thumbnail.HasValue && thumbnail.Value)
            {
                return RedirectToAction("ProfileImage", "Document", new { id = ctx.CurrentUser.ID, width = 126, height = 126 });
            }
            else
            {
                return RedirectToAction("ProfileImage", "Document", new { id = ctx.CurrentUser.ID });
            }
        }

        // GET: /Home/EditUser
        public ActionResult EditUser()
        {
            var model = db.Users.OfType<ParticipantUser>().FirstOrDefault(x => x.ID == ctx.CurrentUser.ID);
            if (model == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            return View(model);
        }

        // POST: /Home/EditUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser([Bind(Include = "ID,UserName,RawPassword,DisplayName,OnlineName,Kana,Gender,BirthDay,AcademicRecord,Departments,AcademicYear,EMailAddress,PhoneNumber,Zip,State,City,StreetAddress1,StreetAddress2,ReturningHomeZip,ReturningHomeState,ReturningHomeCity,ReturningHomeStreetAddress1,ReturningHomeStreetAddress2,Academic,Happpiness,CareerAnchors,Mentor,Answer")] ParticipantUser user, string[] answer)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            // パスワード変更の対応のため、
            // 受け取ったuserをそのまま使わない。

            var currentUser = (ParticipantUser)ctx.CurrentUser;
            if (currentUser.ID != user.ID)
            {
                return RedirectToAction("Profile");
            }

            db.Users.Attach(currentUser);
            currentUser.UserName = user.UserName;
            currentUser.DisplayName = user.DisplayName;
            currentUser.OnlineName = user.OnlineName;
            currentUser.Kana = user.Kana;
            currentUser.Gender = user.Gender;
            currentUser.BirthDay = user.BirthDay;
            currentUser.AcademicRecord = user.AcademicRecord;
            currentUser.Departments = user.Departments;
            currentUser.AcademicYear = user.AcademicYear;
            currentUser.EMailAddress = user.EMailAddress;
            currentUser.PhoneNumber = user.PhoneNumber;
            currentUser.Zip = user.Zip;
            currentUser.State = user.State;
            currentUser.City = user.City;
            currentUser.StreetAddress1 = user.StreetAddress1;
            currentUser.StreetAddress2 = user.StreetAddress2;
            currentUser.ReturningHomeZip = user.ReturningHomeZip;
            currentUser.ReturningHomeState = user.ReturningHomeState;
            currentUser.ReturningHomeCity = user.ReturningHomeCity;
            currentUser.ReturningHomeStreetAddress1 = user.ReturningHomeStreetAddress1;
            currentUser.ReturningHomeStreetAddress2 = user.ReturningHomeStreetAddress2;
            currentUser.Academic = user.Academic;
            currentUser.Happpiness = user.Happpiness;
            currentUser.CareerAnchors = user.CareerAnchors;
            currentUser.Mentor = user.Mentor;
            currentUser.ProfileImageDocumentID = GetProfileImageFromRequest() ?? currentUser.ProfileImageDocumentID;
            // TODO ファイルアップロード対応
            // currentUser.ProfileImageDocumentID = user.ProfileImageDocumentID;

            // パスワード変更
            if (!string.IsNullOrEmpty(user.RawPassword))
            {
                currentUser.Password = Models.User.GenerateHashedPassword(user.RawPassword);
            }

            // 質問回答を配列からカンマ区切りに入れ直し
            StringBuilder sb = new StringBuilder(string.Empty);
            int i = 0;
            for (; i < answer.Length - 1; i++)
            {
                // @Html.checkBoxで自動生成されるhidden項目のvalueの既定値が"false"なので、弾く
                if (answer[i] == "false")
                    continue;

                sb.Append(answer[i]);
                sb.Append(",");
            }
            sb.Append(answer[i]);
            currentUser.Answer = sb.ToString();

            db.Entry(currentUser).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Profile");
        }

        /// <summary>
        /// ブラウザーを使用してアップロードされた画像があれば、
        /// Documentsテーブルに追加してIDを返します。
        /// </summary>
        /// <returns>
        /// 新しいDocumentオブジェクトのID。
        /// アップロードされた画像がなければnull。
        /// </returns>
        internal Guid? GetProfileImageFromRequest()
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
                Uploaded = this.ctx.Now,
                User = this.ctx.CurrentUser
            };
            this.db.Documents.Add(document);
            return document.ID;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreatePersonalThread(string body, HttpPostedFileBase uploadedFile)
        {
            if (string.IsNullOrEmpty(body))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // ユーザーのマイページスレッドを新規追加する
            db.Users.Attach(ctx.CurrentUser);

            var newThread = new PersonalThread()
            {
                // マイページスレッドにはスレッド名は不要だが、Required属性がついているため日時を設定しておく。
                ThreadName = ctx.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                // マイページスレッドには期間は不要。
                Duration = null,
                OwnerUser = (ParticipantUser)ctx.CurrentUser
            };
            db.Threads.Add(newThread);
            var newMessage = threadBusiness.AddMessage(newThread, body, uploadedFile);
            try
            {
                db.SaveChanges();
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                var es = ex.Entries.ToArray();
                var iex = ex.InnerException as OptimisticConcurrencyException;
                System.Diagnostics.Debugger.Break();
                throw;
            }

            // 自分のおよび友人のタイムラインを更新する
            // TODO: 非同期処理の検討
            var myTimeLine = new Timeline
            {
                OwnerID = ctx.CurrentUser.ID,
                Timestamp = ctx.Now,
                Type = TimelineType.PersonalThread,
                RouteValuesJSON = Timeline.ToJSON(new { threadId = newThread.ID }),
            };
            var friends = new Business.FriendGraph(db)
                .GetFriends(ctx.CurrentUser.ID);
            var summary = newMessage.BodySummary;
            var friendTimeLines = friends.Select(u => new Timeline
            {
                OwnerID = u.ID,
                Timestamp = ctx.Now,
                Type = TimelineType.FriendThread,
                SourceName = ctx.CurrentUser.DisplayName,
                Summary = summary,
                ActionName = "Index",
                ControllerName = "User",
                RouteValuesJSON = Timeline.ToJSON(new { id = ctx.CurrentUser.ID, threadId = newThread.ID }),
            });
            using (var publisher = new Business.TimelinePublisher())
            {
                publisher.Publish(new[] { myTimeLine }.Concat(friendTimeLines));
            }

            return RedirectToAction("Index");
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