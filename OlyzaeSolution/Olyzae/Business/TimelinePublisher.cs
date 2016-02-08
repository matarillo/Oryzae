using NihonUnisys.Olyzae.Framework;
using NihonUnisys.Olyzae.Models;
using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Diagnostics;
using System.Linq;
using System.Transactions;

namespace NihonUnisys.Olyzae.Business
{
    /// <summary>
    /// タイムラインテーブルにレコードを挿入する処理を行うクラス。
    /// </summary>
    /// <remarks>
    /// <para>
    /// プッシュ型のフレンド・タイムラインモデルを採用しています。
    /// 参考：http://labs.cybozu.co.jp/blog/kazuho/archives/2008/06/friends_timeline.php
    /// </para>
    /// </remarks>
    public class TimelinePublisher : IDisposable
    {
        /// <summary>
        /// アプリケーションのコンテキスト情報。
        /// </summary>
        public ExecutionContext ExecutionContext { get; set; }

        /// <summary>
        /// エンティティ データに対してクエリを実行してそのデータをオブジェクトとして操作するためのコンテキスト。
        /// </summary>
        /// <remarks>
        /// コントローラーのDBアクセスとは別のトランザクションで動作する設計。
        /// </remarks>
        public Entities DbContext { get; set; }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <remarks>
        /// プロキシ作成をせず、遅延読み込みをしない設定でデータベースコンテキストを作成します。
        /// </remarks>
        public TimelinePublisher()
        {
            var db = new Entities();
            db.Configuration.ProxyCreationEnabled = false;
            db.Configuration.LazyLoadingEnabled = false;
            this.DbContext = db;

            var ctx = ExecutionContext.Create();
            this.ExecutionContext = ctx;
        }

        /// <summary>
        /// タイムラインにレコードを挿入します。
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public int Publish(IEnumerable<Timeline> target)
        {
            using (var scope = this.NewScopeForInserting())
            {
                var result = this.PublishInternal(target);
                scope.Complete();
                return result;
            }
        }

        protected internal virtual int PublishInternal(IEnumerable<Timeline> target)
        {
            foreach (var tl in target)
            {
                this.DbContext.Timelines.Add(tl);
            }
            return this.DbContext.SaveChanges();
        }

        /// <summary>
        /// 指定したユーザーのタイムラインを取得します。
        /// </summary>
        /// <param name="ownerId">ユーザーのID。</param>
        /// <param name="pageNumber">ページング処理をする際のページ番号。</param>
        /// <param name="pageSize">一度に取得するレコード数。</param>
        /// <returns></returns>
        public IList<Timeline> Subscribe(int ownerId, int pageNumber, int pageSize)
        {
            using (var scope = this.NewScopeForSelecting())
            {
                var result = this.SubscribeInternal(ownerId, pageNumber, pageSize);
                scope.Complete();
                return result;
            }
        }

        protected internal virtual IList<Timeline> SubscribeInternal(int ownerId, int pageNumber, int pageSize)
        {
            var query = this.DbContext.Timelines
                .Where(tl => tl.OwnerID == ownerId)
                .OrderByDescending(tl => tl.Timestamp)
                .Skip(pageSize * pageNumber)
                .Take(pageSize);
            LogUtility.DebugWriteQuery(query);
            var timelines = query.ToList();
            return timelines;
        }

        protected internal virtual TransactionScope NewScopeForInserting()
        {
            var transactionOptions = new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted };
            return new TransactionScope(TransactionScopeOption.RequiresNew, transactionOptions);
        }

        protected internal virtual TransactionScope NewScopeForSelecting()
        {
            var transactionOptions = new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted };
            return new TransactionScope(TransactionScopeOption.RequiresNew, transactionOptions);
        }

        /// <summary>
        /// 現在のインスタンスによって使用されているすべてのリソースを解放します。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// アンマネージ リソースを解放し、必要に応じてマネージ リソースも解放します。
        /// </summary>
        /// <param name="disposing">
        /// マネージ リソースとアンマネージ リソースの両方を解放する場合は true。
        /// アンマネージ リソースだけを解放する場合は false。
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                var db = this.DbContext;
                if (db != null)
                {
                    db.Dispose();
                }
            }
        }
    }
}