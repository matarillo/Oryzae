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
    /// <summary>
    /// 要求コンテキストに"groupId"を含んでいた場合の共通処理を実行する抽象コントローラークラスです。
    /// 要求コンテキストに"groupId"を含まないアクションメソッドに対しては何もしません。
    /// </summary>
    [Authorize(Roles = "ParticipantUser")]
    public abstract class AbstractParticipantGroupController : Controller
    {
        /// <summary>
        /// グループのIDを取り出すパラメータ名。
        /// </summary>
        public const string GroupIdKey = "groupId";

        /// <summary>
        /// アプリケーションを実行している現在の参加者。
        /// </summary>
        public ParticipantUser CurrentUser { get; set; }

        /// <summary>
        /// アプリケーションのコンテキスト情報。
        /// </summary>
        public ExecutionContext ExecutionContext { get; set; }

        /// <summary>
        /// 現在参照しているグループ情報。
        /// </summary>
        /// <remarks>
        /// </remarks>
        public Group Group { get; set; }

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
        protected AbstractParticipantGroupController()
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
        /// <para>
        /// 最初に、ユーザー情報を CurrentUser プロパティに設定します。
        /// </para>
        /// <para>
        /// 次に、要求コンテキストからgroupIdを取得します。
        /// 取得できなかった場合は、そこで共通処理を中断してアクションメソッドを開始します。
        /// </para>
        /// <para>
        /// 要求コンテキストからgroupIdを取得できた場合は、
        /// 現在のユーザーがそのグループに参加しているかをデータベースに問い合わせます。
        /// 現在のユーザーがそのグループに参加していた場合は、
        /// グループ情報を Groupプロパティ および ViewBag.Groupに設定します。
        /// </para>
        /// </remarks>
        /// <param name="filterContext">
        /// 現在の要求およびアクションに関する情報。
        /// </param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var ctx = ExecutionContext.Create();
            var participantUser = ctx.CurrentUser as ParticipantUser;
            if (participantUser == null)
            {
                // 実行コンテキストが正しく設定されていない
                throw new InvalidOperationException("実行コンテキストにParticipantUserが設定されていません。");
            }
            this.CurrentUser = participantUser;

            int groupId;
            if (!TryGetGroupId(filterContext.RequestContext, out groupId))
            {
                // groupIdが取得できなかった場合は、共通処理を終了する。
                return;
            }

            //　TODO: トランザクションの検討
            var db = this.DbContext;
            if (db == null)
            {
                // DBコンテキストが正しく設定されていない
                throw new InvalidOperationException("データベースコンテキストが設定されていません。");
            }

            // 自分が参加しているグループのみ取得可能
            db.Users.Attach(participantUser);

            // 自分とグループを紐づける関連オブジェクトを1件取得
            var relationshipQuery = db.ParticipantUserGroups
                .Where(pug => pug.Group.ID == groupId && pug.ParticipantUser.ID == participantUser.ID)
                .Include(pug => pug.Group);
            LogUtility.DebugWriteQuery(relationshipQuery);

            var relationship = relationshipQuery.FirstOrDefault();
            if (relationship == null)
            {
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                return;
            }

            var projectGroup = relationship.Group as ProjectGroup;
            if (projectGroup != null)
            {
                // ProjectGroupの場合は関連するProjectも1件取得
                db.Projects.FirstOrDefault(p => p.ProjectGroups.Any(pg => pg.ID == projectGroup.ID));
            }

            this.Group = relationship.Group;
            this.ViewBag.Group = relationship.Group;
        }

        /// <summary>
        /// 要求コンテキストからグループIDを取得します。
        /// </summary>
        /// <remarks>
        /// グループIDを使用したルーティングに合致している場合は、ルーティング情報のデータに含まれます。
        /// それ以外の場合は、クエリパラメータに含まれます。
        /// TODO: POSTのフォームに入っていた場合の対応
        /// </remarks>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal static bool TryGetGroupId(RequestContext context, out int result)
        {
            var routeValues = context.RouteData.Values;
            object o;
            if (routeValues.TryGetValue(GroupIdKey, out o))
            {
                // groupIdはルーティング情報のデータに含まれている
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

            var queryParameter = context.HttpContext.Request.QueryString[GroupIdKey];
            if (!string.IsNullOrEmpty(queryParameter))
            {
                // groupIdはクエリパラメータに含まれている
                try
                {
                    result = Convert.ToInt32(queryParameter);
                    return true;
                }
                catch (SystemException)
                {
                    // do nothing
                }
                result = default(int);
                return false;
            }

            // groupIdが含まれていなかった
            result = default(int);
            return false;
        }
    }
}