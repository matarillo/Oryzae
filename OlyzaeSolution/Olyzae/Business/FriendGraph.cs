using NihonUnisys.Olyzae.Framework;
using NihonUnisys.Olyzae.Models;
using System.Collections.Generic;
using System.Linq;

namespace NihonUnisys.Olyzae.Business
{
    /// <summary>
    /// 友人関係を処理するクラス。
    /// </summary>
    public class FriendGraph
    {
        private readonly Entities db;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="db">データベースコンテキスト。</param>
        public FriendGraph(Entities db)
        {
            this.db = db;
        }

        /// <summary>
        /// 指定された参加者の友人を取得して返します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// プロトタイプでは、同じプロジェクトに参加している全ユーザーを返します。
        /// </para>
        /// <para>
        /// プロトタイプでは、このメソッドはトランザクションを管理しません。
        /// </para>
        /// </remarks>
        /// <param name="user"></param>
        /// <returns></returns>
        public ICollection<ParticipantUser> GetFriends(int userID)
        {
            var query =
                (from u in this.db.Users.OfType<ParticipantUser>()
                 where u.ID == userID
                 from pup in u.Projects
                 from f in pup.Project.ParticipantUsers
                 where f.ParticipantUser.ID != userID
                 select f.ParticipantUser)
                .Distinct();
            LogUtility.DebugWriteQuery(query);
            var friends = query.ToList();
            return friends;
        }

        /// <summary>
        /// 指定された参加者同士が友人関係にあるかどうかを返します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// プロトタイプでは、同じプロジェクトに参加しているかどうかで判断します。
        /// </para>
        /// <para>
        /// プロトタイプでは、このメソッドはトランザクションを管理しません。
        /// </para>
        /// </remarks>
        /// <param name="user1Id"></param>
        /// <param name="user2Id"></param>
        /// <returns></returns>
        public bool IsFriend(int user1Id, int user2Id)
        {
            var query =
                from pup1 in this.db.ParticipantUserProjects
                from pup2 in this.db.ParticipantUserProjects
                where pup1.ParticipantUser.ID == user1Id
                && pup2.ParticipantUser.ID == user2Id
                && pup1.Project.ID == pup2.Project.ID
                select pup1.Project.ID;
            LogUtility.DebugWriteQuery(query);
            var isFriend = query.Any();
            return isFriend;
        }

        public IQueryable<ParticipantUser> Filter(IQueryable<ParticipantUser> query,int userID)
        {
            var friendQuery = query
                .Where(u => u.Projects.Any(pup =>
                    pup.Project.ParticipantUsers.Any(pup2 =>
                        pup2.ParticipantUser.ID == userID)));
            return friendQuery;
        }
    }
}