using NihonUnisys.Olyzae.Framework;
using NihonUnisys.Olyzae.Models;
using System;
using System.Data.Entity;
using System.Data.Objects;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.Routing;

namespace NihonUnisys.Olyzae.Controllers.Participants
{
    [Authorize(Roles = "ParticipantUser")]
    public abstract class AbstractParticipantProjectController : Controller
    {
        /// <summary>
        /// プロジェクトのIDを取り出すルーティングパラメータ名。
        /// </summary>
        public const string ProjectIdKey = "projectId";

        /// <summary>
        /// アプリケーションを実行している現在の参加者。
        /// </summary>
        public ParticipantUser CurrentUser { get; set; }

        /// <summary>
        /// アプリケーションのコンテキスト情報。
        /// </summary>
        public ExecutionContext ExecutionContext { get; set; }

        /// <summary>
        /// 現在参照しているプロジェクト情報。
        /// </summary>
        /// <remarks>
        /// </remarks>
        public Project Project { get; set; }

        /// <summary>
        /// エンティティ データに対してクエリを実行してそのデータをオブジェクトとして操作するためのコンテキスト。
        /// </summary>
        /// <remarks>
        /// </remarks>
        public Entities DbContext { get; set; }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <remarks>
        /// プロキシ作成をせず、遅延読み込みをしない設定でデータベースコンテキストを作成します。
        /// </remarks>
        protected AbstractParticipantProjectController()
        {
            var db = new Entities();
            db.Configuration.ProxyCreationEnabled = false;
            db.Configuration.LazyLoadingEnabled = false;
            this.DbContext = db;

            var ctx = ExecutionContext.Create();
            this.ExecutionContext = ctx;
        }

        /// <summary>
        /// アンマネージ リソースを解放し、必要に応じてマネージ リソースも解放します。
        /// </summary>
        /// <param name="disposing">
        /// マネージ リソースとアンマネージ リソースの両方を解放する場合は true。
        /// アンマネージ リソースだけを解放する場合は false。
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var db = this.DbContext;
                if (db != null)
                {
                    db.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// アクション メソッドの呼び出し前に呼び出されます。
        /// </summary>
        /// <remarks>
        /// ルーティングパラメータからprojectIdを取得して、
        /// 現在のユーザーがそのプロジェクトに参加しているかをデータベースに問い合わせます。
        /// 現在のユーザーがそのプロジェクトに参加していた場合は、
        /// ユーザー情報を CurrentUser プロパティに設定し、
        /// プロジェクト情報を Projectプロパティ および ViewBag.Projectに設定します。
        /// </remarks>
        /// <param name="filterContext">
        /// 現在の要求およびアクションに関する情報。
        /// ルーティングパラメータに"projectId"が存在していることが要請されます。
        /// </param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            int projectId;
            if (!TryGetProjectId(filterContext.RequestContext.RouteData.Values, out projectId))
            {
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                return;
            }

            var ctx = ExecutionContext.Create();
            var participantUser = ctx.CurrentUser as ParticipantUser;
            if (participantUser == null)
            {
                // 実行コンテキストが正しく設定されていない
                throw new InvalidOperationException("実行コンテキストにParticipantUserが設定されていません。");
            }

            //　TODO: トランザクションの検討
            var db = this.DbContext;
            if (db == null)
            {
                // DBコンテキストが正しく設定されていない
                throw new InvalidOperationException("データベースコンテキストが設定されていません。");
            }

            // 自分が参加しているプロジェクトのみ取得可能
            db.Users.Attach(participantUser);
            var relation = db.Entry(participantUser)
                .Collection(u => u.Projects)
                .Query()
                .Include(pup => pup.Project)
                .Where(pup => pup.Project.ID == projectId)
                .Include(pup => pup.Project.ProjectPage) // ビュー描画用
                .Include(pup => pup.Project.Duration) // ビューのメニュー描画用
                .FirstOrDefault();

            // 現在のuser とprojectが接続された。

            if (relation == null)
            {
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                return;
            }

            this.CurrentUser = participantUser;
            this.Project = relation.Project;
            this.ViewBag.Project = relation.Project;
        }

        internal static bool TryGetProjectId(RouteValueDictionary routeValues, out int result)
        {
            object o;
            if (!routeValues.TryGetValue(ProjectIdKey, out o))
            {
                result = default(int);
                return false;
            }
            try
            {
                result = Convert.ToInt32(o);
                return true;
            }
            catch (SystemException)
            {
                // do nothing
            }
            result = default(int);
            return false;
        }
    }
}