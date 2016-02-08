using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Routing;

namespace NihonUnisys.Olyzae.Models
{
    /// <summary>
    /// タイムラインです。
    /// </summary>
    /// <remarks>
    /// ユーザ数が増えて負荷が高くなった時に、
    /// パーティショニングしたりデータベースを使わない実装に変えたりすることを想定して、
    /// 他のテーブル（モデル）との関連を切っています。
    /// </remarks>
    public class Timeline
    {
        /// <summary>
        /// タイムラインのレコードID。Entity Frameworkの主キーとなる。
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// タイムラインを所有している参加者ID。
        /// </summary>
        [Required]
        // TODO: EF6.1にバージョンを上げて [Index] 属性を付与する
        public int OwnerID { get; set; }

        /// <summary>
        /// タイムラインにレコードが挿入された日時。
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// タイムラインに挿入されたレコードの種別。表示方法を決定するために使用する。
        /// </summary>
        [Required]
        public TimelineType Type { get; set; }

        /// <summary>
        /// タイムラインに挿入されたレコードの発生元の表示名。参加者名、プロジェクト名、企業名、企業アカウント名などが格納される想定。
        /// </summary>
        public string SourceName { get; set; }

        /// <summary>
        /// タイムラインに挿入されたレコードの概要。
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// コントローラー名。リンク先を表す。
        /// </summary>
        public string ControllerName { get; set; }

        /// <summary>
        /// アクション名。リンク先を表す。
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// ルートのパラメーターが含まれるオブジェクトをJSON化した文字列。リンク先を表す。
        /// </summary>
        public string RouteValuesJSON { get; set; }

        public RouteValueDictionary RouteValues
        {
            get
            {
                var json = this.RouteValuesJSON;
                if (json == null)
                {
                    return null;
                }
                try
                {
                    var dictionary = JsonConvert.DeserializeObject<RouteValueDictionary>(json);
                    return dictionary;
                }
                catch (Exception ex)
                {
                    string message = "プロパティ 'RouteValuesJSON' に不正な値が格納されています。\r\n"
                        + "Id: " + this.ID.ToString() + "\r\n"
                        + "RouteValuesJSON: '" + this.RouteValuesJSON + "'";
                    throw new InvalidOperationException(message, ex);
                }
            }
        }

        public static string ToJSON(object routeValues)
        {
            return (routeValues == null) ? null : JsonConvert.SerializeObject(routeValues);
        }
    }
}