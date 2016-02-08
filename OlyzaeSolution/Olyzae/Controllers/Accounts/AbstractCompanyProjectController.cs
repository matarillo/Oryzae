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

namespace NihonUnisys.Olyzae.Controllers.Accounts
{
    [Authorize(Roles = "AccountUser")]
    public abstract class AbstractCompanyProjectController : Controller
    {
        /// <summary>
        /// プロジェクトのIDを取り出すルーティングパラメータ名。
        /// </summary>
        public const string ProjectIdKey = "projectId";

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
        public AbstractCompanyProjectController()
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
        /// 現在のユーザーが所属している会社がそのプロジェクトを管理しているかをデータベースに問い合わせます。
        /// 現在のユーザーが所属している会社がそのプロジェクトを管理していた場合は、
        /// プロジェクト情報を Projectプロパティ および ViewBag.Projectに設定します。
        /// </remarks>
        /// <param name="filterContext">
        /// 現在の要求およびアクションに関する情報。
        /// ルーティングパラメータに"projectId"が存在していることが要請されます。
        /// </param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            int projectId;
            if (!TryGetProjectId(filterContext.RequestContext, out projectId))
            {
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                return;
            }

            var ctx = ExecutionContext.Create();
            var accountUser = ctx.CurrentUser as AccountUser;
            if (accountUser == null)
            {
                // 実行コンテキストが正しく設定されていない
                throw new InvalidOperationException("実行コンテキストにAccountUserが設定されていません。");
            }

            var project = GetProject(projectId, accountUser);
            if (project == null)
            {
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                return;
            }

            this.Project = project;
            this.ViewBag.Project = project;
        }

        public Project GetProject(int projectId, AccountUser accountUser)
        {
            //　TODO: トランザクションの検討
            var db = this.DbContext;
            if (db == null)
            {
                // DBコンテキストが正しく設定されていない
                throw new InvalidOperationException("データベースコンテキストが設定されていません。");
            }

            // 所属会社のプロジェクトのみ取得可能
            db.Companies.Attach(accountUser.Company);
            var projectQuery = db.Projects
                .Where(p => p.ID == projectId && p.Company.ID == accountUser.Company.ID)
                .Include(p => p.ProjectPage)
                .Include(p => p.Duration);
            LogUtility.DebugWriteQuery(projectQuery);

            var project = projectQuery.FirstOrDefault();
            return project;
        }

        /// <summary>
        /// 要求コンテキストからプロジェクトIDを取得します。
        /// </summary>
        /// <remarks>
        /// プロジェクトIDを使用したルーティングに合致している場合は、ルーティング情報のデータに含まれます。
        /// それ以外の場合は、クエリパラメータに含まれます。
        /// TODO: POSTのフォームに入っていた場合の対応
        /// </remarks>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal static bool TryGetProjectId(RequestContext context, out int result)
        {
            var routeValues = context.RouteData.Values;
            object o;
            if (routeValues.TryGetValue(ProjectIdKey, out o))
            {
                // projectIdはルーティング情報のデータに含まれている
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

            var queryParameter = context.HttpContext.Request.QueryString[ProjectIdKey];
            if (!string.IsNullOrEmpty(queryParameter))
            {
                // projectIdはクエリパラメータに含まれている
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

            // projectIdが含まれていなかった
            result = default(int);
            return false;
        }
    }
}