using System;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;

namespace NihonUnisys.Olyzae.Framework
{
    public static class HtmlHelperExtensions
    {
        public static MvcHtmlString Attribute(this HtmlHelper htmlHelper, string attribute)
        {
            return MvcHtmlString.Create(htmlHelper.AttributeEncode(attribute));
        }

        public static MvcHtmlString MenuLink(this HtmlHelper htmlHelper, string linkText, string actionName)
        {
            return MenuLink(htmlHelper, linkText, actionName,
                controllerName: null,
                routeValues: null,
                optionValues: null);
        }

        public static MvcHtmlString MenuLink(this HtmlHelper htmlHelper, string linkText, string actionName, string controllerName)
        {
            return MenuLink(htmlHelper, linkText, actionName, controllerName,
                routeValues: null,
                optionValues: null);
        }

        public static MvcHtmlString MenuLink(this HtmlHelper htmlHelper, string linkText, string actionName, string controllerName, object routeValues)
        {
            return MenuLink(htmlHelper, linkText, actionName, controllerName, routeValues,
                optionValues: null);
        }

        public static MvcHtmlString MenuLink(this HtmlHelper htmlHelper, string linkText, string actionName, string controllerName, object routeValues, object optionValues)
        {
            var routeDictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(routeValues);
            var optionDictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(optionValues);

            var outerTagName = GetOuterTagName(optionDictionary);
            var activeClassName =
                IsActive(htmlHelper, actionName, controllerName, routeDictionary, optionDictionary)
                    ? GetActiveClassName(optionDictionary)
                    : null;

            var menuHtml =
                string.IsNullOrEmpty(outerTagName)
                    ? GenerateMenuLink(htmlHelper, linkText, actionName, controllerName, activeClassName, routeDictionary)
                    : GenerateMenuLinkAndWrap(htmlHelper, linkText, actionName, controllerName, outerTagName, activeClassName, routeDictionary);
            return MvcHtmlString.Create(menuHtml);
        }

        internal static bool IsActive(HtmlHelper htmlHelper, string actionName, string controllerName, RouteValueDictionary routeValues, RouteValueDictionary optionValues)
        {
            return (IsCurrentController(htmlHelper, controllerName))
                && (IgnoreAction(optionValues) || IsCurrentAction(htmlHelper, actionName));
        }

        internal static bool IsCurrentController(HtmlHelper htmlHelper, string controllerName)
        {
            if (string.IsNullOrEmpty(controllerName))
            {
                // 未指定の時は現在のコントローラーと見なす
                return true;
            }
            string currentController = htmlHelper.ViewContext.RouteData.GetRequiredString("controller");
            return string.Equals(currentController, controllerName);
        }

        internal static bool IsCurrentAction(HtmlHelper htmlHelper, string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
            {
                // 未指定の時は現在のアクションと見なす
                return true;
            }
            string currentAction = htmlHelper.ViewContext.RouteData.GetRequiredString("action");
            return string.Equals(currentAction, actionName);
        }

        internal static bool IgnoreAction(RouteValueDictionary optionValues)
        {
            bool ignoreAction;
            TryGetBoolean(optionValues, "ignoreAction", out ignoreAction);
            return ignoreAction;
        }

        internal static string GetActiveClassName(RouteValueDictionary optionValues)
        {
            string className;
            if (!TryGetString(optionValues, "class", out className))
            {
                // 未指定の時は既定値
                className = "active";
            }
            return className;
        }

        internal static string GetOuterTagName(RouteValueDictionary optionValues)
        {
            string tagName;
            if (!TryGetString(optionValues, "tag", out tagName))
            {
                // 未指定の時は既定値
                tagName = "li";
            }
            return tagName;
        }

        internal static string GenerateMenuLink(HtmlHelper htmlHelper, string linkText, string actionName, string controllerName, string className, RouteValueDictionary routeValues)
        {
            object attr = string.IsNullOrEmpty(className) ? null : new { @class = className };
            var htmlAttributes = HtmlHelper.AnonymousObjectToHtmlAttributes(attr);

            return HtmlHelper.GenerateLink(
                htmlHelper.ViewContext.RequestContext,
                htmlHelper.RouteCollection,
                linkText,
                null, // routeName
                actionName,
                controllerName,
                routeValues,
                htmlAttributes);
        }

        internal static string GenerateMenuLinkAndWrap(HtmlHelper htmlHelper, string linkText, string actionName, string controllerName, string tagName, string className, RouteValueDictionary routeValues)
        {
            var tagBuilder = new TagBuilder(tagName)
            {
                InnerHtml = GenerateMenuLink(htmlHelper, linkText, actionName, controllerName, null /* className */, routeValues)
            };
            if (!string.IsNullOrEmpty(className))
            {
                tagBuilder.AddCssClass(className);
            }
            return tagBuilder.ToString(TagRenderMode.Normal);
        }

        internal static bool TryGetString(RouteValueDictionary routeValues, string key, out string result)
        {
            object o;
            if (routeValues.TryGetValue(key, out o))
            {
                result = (o == null) ? null : o.ToString();
                return true;
            }
            result = null;
            return false;
        }

        internal static bool TryGetBoolean(RouteValueDictionary routeValues, string key, out bool result)
        {
            object o;
            if (routeValues.TryGetValue(key, out o))
            {
                try
                {
                    result = Convert.ToBoolean(o);
                    return true;
                }
                catch (SystemException)
                {
                    // do nothing
                }
            }
            result = default(bool);
            return false;
        }

        public static string ToText(this Models.ProjectCategory category)
        {
            switch (category)
            {
                case Models.ProjectCategory.Default:
                    return "その他";

                case Models.ProjectCategory.Career:
                    return "キャリア";
                
                case Models.ProjectCategory.Internship:
                    return "インターンシップ";
                
                case Models.ProjectCategory.Arbeit:
                    return "アルバイト";
                
                case Models.ProjectCategory.Study:
                    return "学び";
                
                case Models.ProjectCategory.Travel:
                    return "旅行";
                
                case Models.ProjectCategory.Event:
                    return "イベント";
                
                default:
                    break;
            }
            return "";
        }

        public static string ToValue(this Models.ProjectCategory category)
        {
            switch (category)
            {
                case Models.ProjectCategory.Default:
                    return "Default";

                case Models.ProjectCategory.Career:
                    return "Career";

                case Models.ProjectCategory.Internship:
                    return "Internship";

                case Models.ProjectCategory.Arbeit:
                    return "Arbeit";

                case Models.ProjectCategory.Study:
                    return "Study";

                case Models.ProjectCategory.Travel:
                    return "Travel";

                case Models.ProjectCategory.Event:
                    return "Event";

                default:
                    break;
            }
            return "";
        }

        public static string ToText(this Models.ProjectStatus status)
        {
            switch (status)
            {
                case Models.ProjectStatus.Default:
                    return "公開前";

                case Models.ProjectStatus.Accepting:
                    return "募集開始";

                case Models.ProjectStatus.Accepted:
                    return "募集終了";

                case Models.ProjectStatus.Holding:
                    return "実施開始";

                case Models.ProjectStatus.Held:
                    return "実施終了";

                case Models.ProjectStatus.Cancelled:
                    return "キャンセル";

                case Models.ProjectStatus.Expired:
                    return "公開停止";

                default:
                    break;
            }
            return "";
        }

        public static string ToValue(this Models.ProjectStatus status)
        {
            switch (status)
            {
                case Models.ProjectStatus.Default:
                    return "Default";

                case Models.ProjectStatus.Accepting:
                    return "Accepting";

                case Models.ProjectStatus.Accepted:
                    return "Accepted";

                case Models.ProjectStatus.Holding:
                    return "Holding";

                case Models.ProjectStatus.Held:
                    return "Held";

                case Models.ProjectStatus.Cancelled:
                    return "Cancelled";

                case Models.ProjectStatus.Expired:
                    return "Expired";

                default:
                    break;
            }
            return "";
        }

        /// <summary>
        /// 遷移可能な、次のステートの配列を取得します。
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// <list type="table">
        /// <listheader>
        /// <term>現在の値</term>
        /// <description>説明</description>
        /// </listheader>
        /// <item>
        /// <term>Default</term>
        /// <description>デフォルト状態。プロジェクトは未公開。Acceptingに遷移可能。</description>
        /// </item>
        /// <item>
        /// <term>Accepting</term>
        /// <description>募集開始状態（公開）。募集期間内であれば参加者が申込み可能。AcceptedとCancelledに遷移可能。</description>
        /// </item>
        /// <item>
        /// <term>Accepted</term>
        /// <description>募集早期締め切り状態（公開）。募集期間内だが参加者は申込み不可能。HoldingとCancelledに遷移可能。</description>
        /// </item>
        /// <item>
        /// <term>Holding</term>
        /// <description>プロジェクト開催状態（公開）。HeldとCancelledとExpiredに遷移可能。</description>
        /// </item>
        /// <item>
        /// <term>Held</term>
        /// <description>プロジェクト完了状態（公開）。Expiredに遷移可能。</description>
        /// </item>
        /// <item>
        /// <term>Cancelled</term>
        /// <description>開催側都合によるプロジェクト中止状態（公開）。Expiredに遷移可能。</description>
        /// </item>
        /// <item>
        /// <term>Expired</term>
        /// <description>プロジェクト破棄状態。プロジェクトの公開を停止する。</description>
        /// </item>
        /// </list>
        /// </remarks>
        public static Models.ProjectStatus[] ToNextStates(this Models.ProjectStatus status)
        {
            switch (status)
            {
                case Models.ProjectStatus.Default:
                    return new[] { Models.ProjectStatus.Default, Models.ProjectStatus.Accepting };

                case Models.ProjectStatus.Accepting:
                    return new[] { Models.ProjectStatus.Accepting, Models.ProjectStatus.Accepted, Models.ProjectStatus.Cancelled };

                case Models.ProjectStatus.Accepted:
                    return new[] { Models.ProjectStatus.Accepted, Models.ProjectStatus.Holding, Models.ProjectStatus.Cancelled };

                case Models.ProjectStatus.Holding:
                    return new[] { Models.ProjectStatus.Holding, Models.ProjectStatus.Held, Models.ProjectStatus.Cancelled };

                case Models.ProjectStatus.Held:
                    return new[] { Models.ProjectStatus.Held, Models.ProjectStatus.Expired };

                case Models.ProjectStatus.Cancelled:
                    return new[] { Models.ProjectStatus.Cancelled, Models.ProjectStatus.Expired };

                case Models.ProjectStatus.Expired:
                    return new[] { Models.ProjectStatus.Expired };

                default:
                    break;
            }
            return new Models.ProjectStatus[0];
        }
    }
}